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

## 6. Database Normalisation — Satisfying Mandatory Requirement #4

The rubric's mandatory requirement #4 demands "a normalised database (≥4 information tables for a data-driven system, with at least one junction/relationship table)". The schema satisfies this on three axes.

### 6.1 Vertical partitioning — "one role, one table"

Already described in §2; the `UserStudents` / `UserMentors` / `UserMentees` / `UserSupervisors` / `UserAdmins` set is a textbook example of **third-normal-form vertical partitioning** — every column lives in the table that "owns" it, no NULLs are wasted on roles that the row is not.

### 6.2 Horizontal partitioning — reference data

| Reference table | Holds | Joined by |
|---|---|---|
| `Grades` | (`Id`, `Name`, `Num`) | `UserStudents.GradeId`, `SchoolClasses.GradeId` |
| `Subjects` | (`Id`, `Name`) | `UserMentors.SubjectId`, `UserMentees.SubjectId` |
| `IssueCategories` | (`Id`, `Name`) | `Issues.CategoryId` |
| `SchoolClasses` | (`GradeId`, `ClassNum`, `SupervisorId`) | many-to-one with `UserSupervisors` |

Reference data is loaded once at startup and cached in repository singletons (see §1) to keep join chains cheap.

### 6.3 Junction tables — explicit M:N relationships

The schema models *true* M:N relationships through dedicated link tables, never through comma-separated columns.

| Junction table | Relates | Extra columns | Purpose |
|---|---|---|---|
| `Pairs` | `Mentor ↔ Mentee ↔ Supervisor` (3-way) | `CreatedAt`, `IsIncomplete` | A confirmed mentoring triad |
| `PairRequests` | `Mentee → Mentor` | `Tier`, `RequestedAt`, `Status` | Pending mentee→mentor request |
| `MatchScores` | `Mentor ↔ Mentee` | `Score` | Cached compatibility score |
| `IssueForwarding` (column on `Issues`) | `Issue → Supervisor` | — | Forwarding lifecycle |

> **Code-reviewer note** — `Pairs` is a *3-way* junction. ER theory often introduces an artificial "PairId" surrogate to keep operations simple; this codebase does the same (`Pairs.Id INTEGER PRIMARY KEY`), which simplifies foreign-key references from `Issues` and `Reviews`.

### 6.4 Normalisation traps that were avoided

- **No CSV columns** — tags, subjects, classes are all link rows.
- **No JSON blobs in the domain tables** — settings live in `Settings(Key, Value)` only.
- **No mixed-meaning enums in strings** — gender, role, match tier are stored as integers and parsed in C#.

---

## 7. SQL Injection Prevention (extension §10 — `Injection Sql`)

The rubric's section 10 explicitly calls out *"Protection against database hacks via SQL Injection — using parameters"*. Every `SqlXxxRepo` builds queries with **named parameters** that the SQLite driver binds positionally; user-controlled strings are never concatenated into SQL.

### 7.1 The parameter funnel

```csharp
// src/MentoringApp.Data/Repository/SQLite/ConnectionsService/SQLiteConnectionService.cs
private static void AddParameters(SqliteCommand cmd, object? parameters)
{
    if (parameters is null) return;
    foreach (var prop in parameters.GetType().GetProperties())
    {
        cmd.Parameters.AddWithValue("@" + prop.Name, prop.GetValue(parameters) ?? DBNull.Value);
    }
}
```

> **Why anonymous-object parameters?** A caller writes `_db.QuerySingleAsync<UserDao>(sql, new { UserId = id })`, which:
> 1. Names parameters by the property name, mirroring `@UserId` in the SQL string.
> 2. Passes them through `SqliteCommand.Parameters.AddWithValue`, which performs **type-safe binding**, not string substitution.
> 3. Forces every callsite into the parameter pipeline — there is no overload that accepts a raw, formatted SQL string.

### 7.2 Concrete example

```csharp
// src/MentoringApp.Data/Repository/SQLite/SqlUserRepo.cs
private async Task<bool> IsAdmin(int userId)
{
    var row = await _db.QuerySingleAsync<CountRow>(
        "SELECT COUNT(1) AS Count FROM UserAdmins WHERE UserId = @UserId",
        new { UserId = userId });
    return row != null && row.Count > 0;
}
```

`userId` cannot influence SQL grammar — even if a malicious caller passed `'; DROP TABLE …`, SQLite would treat the entire string as the value bound to `@UserId`.

### 7.3 LIKE-clause hardening

Wildcard search uses parameter binding *plus* explicit wildcard concatenation in the parameter value, never in the SQL:

```sql
-- correct
WHERE UserName LIKE @Q

-- bound as
new { Q = $"%{userInput}%" }
```

The SQL string remains constant; the wildcard is part of the bound *value*.

### 7.4 Reviewer checklist

- [ ] No string interpolation builds SQL — `$"SELECT … {userInput}"` is forbidden.
- [ ] Every `cmd.CommandText` is a string literal or a `const string`.
- [ ] Every variable in a query has a matching `@Name` placeholder.
- [ ] User-supplied wildcards are concatenated to the **parameter value**, not the SQL.

---

## 8. Asynchronous Database Access (Async Track, mandatory)

The rubric's Async-Programming track demands *"asynchronous data handling on the server, using delegates among other things"*. Every repository method exposes a `Task`-returning variant powered by ADO.NET's async APIs.

### 8.1 The async surface

```csharp
// SQLiteConnectionService — async variants
public Task<T?>          QuerySingleAsync<T>(string sql, object? parameters = null) where T : new();
public Task<List<T>>     QueryAsync<T>(string sql, object? parameters = null)       where T : new();
public Task<int>         ExecuteAsync(string sql, object? parameters = null);
```

Internally they delegate to `SqliteConnection.OpenAsync()`, `SqliteCommand.ExecuteReaderAsync()`, and `SqliteDataReader.ReadAsync()` — every I/O point is awaited.

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

> **Note — connection lifetime.** A new `SqliteConnection` is opened *per call*. SQLite connections are cheap because the underlying file handle is shared by the SQLite driver; explicit pooling would add complexity for negligible gain.

### 8.2 Async fan-out — `Task.WhenAll`

The service layer composes async repository calls with `Task.WhenAll` whenever the I/O is independent:

```csharp
// src/MentoringApp.Service/NotificationService.cs
var studentTasks    = students.Select(s => _emailService.SendEmailAsync(...));
var supervisorTasks = supervisors.Select(sv => _emailService.SendEmailAsync(...));
var results         = await Task.WhenAll(studentTasks.Concat(supervisorTasks));
```

This is parallel I/O without manual thread management — the runtime schedules continuations on the thread pool when each SMTP/HTTP request completes.

### 8.3 Async ↔ delegate coupling

The async path frequently meets a delegate at a layer boundary. For example, `MatchingFlowService` projects a `List<UserDao>` into match candidates via a `Func<…, double>` (the `CompatibilityScorer`); the projection itself is sync, but it is invoked **inside** an async pipeline. See [`06-async-and-delegates.md`](06-async-and-delegates.md).

---

## 9. Putting it together — the canonical write path

```
Service.UpdateMentorProfileAsync(userId, request)
    │
    ├── UserValidator.Validate(model)                          ← FluentValidation
    │     └─ on failure → return Result.ValidationFailure(...)
    │
    ├── userRepo.UpdateMentorProfileAsync(UserMapper.ToDao(model))
    │     └─ SqlUserRepo:
    │         - SQLiteConnectionService.ExecuteAsync(UPSERT INTO UserMentors …)
    │           ↳ parameters bound positionally — never concatenated
    │           ↳ awaited end-to-end — never blocks a thread
    │
    └── return Result.Ok()
```

Each layer knows only its immediate neighbour; no business rule lives in SQL, no SQL leaks into the domain.

---

## 10. Curriculum Alignment

| Rubric requirement | Where realised | Section |
|---|---|---|
| Mandatory #1 — multi-table queries / updates | `SqlUserRepo.GetSupervisorStatisticsAsync` (4-table join) | §1 |
| Mandatory #4 — normalised DB w/ junction table | `Pairs`, `PairRequests`, `MatchScores` | §6 |
| Mandatory #6 — smart queries / joins / updates | All `SqlXxxRepo` methods | §1, §6 |
| Async track — async DB access | `SQLiteConnectionService.*Async` family | §8 |
| Extension §10 — SQL Injection prevention | `AddParameters`, parameterised query funnel | §7 |
| Extension §10 — validation classes | `UserValidator` / `MentorProfileValidator` | §4 |
