# 02 — Data Persistence & Abstraction

This document describes how the MentoringApp persistence layer is **abstracted from any single database engine**, how the storage model is decoupled from the domain model through Data Access Objects (DAOs), and how mapping and validation responsibilities are sliced between the Service and Data layers.

---

## 1. Database-Agnostic Repository Pattern

The architecture is engine-agnostic by design. The Service layer never references `Microsoft.Data.Sqlite`, `Npgsql`, or `EntityFrameworkCore`; it speaks exclusively to repository **interfaces** declared in `MentoringApp.Data/Interfaces/`.

### Why

* **Pluggable storage.** A future migration to PostgreSQL or to an EF-Core-based stack only needs to provide a new `Sql*Repo` (or `Pg*Repo`) implementation; the Service layer is untouched.
* **Testability.** Repositories are trivially mockable for unit tests because they expose narrow, intention-revealing interfaces rather than `DbContext` or `IQueryable<T>`.
* **Migration insurance.** The DI bootstrap is a single funnel where the engine choice is concentrated, so the migration risk is bounded.

### How — interface catalogue

`src/MentoringApp.Data/Interfaces/Interfaces.cs` declares one interface per aggregate:

| Interface | Responsibility |
|---|---|
| `IDbRepo` | Schema lifecycle (`Recreate`, migration helpers) |
| `IUserRepo` | Identity, profile pictures, language, role-specific upserts |
| `IPairRepo` | Mentor/mentee/supervisor triads and "incomplete profile" flags |
| `IIssueRepo` / `IIssueCategoryRepo` | Issue lifecycle and forwarding |
| `IReviewRepo` | Per-pair review history |
| `IPairRequestRepo` | Pending mentee→mentor requests with tier annotations |
| `IMatchScoreRepo` | Bulk insert + top-N retrieval of compatibility scores |
| `IGradeRepo` / `ISubjectRepo` / `ISchoolClassRepo` | Reference data |
| `ISettingsRepo` | Key/value runtime settings |
| `IVerificationCodeRepo` | Throwaway codes for the email-based login flow |

Each repository surface is **task-oriented** (`GetMatchedMenteeIdsAsync`, `MarkIssueResolvedAsync`) rather than CRUD-shaped. This keeps the abstraction useful: the service layer never has to compose four primitive operations to accomplish a single intent.

### How — DI funnel

The whole storage choice lives in `src/MentoringApp.Data/DI/DataDepedencyInjection.cs`:

```csharp
public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=../Data/Resources/Database/mentoring.db";
    return services.AddSqlDataRepositories(connectionString);
}

public static IServiceCollection AddSqlDataRepositories(this IServiceCollection services, string connectionString)
{
    services.AddSingleton<ISQLiteConnectionService>(_ => new SQLiteConnectionService(dbPath));
    services.AddSingleton<IUserRepo>(sp =>
        new SqlUserRepo(sp.GetRequiredService<ISQLiteConnectionService>(), connectionString));
    services.AddScoped<IPairRepo>(sp =>
        new SqlPairRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
    // …
}
```

Adding a Postgres backend means adding a new extension method (`AddPgDataRepositories`) and a new branch in `AddDataLayer`. No code outside `MentoringApp.Data` changes.

---

## 2. Storage Modelling — DAOs Distinct from Domain Models

`MentoringApp.Data/Dao/` defines a parallel hierarchy: `UserDao`, `PairDao`, `IssueDao`, `ReviewDao`, `MatchScoreDao`, `SupervisorStatsDao`, etc. The Repository layer **only ever exposes domain models** to its callers; DAOs never escape the project.

### Why two shapes

* The relational shape and the domain shape have different optimisation pressures. A `UserDao` is a flat row joining `Users` with role-specific tables (`UserStudents`, `UserMentors`, …). A `UserModel` is a polymorphic object graph with optional sub-profiles.
* Decoupling shields the domain from accidental schema-driven changes (e.g. a column rename should not propagate to ViewModels).

### How — vertical partitioning at the table layer

The SQLite schema is **vertically partitioned by role** so that each table holds only the columns that role actually uses:

```
Users          (id, email, nationalId, userName, language, gender, …)
UserStudents   (userId FK, gradeId, classNum)
UserMentors    (userId FK, subjectToTeach, maxMentees)
UserMentees    (userId FK, subjectNeedingHelp, hoursPerWeek)
UserSupervisors(userId FK, …)
UserAdmins     (userId FK)
```

`SqlUserRepo` joins these with `LEFT JOIN`s into a single `UserDao`, then constructs the appropriate `UserModel` subtype before returning to the service layer.

### How — reflection-driven row mapping

`SQLiteConnectionService` (`src/MentoringApp.Data/Repository/SQLite/ConnectionsService/`) hides the ADO.NET ceremony behind a tiny generic API:

```csharp
public T QuerySingle<T>(string sql, object parameters = null) where T : new();
public List<T> Query<T>(string sql, object? parameters = null) where T : new();
public int Execute(string sql, object? parameters = null);

private static T MapReaderToObject<T>(SqliteDataReader reader) where T : new()
{
    // Case-insensitive name match between columns and writable properties.
    // DateTime columns are stored as ISO-8601 strings; enums as integers;
    // Nullable<T> is handled explicitly.
}
```

Reflection is used only on the inbound mapping path; SQL is hand-authored throughout. This intentionally avoids the cost and complexity of a full ORM while still saving the per-property boilerplate that ADO.NET demands.

---

## 3. Mappers — DAO ⇄ Model Translation

Mappers live in `src/MentoringApp.Service/Mapping/`. They are pure, stateless conversion functions:

```csharp
public static class IssueMapper
{
    public static IssueModel ToModel(IssueDao dao, IssueCategoryModel category)
        => new IssueModel
        {
            Id          = dao.Id,
            Description = dao.Description,
            Category    = category,
            ReporterId  = dao.ReporterId,
            IsResolved  = dao.IsResolved,
            ForwardedToSupervisorId = dao.ForwardedToSupervisorId
        };

    public static IssueDao ToDao(IssueModel model) => new IssueDao { /* … */ };
}
```

### Why mappers in the **Service** project rather than the Data project

Because mapping a DAO to a domain model often requires **enrichment** that the data layer cannot supply — e.g. an `IssueDao` carries a `categoryId` integer, but the domain `IssueModel` carries a fully-resolved `IssueCategoryModel`. The service layer has access to other repositories and reference caches, so this is the correct seam for translation.

---

## 4. Validators

`src/MentoringApp.Service/Validator/` hosts FluentValidation rule classes. The flagship is `UserValidator`:

```csharp
public class UserValidator : AbstractValidator<UserModel>
{
    public UserValidator()
    {
        RuleFor(u => u.Email).NotEmpty().EmailAddress();
        RuleFor(u => u.UserName).NotEmpty().MaximumLength(60);
        RuleFor(u => u.NationalId).NotEmpty().Matches(@"^\d{6,12}$");

        RuleFor(u => u).ChildRules(c =>
            c.When(x => x is StudentModel s && s.IsMentor,
                () => c.RuleFor(x => ((StudentModel)x).MentorProfile)
                       .NotNull()
                       .SetValidator(new MentorProfileValidator())));
    }
}
```

### Why FluentValidation rather than DataAnnotations alone

DataAnnotations are the right tool **inside ViewModels** where the binding pipeline is expecting them (the `LoginViewModel` uses them for live form validation). FluentValidation is the right tool **server-side** because rules across multiple properties (e.g. "if `IsMentor` is true, `MentorProfile` must be present") are expressed naturally as conditional `RuleFor` chains.

The service layer registers the validator as **Singleton** (it is stateless) and feeds its result into `Result.ValidationFailure(…)` so the entire failure mode is consistent with the rest of the service contract.

---

## 5. Configuration in Flat Files

There is no configuration database. Operational settings live in **flat JSON files** read at host startup:

| Host | File | Sections |
|---|---|---|
| WPF | `src/MentoringApp.Desktop/appsettings.json` | `ApiSettings.BaseUrl` |
| API | `src/MentoringApp.Api/appsettings.json` | `ConnectionStrings`, `JwtSettings`, `EmailSettings`, `AllowedOrigins`, `Logging`, `DataProvider` |
| Web | `src/MentoringApp.Web/appsettings.json` | `ApiSettings.BaseUrl` |

```csharp
_configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
```

### Why flat files

* **Boot-time determinism.** The host can fail fast on a missing or malformed configuration before any DI registration runs.
* **Per-environment overrides.** The standard ASP.NET Core convention `appsettings.{Environment}.json` is honoured, allowing developer-specific connection strings without source control noise.
* **No bootstrap circularity.** Storing email/SMTP credentials in the database would create a chicken-and-egg problem where the database needs to be reachable before the email subsystem can warn about an unreachable database.

User-facing **runtime** settings (matching weights, retention windows, …) are stored in the database via `ISettingsRepo` and `SettingsService.GetDoubleAsync(...)` / `SetDoubleAsync(...)`. The split is intentional: things that affect *how the host boots* live in JSON; things that affect *how the business behaves* live in SQL.

---

## 6. Putting it together — the canonical write path

```
Service.UpdateMentorProfileAsync(userId, request)
    │
    ├── UserValidator.Validate(model)                          ← FluentValidation
    │     └─ on failure → return Result.ValidationFailure(...)
    │
    ├── userRepo.UpdateMentorProfileAsync(UserMapper.ToDao(model))
    │     └─ SqlUserRepo:
    │         - SQLiteConnectionService.ExecuteAsync(UPSERT INTO UserMentors …)
    │
    └── return Result.Ok()
```

Each layer knows only its immediate neighbour; no business rule lives in SQL, no SQL leaks into the domain.
