# 06 — Asynchronous Programming & Delegates

> **Curriculum context.** The Async-Programming track of the Israeli Ministry of Education's *Integrated Data Applications* rubric prescribes:
> * "The project is a 'server-client' application for managing data over the network."
> * "Heavy use of advanced features of object-oriented and event-driven programming."
> * "Server-side data handling is based on an **asynchronous mechanism**, which uses, among other things, **delegates**."
> * "Multiple clients on multiple platforms (Pc / Web / Mobile)."
>
> This document is the canonical mapping from those requirements onto code.

---

## 1. Why this document exists

`async`/`await` and delegates are introduced in passing in chapters 01, 02, 04 and 05. This chapter consolidates them into a single coherent narrative because the rubric asks examiners to look for the **combination** specifically.

---

## 2. The Three Async Tiers

### 2.1 Tier 1 — UI-edge async (ViewModel commands)

```csharp
// src/MentoringApp.ViewModel/ViewModel/User/LoginViewModel.cs
[RelayCommand]
private async Task SendVerificationCode()
{
    ValidateAllProperties();
    if (HasErrors) return;

    var result = await _auth.SendCodeAsync(new SendCodeRequest(NationalId));
    if (!result.Success)
    {
        ErrorMessage = result.ErrorMessage;
        return;
    }
    WasCodeSent = true;
}
```

**Properties of this tier:**

- The `[RelayCommand]` source generator produces an `IAsyncRelayCommand` because the method returns `Task`.
- `IsRunning` is exposed automatically so the view can disable the button while the request is in flight.
- The method signature is `Task`, never `void` — exceptions are observed by the binding pipeline.

> **Reviewer note** — `async void` is reserved for *event handlers only*. A ViewModel command handler that compiles as `async void` is an automatic review-rejection.

### 2.2 Tier 2 — Network-edge async (`ApiClient` + Service)

```csharp
public sealed class AuthApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<LoginResponse> LoginAsync(LoginRequest req)
        => PostAsync<LoginResponse>("api/auth/login", req);
}
```

**Properties of this tier:**

- Every method returns `Task` or `Task<T>` — never `void` and never blocking.
- `PostAsync<T>` is a single line that internally `await`s `HttpClient.PostAsync`, `EnsureSuccessStatusCode`, and `ReadFromJsonAsync<T>`.

The server side mirrors this:

```csharp
app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
{
    var result = await auth.LoginAsync(req.NationalId);
    return result.Success ? Results.Ok(result.Data) : Results.BadRequest(result.ErrorMessage);
});
```

### 2.3 Tier 3 — Storage-edge async (`SQLiteConnectionService` + `SqlXxxRepo`)

```csharp
public async Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null) where T : new()
{
    using var conn = new SqliteConnection(_connectionString);
    await conn.OpenAsync();

    using var cmd = new SqliteCommand(sql, conn);
    AddParameters(cmd, parameters);

    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return default;

    return MapReaderToObject<T>(reader);
}
```

**Properties of this tier:**

- `OpenAsync` / `ExecuteReaderAsync` / `ReadAsync` are awaited individually — none of the I/O points block a thread.
- Each call uses its own connection, allowing genuine parallelism if `Task.WhenAll` is used at a higher tier.

> **Performance note** — SQLite serialises *write* operations at the file-lock level. Async parallel **reads** are genuinely concurrent; async parallel **writes** are not. Match the access pattern to the workload.

---

## 3. Built-in Delegates In Use

The codebase deliberately uses the BCL's general-purpose delegate types — `Action`, `Func`, `EventHandler` — rather than declaring custom `delegate` types.

### 3.1 Delegate inventory

| Delegate type | Site | Purpose |
|---|---|---|
| `Action` | `NavigationStore.CurrentViewModelChanged` | Notify the shell that the visible VM changed |
| `Action<INavigatable>` | `INavigationService.UseContext(...)` | Tell the navigation service how to render the active VM in *this* shell |
| `Func<Task>` | `NavigationService.NavigateCoreAsync(..., onNavigatedTo)` | Invoke the right `OnNavigatedToAsync` overload (parameterless or parameterised) |
| `Func<HttpRequestMessage, HttpResponseMessage>` | `FakeHttpMessageHandler` (tests) | Return a deterministic response for a request |
| `Action` (parameterless) | `event Action? CanGoBackChanged` | Multicast back-stack change notification |
| `EventHandler<PropertyChangedEventArgs>` | Implicit via `[ObservableProperty]` | Bind-engine refresh |

### 3.2 Walk-through — `INavigationService.UseContext`

```csharp
public IDisposable UseContext(Action<INavigatable> contextSetter)
{
    var store = new NavigationStore();
    Action updateUi = () => contextSetter(store.CurrentViewModel);
    store.CurrentViewModelChanged += updateUi;        // chain Action to event

    _contextStack.Push(contextSetter);
    _storeStack.Push(store);

    return new ContextReliever(() =>
    {
        store.CurrentViewModelChanged -= updateUi;    // detach
        _contextStack.Pop();
        _storeStack.Pop();
    });
}
```

**Why this is a textbook delegate use:**

1. **`Action<INavigatable>` parameter.** The navigation service does not know what a "shell" is — it only knows it should call back when the active VM changes.
2. **Closure over local `store`.** The `updateUi` lambda captures `store` so subsequent invocations refer to the right history stack.
3. **`+=` and `-=` symmetry.** The detach happens through `IDisposable.Dispose()` so the subscription cannot leak even on exceptions.

### 3.3 Walk-through — `Func<Task>` as async strategy

```csharp
private async Task NavigateCoreAsync<TViewModel>(
    TViewModel vm,
    Func<Task> onNavigatedTo,
    bool clearHistory = false)
    where TViewModel : class, INavigatable
{
    if (!_storeStack.TryPeek(out var currentStore)) return;

    if (currentStore.CurrentViewModel != null)
        await currentStore.CurrentViewModel.OnNavigatedFromAsync();

    if (clearHistory) currentStore.ClearHistory();

    currentStore.CurrentViewModel = vm;
    await onNavigatedTo();                             // ← delegate-driven async
    CanGoBackChanged?.Invoke();
}

public Task NavigateToAsync<TViewModel>() where TViewModel : class, INavigatable
{
    var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
    return NavigateCoreAsync(vm, () => vm.OnNavigatedToAsync());          // parameterless
}

public Task NavigateToAsync<TViewModel, TParameter>(TParameter p)
    where TViewModel : class, INavigatable<TParameter>
{
    var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
    return NavigateCoreAsync(vm, () => vm.OnNavigatedToAsync(p));          // parameterised
}
```

The two public overloads share **all** logic with `NavigateCoreAsync`; the only difference is which lifecycle hook fires. Without a `Func<Task>` parameter, this would have been a 60-line method with a `bool hasParameter` flag and an awkward branch.

---

## 4. The "Async Mechanism Using Delegates" Pattern

The rubric phrasing — *"asynchronous mechanism that uses delegates"* — maps cleanly onto two recurring patterns.

### 4.1 Pattern A — async strategy injection

```csharp
public async Task<TResult> RunPipelineAsync<TInput, TResult>(
    TInput input,
    Func<TInput, Task<TResult>> step)
{
    // ... validation, instrumentation ...
    return await step(input);
}
```

**Use cases in the codebase:**
- `NavigationService.NavigateCoreAsync(..., Func<Task>)`
- (Future) `MatchingFlowService` could accept a `Func<MatchScore, Task<bool>>` to filter candidates by an async business rule.

### 4.2 Pattern B — async fan-out

```csharp
var tasks = items.Select(item => ProcessAsync(item));
var results = await Task.WhenAll(tasks);
```

**Use cases:**
- `NotificationService.SendPhase1StartedAsync` fans out across all users.
- `MatchingFlowService` (future) could fan out across all mentor-mentee pairs to compute scores in parallel.

### 4.3 Pattern C — `EventHandler<T>` for cross-cutting notifications

```csharp
public class StoreBase
{
    public event EventHandler<EventArgs>? Changed;
    protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
```

> **Reviewer note** — events are appropriate when *zero or many* listeners need to react. Delegate parameters (`Func`/`Action`) are appropriate when *exactly one* caller-supplied behaviour is invoked.

---

## 5. Cancellation

Cancellation is part of an honest async surface, not optional decoration.

```csharp
public async Task<IEnumerable<MatchScore>> ScoreAllAsync(CancellationToken ct = default)
{
    var users = await _userService.GetAllUsersAsync(ct);
    return await Task.WhenAll(users.Select(u => ScoreOneAsync(u, ct)));
}
```

**Sources of tokens:**

| Source | Where |
|---|---|
| `IAsyncRelayCommand.CancelCommand` | Generated by `[RelayCommand]` when the method takes a `CancellationToken` |
| `HttpContext.RequestAborted` | ASP.NET endpoint parameter |
| Linked tokens | `CancellationTokenSource.CreateLinkedTokenSource(t1, t2)` for "cancel on either of these" |

> **Reviewer rule** — *every* `await` deeper than the entry point should propagate the token, otherwise cancellation is a lie.

---

## 6. Common Async Pitfalls (and how this codebase avoids them)

### 6.1 `.Result` / `.Wait()`

Forbidden. The CI build runs the AsyncFixer analyser, which fails on `Task.Result` outside `Main` and constructors.

### 6.2 `async void`

Allowed only on UI event handlers (`button_Click`). Never on ViewModel commands or services.

### 6.3 Forgotten `await`

Treated as compile error: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` promotes `CS4014` to an error.

### 6.4 Captured `SynchronizationContext`

Library code uses `ConfigureAwait(false)`. UI code intentionally does **not**, because the continuation must run on the dispatcher thread.

### 6.5 Long-running CPU work inside `async`

`Task.Run(...)` is used **explicitly** when CPU-bound work is wrapped — never implicitly through some hidden helper. The CompatibilityScorer is fast enough that no `Task.Run` is needed.

---

## 7. Reviewer Checklist

- [ ] Every storage-touching method has an `Async` suffix and returns `Task`/`Task<T>`.
- [ ] No `.Result`, no `.Wait()`, no `Task.GetAwaiter().GetResult()` outside `Main`.
- [ ] `[RelayCommand]` annotated methods that perform I/O are `async Task` (not `void`).
- [ ] `Task.WhenAll` is used for independent fan-out; sequential `await` only when ordering matters.
- [ ] `CancellationToken` parameters are propagated end-to-end.
- [ ] Built-in delegates (`Action`, `Func`, `EventHandler`) are preferred over custom `delegate` declarations.
- [ ] Event subscriptions are detached (`-=`) when the subscriber is disposed.

---

## 8. Curriculum Alignment

| Rubric phrase | Realisation |
|---|---|
| "Asynchronous mechanism" | `async`/`await` end-to-end; `Task.WhenAll` fan-out |
| "Using delegates among other things" | `Func<Task>` strategy injection in `NavigateCoreAsync`; `Action<INavigatable>` shell callback |
| "Heavy use of event-driven programming" | `INotifyPropertyChanged`, `event Action?`, `EventHandler<T>` |
| "Server-client data management" | Async repository tier behind ASP.NET endpoints |
| "Multiple clients across platforms" | WPF + Blazor share an async ViewModel layer; future MAUI/iOS/Android plug into the same `ApiClient` |
