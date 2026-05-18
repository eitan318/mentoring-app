# 05 — Engineering Excellence & Tooling

This document covers the cross-cutting engineering choices that make the codebase maintainable: the **Dependency Injection container**, the **directory structure** that enforces project independence, the use of advanced C# features (delegates, reflection, source generators) and selected NuGet packages, and the **testing strategy** spanning unit, ViewModel and end-to-end tiers.

---

## 1. Dependency Injection

The codebase uses `Microsoft.Extensions.DependencyInjection` exclusively. There is one `IServiceCollection` per host process; each layer exposes an `Add*` extension method that the host composes.

### Why a layered DI bootstrap

* **Host-agnostic registration.** `AddServices` is identical whether called from the API host, the WPF host, or a test host. Each host then layers its own concerns on top.
* **Single source of lifetime truth.** Each registration line is the canonical statement of how long an instance lives. There is no `[Injectable]`-style attribute scattered through the codebase.
* **Composable swap-in.** The web host inherits the desktop bootstrap and overrides only the `INavigationService` (see [03-navigation-and-ui.md](03-navigation-and-ui.md)).

### How — the host bootstraps

```csharp
// src/MentoringApp.Desktop/App.xaml.cs (excerpt)
public App()
{
    _configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();

    var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7233";

    var services = new ServiceCollection();
    services.AddViewModels(apiBaseUrl);   // ViewModel + ApiClient
    services.AddView();                   // WPF-only IViewService implementations

    _serviceProvider = services.BuildServiceProvider();
}
```

```csharp
// src/MentoringApp.Api/Program.cs (excerpt)
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(/* … */);
```

Each `Add*` extension is a single composition function exposed by its layer.

### Lifetime contract

| Component | Lifetime | Rationale |
|---|---|---|
| `UserStore`, `AuthTokenStore`, `INavigationService`, `SessionService` | Singleton | Cross-cutting, ambient state used by every ViewModel |
| `ILanguageService`, `IWindowService`, `IFileService`, `IToastService` | Singleton | Wrappers around process-level OS resources |
| `EmailService`, `UserValidator` | Singleton | Stateless or immutable host-level configuration |
| `IUserRepo`, `IGradeRepo`, `ISubjectRepo`, `IDbRepo`, `ISQLiteConnectionService` | Singleton | Reference-data heavy; SQLite handles per-call connection pooling internally |
| `IPairRepo`, `IIssueRepo`, `IReviewRepo`, `ISettingsRepo`, `IPairRequestRepo`, `IMatchScoreRepo` | Scoped | Per-request work, fits the API request-scope model |
| All Services (`AuthService`, `UserService`, `PairService`, …) | Scoped | One service instance per request, share repositories within a request |
| All ViewModels | Transient | Fresh instance per navigation, avoiding stale state from prior visits |
| Typed `*ApiClient`s | Per `HttpClient` factory rules (effectively transient with pooled handler) | Standard `IHttpClientFactory` semantics |

### Why ViewModels are Transient

Without a fresh instance, a user re-entering the **same** screen would see leftover state — selected tab, scroll position, validation errors, partially-filled forms — from the previous visit. Transient registration in concert with the navigation stack guarantees a clean ViewModel for every visit, while shared state still lives in singletons (`UserStore`, language, theme).

---

## 2. Directory Structure & Project Independence

The solution is intentionally split into **eight production projects** plus three test projects. Project references are minimal and unidirectional; circular references are prevented at compile time.

### The reference graph

```
Model ◄────── Data ◄────── Service ◄────── Api
  ▲                                         
  └────── ApiClient ◄────── ViewModel ◄────── Desktop (WPF)
                                         ◄────── Web (Blazor)
```

Notable invariants:

* **Model is a leaf.** Nothing in `MentoringApp.Model` references any other project in the solution. It depends on `CommunityToolkit.Mvvm` only.
* **ViewModel does not reference Service or Data.** It reaches the back end exclusively through `ApiClient`. This is the rule that lets the same ViewModel binary execute under WPF and Blazor.
* **Desktop and Web do not reference Service or Data either.** They are pure shells. Reaching SQL from a XAML code-behind is a compile error.
* **Api is the only project that references both Service and Data.** It is the host that composes the back-end stack.

### Why these invariants matter

* **Migration cost is bounded.** Replacing the Web shell, swapping SQLite for PostgreSQL, or introducing a mobile client (MAUI, Avalonia) becomes a project-scoped change, not a refactor across the whole codebase.
* **Build-time enforcement.** The compiler refuses to build a solution where a XAML view tries to import a repository. Documentation cannot be ignored; project references cannot.

### Directory conventions inside a project

* `View/` is split by role (`Admin/`, `Supervisor/`, `Student/`, `User/`); ViewModels mirror that structure under `ViewModel/ViewModel/`.
* `IViewService/` defines View-layer abstractions (`IWindowService`, `IFileService`) that ViewModels can call without dragging in WPF types.
* `Localization/` contains `Strings.resx` and `Strings.he.resx` plus `TranslationSource.cs`.

---

## 3. Advanced C# Features in Use

### Source generators

`CommunityToolkit.Mvvm` (8.4.0) source generators expand `[ObservableProperty]`/`[RelayCommand]`/`[NotifyCanExecuteChangedFor]` at compile time. Generated artefacts ship inside the assembly without runtime reflection — observability is effectively free of overhead.

### Reflection

Used **deliberately** and in only two places:

1. **`SQLiteConnectionService.MapReaderToObject<T>`** — column→property mapping. Reflection is used inbound only, so SQL bug surface is small.
2. **`TranslationSource[key]`** — `ResourceManager.GetString(key, culture)` looks up resx entries by name. Reflection is the only way `ResourceManager` works; it is used carefully behind a singleton.

Reflection is **not** used for command binding, validation or DI. Those concerns are handled by source generators or first-class container APIs.

### Delegates

* `Action<INavigatable>` is the contract by which a navigation context updates its shell (see `INavigationService.UseContext` in [03](03-navigation-and-ui.md)).
* `Func<HttpRequestMessage, HttpResponseMessage>` is the test seam for fake HTTP handlers in `MentoringApp.ViewModel.Tests`.
* `event Action? CanGoBackChanged` notifies shells about back-stack mutations without a pub/sub framework.

### Pattern matching

Used pervasively for role discrimination on the `UserModel` hierarchy:

```csharp
public string Role => this switch
{
    AdminModel       => "Admin",
    SupervisorModel  => "Supervisor",
    StudentModel { IsMentor: true } => "Mentor",
    StudentModel                    => "Mentee",
    _                               => "Unknown"
};
```

Property patterns (`{ IsMentor: true }`) keep the discriminator readable and exhaustive.

### JSON polymorphism

`[JsonDerivedType]` attributes on `UserModel` enable polymorphic (de)serialisation across the wire without a custom converter — see [01](01-design-patterns.md).

### Selected NuGet packages

| Package | Version | Where used | Purpose |
|---|---|---|---|
| `CommunityToolkit.Mvvm` | 8.4.0 | Model, ViewModel | INPC + RelayCommand source generators |
| `Microsoft.Extensions.DependencyInjection` | net9.0 | All hosts | DI container |
| `Microsoft.Extensions.Configuration.Json` | net9.0 | Hosts | `appsettings.json` binding |
| `Microsoft.Data.Sqlite` | net9.0 | Data | Raw ADO.NET driver |
| `FluentValidation` | 12.1.1 | Service | Cross-property validation rules |
| `ClosedXML` | 0.105.0 | Service | Excel import / template generation |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.4 | Api | JWT bearer authentication |
| `Microsoft.Playwright` | latest | E2eTests | Headless browser automation |
| `xunit`, `FluentAssertions` | latest | Tests | Unit-test framework + readable assertions |

---

## 4. Testing Strategy

The solution maintains three test projects, each targeting a distinct level of the architecture.

### Tier 1 — `MentoringApp.Tests`

* **Scope.** Service, Mapping, Validator and Data layers.
* **Style.** Pure unit tests. Where a service depends on a repository, a stub or mock implementation is provided.
* **Why this layer first.** Business rules carry the highest defect cost and the lowest test-setup cost. Validators and the matching algorithm in particular are fully covered here; they are pure functions with no I/O.

### Tier 2 — `MentoringApp.ViewModel.Tests`

* **Scope.** ViewModel orchestration: navigation, store mutation, command flows, error propagation, language persistence.
* **Style.** Each test instantiates a real ViewModel against fake `HttpClient`s and a real `NavigationService`. Network responses are produced by a small `FakeHttpMessageHandler`:

  ```csharp
  private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
      : HttpMessageHandler
  {
      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
          => Task.FromResult(handler(req));
  }
  ```

  This is the seam that lets us assert ViewModel behaviour deterministically without a running API.
* **Why a separate project.** The dependency surface is different from the service tests (it brings in `MentoringApp.ViewModel` and `MentoringApp.ApiClient`), and the failure modes worth catching are different (binding glitches, navigation regressions, store corruption).

### Tier 3 — `MentoringApp.E2eTests`

* **Scope.** End-to-end flows over the live API + Web stack, driven by Playwright.
* **Configuration.** `tests/MentoringApp.E2eTests/TestConfig.cs` reads URLs and seeded test identities from environment variables:

  ```csharp
  public static string WebBaseUrl     => Environment.GetEnvironmentVariable("E2E_WEB_URL")     ?? "http://localhost:5041";
  public static string ApiBaseUrl     => Environment.GetEnvironmentVariable("E2E_API_URL")     ?? "http://localhost:5035";
  public static string AdminNationalId=> Environment.GetEnvironmentVariable("E2E_ADMIN_NATIONAL_ID") ?? "100";
  public const  string LocalStorageKey = "mentoring_jwt";
  ```

* **Why E2E for the Web client only.** The desktop client is not amenable to headless automation; equivalent coverage at the desktop tier is provided by ViewModel tests because the WPF view layer is a thin XAML shell over the same ViewModels exercised in Tier 2.

### Test execution

```bash
dotnet test tests/MentoringApp.Tests/MentoringApp.Tests.csproj
dotnet test tests/MentoringApp.ViewModel.Tests/MentoringApp.ViewModel.Tests.csproj
dotnet test tests/MentoringApp.E2eTests/MentoringApp.E2eTests.csproj    # requires API + Web running
```

The first two are run on every pull request. The E2E suite is run nightly and on release branches because it requires a provisioned API + database.

### Coverage philosophy

* **Validators and pure algorithms** (e.g. `CompatibilityScorer`, `MatchingFlowService` decision branches): exhaustive.
* **ViewModel command flows** that mutate stores or navigate: each non-trivial branch has a test.
* **Repositories**: smoke-level integration tests against an in-memory SQLite database; the goal is to catch SQL syntax errors and column-mapping regressions, not to re-test SQLite itself.
* **HTTP shape**: covered transitively by ViewModel tests; we do not test `ApiClientBase` in isolation because every test that exercises a real ViewModel flow also exercises it.

---

## 5. Async Programming Tooling (Async Track — primary requirement)

The Async-Programming track of the rubric demands more than `async`/`await` keywords sprinkled in handlers; it requires a **coherent async pipeline** end-to-end. This section catalogues the tools and rules that make the pipeline coherent.

### 5.1 Async surface contracts

Every method that crosses an I/O boundary returns `Task` or `Task<T>`. Synchronous wrappers exist only at the *outer-most* edge (DI bootstrap, `Main`).

| Layer | Sync? | Why |
|---|---|---|
| `View` event handlers | Sync entry; awaits inside | XAML/Razor event signatures |
| `ViewModel` `[RelayCommand]` | `async Task` | Source generator chooses async overload automatically |
| `ApiClient` | `async Task<T>` | Wraps `HttpClient` async APIs |
| `Service` | `async Task<Result<T>>` | Composes repositories + validators |
| `Repository` | `async Task<T>` | Wraps `SqliteDataReader.ReadAsync` etc. |

> **Rule** — never call `.Result` or `.Wait()`. The codebase compiles with `<TreatWarningsAsErrors>` enabled and the Roslyn analyser CA2007/CA1849/AsyncFixer rule set, so a sync-over-async violation fails the build.

### 5.2 `Task.WhenAll` for fan-out

Independent I/O is parallelised:

```csharp
var studentTasks    = students.Select(s => _emailService.SendEmailAsync(...));
var supervisorTasks = supervisors.Select(sv => _emailService.SendEmailAsync(...));
var results         = await Task.WhenAll(studentTasks.Concat(supervisorTasks));
```

> **Reviewer note** — `Task.WhenAll` rethrows the *first* exception it observes. If the caller needs to know about all failures, iterate the returned tasks afterwards and inspect `task.IsFaulted`.

### 5.3 `IAsyncRelayCommand` and `[RelayCommand]`

The toolkit's source generator produces an `IAsyncRelayCommand` whenever the annotated method is `async Task`:

```csharp
[RelayCommand]
private async Task SendVerificationCode()
{
    ValidateAllProperties();
    if (HasErrors) return;
    var result = await _auth.SendCodeAsync(new SendCodeRequest(NationalId));
    // ...
}
```

The generated command exposes:

- `IsRunning` — bound to a "Sending…" `Visibility`/`IsEnabled` state in XAML.
- `CancelCommand` — automatically generated when the method accepts a `CancellationToken`.

> **Reviewer note** — `IsRunning` is the canonical anti-double-click guard; never re-implement it manually.

### 5.4 `CancellationToken` propagation

Long-running flows accept a `CancellationToken` parameter and pass it down to every awaiter:

```csharp
public async Task<IEnumerable<MatchScore>> ScoreAllAsync(CancellationToken ct = default)
{
    var users = await _userService.GetAllUsersAsync(ct);
    return await Task.WhenAll(users.Select(u => ScoreOneAsync(u, ct)));
}
```

> **Where the tokens come from** — `IAsyncRelayCommand` injects a token tied to its `CancelCommand`; ASP.NET endpoints inject `HttpContext.RequestAborted`. Both are propagated end-to-end.

### 5.5 Async + delegates — the prescribed combination

The Async-Programming rubric line reads: *"server-side data handling based on an asynchronous mechanism that uses delegates, among other things"*. The codebase satisfies this on three different combinations:

1. **`Func<Task>` as an async strategy** — `NavigationService.NavigateCoreAsync<T>(vm, onNavigatedTo: () => vm.OnNavigatedToAsync())`.
2. **`Action<T>` as a sync continuation in an async pipeline** — `INavigationService.UseContext` callback.
3. **`Func<HttpRequestMessage, HttpResponseMessage>` for test seams** — `FakeHttpMessageHandler`.

See the dedicated chapter [`06-async-and-delegates.md`](06-async-and-delegates.md).

### 5.6 Common pitfalls (and how the codebase avoids them)

| Pitfall | Avoided by |
|---|---|
| Sync over async (`.Result`) | Analyser rule + reviewer policy |
| Capturing `SynchronizationContext` in libraries | All library methods use `ConfigureAwait(false)` |
| Async `void` event handlers | Only the outer-most XAML event handler is `async void`; everything inside awaits `Task` |
| Forgetting to `await` `Task.WhenAll` | Build warning `CS4014` is treated as error |

---

## 6. Closing Notes

The engineering choices reinforce one another. Strict project layering makes the DI bootstrap obvious; the DI bootstrap makes lifetime contracts explicit; explicit lifetimes make tests fast and deterministic; deterministic tests make refactors cheap. The codebase is structured so that the **next** architectural change — Postgres, MAUI, gRPC — can be implemented behind an existing seam rather than across the whole solution.

---

## 7. Curriculum Alignment

| Rubric concept | Realisation | Section |
|---|---|---|
| Advanced OOP w/ inheritance (mandatory #7) | UserModel hierarchy, ValueConverters via `IValueConverter` | §3 |
| Source generators / advanced C# | `[ObservableProperty]`, `[RelayCommand]`, pattern matching | §3 |
| Async server programming (Async track) | `Task`-typed method surface, fan-out, cancellation | §5 |
| Multi-platform clients (Async track) | WPF + Web share ViewModels via DI swap | §2 |
| Testing (project quality) | Three-tier test pyramid: Service / ViewModel / E2E | §4 |
