# 01 — Core Architecture & Design Patterns

This document describes the architectural philosophy that shapes the codebase: how it positions itself between Object-Oriented Programming, Domain-Driven Design and Event-Driven thinking; how MVVM is realized via the **CommunityToolkit.Mvvm** source generators; how the Service Layer is sliced; and how domain modelling balances classical inheritance with C# 9 records.

---

## 1. Architectural Style — Pragmatic Layered OOP with DDD Influences

MentoringApp is best characterised as a **layered, object-oriented architecture with selective Domain-Driven Design influences**. It is *not* a strict DDD bounded-context implementation, nor a fully event-driven CQRS system.

### Why this position

* **Single ubiquitous language.** The whole solution shares one model project (`MentoringApp.Model`), so terms such as `MentorProfile`, `MenteeProfile`, `PairModel`, `MatchTier` mean the same thing in the WPF view, the Blazor page, the REST contract and the SQL layer.
* **Aggregates are implicit.** `UserModel` is the natural aggregate root for student profiles, and `PairModel` is the aggregate root for the mentor/mentee/supervisor triad. They are not enforced through repositories-per-aggregate, but the model boundaries respect aggregate semantics.
* **No event bus.** The system reacts synchronously through service calls and `Result<T>` returns. The only event-style channels are the MVVM `INotifyPropertyChanged` notifications and a lightweight in-process `INotificationService`.

### How it manifests in code

* **Anaemic-but-validated domain.** Domain types carry data and a small number of pure-domain methods (e.g. `UserModel.IsValidProfilePicture`, `StudentModel.CanHaveMentorProfile()`); behavioural rules that need cross-aggregate access live in the **Service Layer**.
* **Pure model project.** `MentoringApp.Model` references only `CommunityToolkit.Mvvm`. It does not reference repositories, DI, or networking — preserving the integrity of the language layer.

---

## 2. MVVM via CommunityToolkit.Mvvm

The ViewModel layer (`MentoringApp.ViewModel`) and the domain layer (`MentoringApp.Model`) both rely on **`CommunityToolkit.Mvvm` 8.4.0** source generators.

### Why the toolkit

* Eliminates the boilerplate of `INotifyPropertyChanged` plumbing, which previously dominated WPF code-bases.
* Provides `ObservableValidator` for **DataAnnotations**-driven validation that integrates seamlessly with the binding pipeline.
* Generates `IRelayCommand` / `IAsyncRelayCommand` instances at compile time, removing reflective `RelayCommand` allocations and yielding deterministic behaviour for `CanExecute` re-evaluation.

### How it is applied — `LoginViewModel`

```csharp
public partial class LoginViewModel : ObservableValidator, INavigatable
{
    [ObservableProperty] private string _selectedLanguage = "en";
    [ObservableProperty] private bool   _wasCodeSent;

    [ObservableProperty]
    [Required(ErrorMessage = "National ID is required")]
    private string _nationalId = "";

    [RelayCommand]
    private async Task SendVerificationCode()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        // ...
    }

    partial void OnSelectedLanguageChanged(string value)
        => _languageService.ApplyLanguage(value);
}
```

Three distinct generator behaviours coexist in this single class:

1. `[ObservableProperty]` produces the public property + `OnXxxChanged`/`OnXxxChanging` partial hooks.
2. `[RelayCommand]` produces the `SendVerificationCodeCommand` property bound from XAML.
3. `ObservableValidator` exposes `ValidateAllProperties()` and `HasErrors` so the view can react to validation results without reflection at runtime.

### How it is applied — Domain models

The same generators are reused in the domain layer so that view bindings work directly against domain instances:

```csharp
public abstract partial class UserModel : ObservableObject
{
    [ObservableProperty] private string  _userName = string.Empty;
    [ObservableProperty] private string  _profilePicturePath = string.Empty;
    [ObservableProperty] private Gender  _gender = Gender.PreferNoAnswer;

    public string Role => this switch
    {
        AdminModel      => "Admin",
        SupervisorModel => "Supervisor",
        StudentModel s  => s.IsMentor ? "Mentor" : "Mentee",
        _               => "Unknown"
    };
}
```

This is a deliberate departure from "pure DTO" purism: because the WPF and Blazor views need to react to mutations on a logged-in user without translating to a separate ViewModel, the domain class itself is observable. The cost is acceptable because the model project takes the toolkit dependency only — no UI framework is referenced.

---

## 3. Service Layer Separation

The Service Layer (`MentoringApp.Service`) is the **only place where business rules live**. ViewModels orchestrate UI flow, repositories talk to storage; everything in between is a service.

### Why

* Keeping business logic out of ViewModels guarantees parity between WPF and Blazor — both call into the **same** services through the API surface.
* Keeping it out of repositories prevents domain rules from being scattered across storage code, which would make a future migration to PostgreSQL or to a non-SQL store a rewrite rather than a refactor.

### How — service catalogue

| Service | Responsibility |
|---|---|
| `AuthService` | Verification-code issuance, JWT-issuing login flow |
| `UserService` | CRUD over users, profile mutations, role transitions |
| `PairService` | Manage mentor/mentee/supervisor triads |
| `MatchingFlowService` | The 5-tier cascade matching pipeline |
| `CompatibilityScorer` | Pure scoring algorithm (no I/O) used by `MatchingFlowService` |
| `IssueService` / `ReviewService` | Per-pair issue tracking and supervisor reviews |
| `EmailService` | SMTP send-with-retry (singleton; configured from `appsettings.json`) |
| `ExcelImportService` | ClosedXML-based bulk import for students/supervisors |
| `NotificationService` | In-memory pub/sub for UI toast notifications |
| `SettingsService` | Read/write keyed configuration values |
| `DummyDataSeeder` | Deterministic seed data for development & E2E |

### Result Pattern

Service methods communicate failure **declaratively** rather than through exceptions. The `Result<T>` type (`src/MentoringApp.Service/Result.cs`) is the canonical return type:

```csharp
public class Result
{
    public bool Success { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public Dictionary<string, string>? ValidationErrors { get; set; }

    public static Result Ok()                                      => new() { Success = true  };
    public static Result Failure(string message)                   => new() { Success = false, ErrorMessage = message };
    public static Result ValidationFailure(Dictionary<string,string> errors)
        => new() { Success = false, ValidationErrors = errors };
}

public class Result<T> : Result
{
    public T? Data { get; private set; }
    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
}
```

**Why a Result type rather than exceptions**: validation failures are *expected control flow*, not exceptional. Callers can `if (!result.Success) ...` without try/catch ceremony, and validation errors carry a structured per-field dictionary that the UI binds to error templates directly.

---

## 4. Modelling — Inheritance for Identity, Records for Values

The domain mixes class-based inheritance with `record`-typed value carriers.

### Why this split

Inheritance is appropriate when an identity travels through the system over time and its kind is meaningful (an `AdminModel` is not interchangeable with a `StudentModel`). Records are appropriate for ephemeral, immutable bundles of data — request payloads, score envelopes, configuration tuples — where structural equality matters more than identity.

### How — inheritance

`UserModel` is an abstract polymorphic root with three sealed children:

```
UserModel (abstract, ObservableObject)
├── AdminModel          (no extra state)
├── SupervisorModel     (AssignedClasses, Issues, capacity counters)
└── StudentModel        (Grade, ClassNum, MentorProfile?, MenteeProfile?)
```

Polymorphism is preserved across the wire by JSON discriminators declared on the base type:

```csharp
[JsonDerivedType(typeof(StudentModel),    "student")]
[JsonDerivedType(typeof(SupervisorModel), "supervisor")]
[JsonDerivedType(typeof(AdminModel),      "admin")]
public abstract partial class UserModel : ObservableObject { /* … */ }
```

This means a single `/api/users` endpoint can return a heterogeneous list and the client deserialises it into the correct concrete subtype with **`System.Text.Json`** alone — no custom converters.

### How — composition over inheritance for student roles

Instead of `MentorStudent : StudentModel` and `MenteeStudent : StudentModel`, a `StudentModel` carries optional `MentorProfile` and `MenteeProfile`:

```csharp
public partial class StudentModel : UserModel
{
    public MentorProfile? MentorProfile { get; set; }
    public MenteeProfile? MenteeProfile { get; set; }

    public bool IsMentor => MentorProfile is not null;
    public bool IsMentee => MenteeProfile is not null;
}
```

This reflects the real-world rule: the **same student** can be both a mentor for younger pupils and a mentee for advanced subjects. Inheritance would have made that case impossible without multiple-inheritance hacks.

### How — records for value-style data

The codebase uses records / record-like immutable types for:

* API request DTOs in `ApiModels.cs` (`SendCodeRequest`, `LoginRequest`, `CreateUserRequest`, `UpdateMentorProfileRequest`, …)
* `MatchScore` and `MatchTier` value carriers
* `AvailabilitySlot` time windows

Records earn their keep here through structural equality and `with`-expression updates, both useful in the matching pipeline where score collections are filtered and re-projected repeatedly.

---

## 5. Event-Driven Programming — `INotifyPropertyChanged`, Custom Events, and Delegates

The Async-Programming track of the rubric mandates *"heavy use of advanced OOP and event-driven features"*. This codebase satisfies that requirement on three layers, each using a different delegate flavour:

### 5.1 Implicit events — `INotifyPropertyChanged` via the toolkit

Every `[ObservableProperty]`-annotated field in the ViewModel and Model layers expands into a setter that raises `PropertyChanged`. WPF and Blazor bindings subscribe transparently, so a single mutation propagates to dozens of bound controls without imperative refresh code.

> **Note** — `PropertyChanged` is itself a built-in `EventHandler<PropertyChangedEventArgs>` delegate; the toolkit only saves the boilerplate, it does **not** replace the underlying event mechanism.

### 5.2 Explicit `event Action` — navigation surface

The navigation service raises a parameterless built-in delegate to broadcast back-stack mutations:

```csharp
// src/MentoringApp.ViewModel/Navigation/NavigationService.cs
public event Action? CanGoBackChanged;
// ...
CanGoBackChanged?.Invoke();
```

Subscribers (typically shell ViewModels) refresh `IRelayCommand.CanExecute()` for the back-button command. Multicast is implicit — every shell that subscribes is notified.

### 5.3 `Action<T>` — context-bound callbacks

`INavigationService.UseContext(Action<INavigatable>)` accepts a closure that knows how to update *its* surrounding container (a `ContentControl` in WPF, a router in Blazor). The navigation service does not know what the container is; it merely calls the delegate when the current `ViewModel` changes:

```csharp
public IDisposable UseContext(Action<INavigatable> contextSetter)
{
    var store = new NavigationStore();
    Action updateUi = () => contextSetter(store.CurrentViewModel);
    store.CurrentViewModelChanged += updateUi;       // chain delegate to event
    // ...
}
```

> **Note — Why `Action<T>` instead of an interface?** A delegate is the lightest possible polymorphism; defining `IShellHost { void Update(INavigatable) }` would force every shell to implement an interface for a single one-line method.

### 5.4 `Func<Task>` — asynchronous strategy injection

The internal `NavigateCoreAsync<T>` method receives a `Func<Task>` representing *which* `OnNavigatedToAsync` overload to call (parameterless versus parameterised). The same pipeline therefore handles both navigation kinds:

```csharp
private async Task NavigateCoreAsync<TViewModel>(
    TViewModel vm, Func<Task> onNavigatedTo, bool clearHistory = false)
    where TViewModel : class, INavigatable
{
    // ...
    await onNavigatedTo();
    CanGoBackChanged?.Invoke();
}
```

> **Code reviewer note** — this is the cleanest pattern for "do mostly the same thing, but call a different async hook at the end". An `if (parameter is null) call A else call B` would force `NavigateCoreAsync` to know about the parameter shape.

### 5.5 Custom delegates — fakes for tests

`MentoringApp.ViewModel.Tests` injects fakes through a custom delegate signature:

```csharp
private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        => Task.FromResult(handler(req));
}
```

The captured `Func<HttpRequestMessage, HttpResponseMessage>` is the test seam: tests register an arbitrary lambda, the ViewModel calls real `ApiClient` code, and assertions run against the recorded request.

### 5.6 The pattern matrix

| Mechanism | Delegate type | Where used | Why |
|---|---|---|---|
| `INotifyPropertyChanged` | `PropertyChangedEventHandler` | ViewModels, domain models | Implicit binding refresh |
| `event Action?` | Built-in parameterless | `NavigationService.CanGoBackChanged`, `StoreBase` | Multicast notification |
| `Action<T>` | Built-in single-arg | Shell context callback | Lightweight strategy injection |
| `Func<Task>` | Built-in async | Navigation pipeline | Async strategy without virtuals |
| `Func<TIn, TOut>` | Built-in | Test fakes | Inline lambda swaps |
| `EventHandler<T>` | Built-in | Toast service, store mutations | CLR-canonical event signature |

> **Reviewer note** — the codebase deliberately avoids declaring **custom** named delegate types (`public delegate void MyXxxHandler(...)`). Built-in `Action`/`Func`/`EventHandler` cover every scenario without polluting the namespace, and they compose naturally with LINQ and async.

---

## 6. Putting it together

A typical write-side request now flows like this:

1. The view raises a `[RelayCommand]` on a `partial` ViewModel.
2. The ViewModel composes a record-style request and calls a typed `ApiClient` method.
3. The `Api` host routes the call to a service; the service runs validators, invokes mappers, calls repositories, and returns a `Result<T>`.
4. The ViewModel consumes the `Result<T>`, populates observable state, and the view re-renders without imperative refresh code.

Each layer keeps the next one ignorant of the layers below it. That is the central architectural promise of this codebase.

---

## 7. Curriculum Alignment

| Rubric concept | Realisation |
|---|---|
| OOP (mandatory #7) | `UserModel` polymorphic hierarchy + composition for student profiles |
| Inheritance | `AdminModel`/`SupervisorModel`/`StudentModel : UserModel` |
| Event-driven programming | `INotifyPropertyChanged` toolkit + `event Action?` callbacks |
| Delegates (Async track) | `Func<Task>`, `Action<T>`, `Func<TIn, TOut>` across navigation, stores, test seams |
| Validation classes (extension §10) | `UserValidator`, `MentorProfileValidator` (FluentValidation) |
| Service layer (Network Services track) | `*Service` classes — see §3 |
