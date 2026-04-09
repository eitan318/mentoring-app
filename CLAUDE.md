# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build the entire solution
dotnet build MentoringApp.sln

# Run the WPF application
dotnet run --project MentoringApp/MentoringApp.csproj

# Build a specific project
dotnet build Data/MentoringApp.Data.csproj
```

There are no automated tests in this repository. The app targets `net9.0-windows` and only runs on Windows (WPF).

## Architecture

5-project layered solution: **View → ViewModel → Service → Repository → SQLite**

| Project | Role |
|---|---|
| `MentoringApp` | WPF entry point, DI bootstrapping, Views, Styles, Converters, Localization |
| `MentoringApp.ViewModel` | MVVM ViewModels, navigation service, global user state |
| `MentoringApp.Service` | Business logic, matching algorithm, email, Excel import |
| `MentoringApp.Model` | Domain models (UserModel hierarchy, Pair, Issue, Review, etc.) |
| `MentoringApp.Data` | Repository pattern over raw ADO.NET/SQLite, DTOs |

### Key architectural points

**Navigation** — Stack-based with `INavigationService`. ViewModels implement `INavigatable` (with `OnNavigatedTo`/`OnNavigatedFrom` lifecycle hooks) and optionally `IClosable`. Views are mapped to ViewModels in `MentoringApp/Styles/ViewModelViewMap.xaml`.

**DI** — Full `Microsoft.Extensions.DependencyInjection` container wired in `App.xaml.cs`. Repositories and services are registered as Scoped; ViewModels as Transient (fresh instance per navigation); `UserStore` and `INavigationService` as Singletons. View-layer services (`IWindowService`, `IFileService`, `ILanguageService`) are Singleton and abstracted so ViewModels don't depend on WPF directly.

**User model hierarchy** — `UserModel` (abstract) → `StudentModel`, `AdminModel`, `SupervisorModel`. `StudentModel` has a `MentorProfile` or `MenteeProfile` depending on role.

**Data access** — No ORM. Each entity has a typed interface (`IUserRepo`, `IPairRepo`, etc.) implemented by `SqlXxxRepo` classes using `SQLiteConnectionService` for connection management. DTOs (`UserDto`, `PairDto`, etc.) are used between the data and service layers.

**Result pattern** — Services return `Result<T>` for explicit success/failure signaling instead of exceptions.

**Matching pipeline** — `MatchingFlowService` implements a 5-tier cascade: (1) Direct mentee requests, (2) Auto-matching via `CompatibilityScorer`, (3) Supervisor-assisted, (4) Admin manual creation, (5) Profile-incomplete pairs flagged for review.

### Configuration

`MentoringApp/appsettings.json` contains:
- `ConnectionStrings` — path to SQLite DB at `Data/Resources/Database/mentoring.db`
- `EmailSettings` — Gmail SMTP credentials for verification code emails

### Database

SQLite database lives at `Data/Resources/Database/mentoring.db`. On startup, `App.xaml.cs` has a `recreateInitialDb` bool (currently `true`) that drops and recreates the DB via `IDbRepo.Recreate()` then calls `DummyDataSeeder.SeedAsync()`. The DB is checked into git (`mentoring.db` is tracked).

### Localization

Two locales supported: English (`Strings.resx`) and Hebrew (`Strings.he.resx`). `TranslationSource` is a singleton with an indexer used in XAML bindings:
```xml
Content="{Binding [Key_Name], Source={x:Static loc:TranslationSource.Instance}}"
```
Hebrew is RTL — `TranslationSource.FlowDirection` is bound to window/panel `FlowDirection`. To add a new string, add it to both `.resx` files.

### View organization

Views under `MentoringApp/View/` are split by role: `Admin/`, `Student/`, `Supervisor/`, `User/`. ViewModels under `ViewModel/ViewModelPage/` follow the same role-based folder structure.
