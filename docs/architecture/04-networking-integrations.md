# 04 — Networking & External Integrations

This document covers the seam between the ViewModel layer and the network: the **shared API client** consumed by both WPF and Blazor, the strict separation of *services* (business logic) from *API code* (HTTP), the typed-request-to-`HttpRequestMessage` conversion, and the SMTP-based email subsystem.

---

## 1. The Shared API Client

`MentoringApp.ApiClient` is the single network entry point used by both desktop and web shells. It is a dependency of `MentoringApp.ViewModel` and is **the only project that issues HTTP calls from the client side**.

### Why a dedicated project

* **Reuse without duplication.** Both `MentoringApp.Desktop` and `MentoringApp.Web` reference `MentoringApp.ViewModel`, which references `MentoringApp.ApiClient`. There is exactly one implementation of the HTTP surface.
* **Compile-time enforcement of separation.** Because `MentoringApp.ApiClient` is a library project that depends only on `MentoringApp.Model`, the compiler refuses any attempt to import a service or repository — preventing accidental coupling between the network layer and the back-end internals.

### How — client catalogue

`src/MentoringApp.ApiClient/Clients/`:

| Client | Endpoints |
|---|---|
| `AuthApiClient` | `SendCodeAsync`, `LoginAsync`, `ValidateTokenAsync` |
| `UserApiClient` | `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateLanguageAsync`, `UpdateMentorProfileAsync`, `UploadProfilePictureAsync`, … |
| `PairApiClient` | `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `DeleteAsync`, `GetMatchedMentorsAsync`, … |
| `MatchingApiClient` | `RunMatchingAsync` |
| `IssueApiClient` | `GetByPairAsync`, `CreateAsync`, `MarkResolvedAsync`, `ForwardAsync` |
| `ReviewApiClient` | `GetByPairAsync`, `CreateAsync` |
| `ReferenceApiClient` | `GetGradesAsync`, `GetSubjectsAsync`, `GetIssueCategoriesAsync` |
| `SettingsApiClient` | `GetDoubleAsync`, `SetDoubleAsync`, `GetStringAsync`, `SetStringAsync` |
| `NotificationApiClient` | `GetUnreadAsync`, `MarkReadAsync` |

Every client extends `ApiClientBase`, which encapsulates the actual HTTP semantics.

---

## 2. Strict Service / API-Code Separation

### Why

The ViewModel must remain ignorant of HTTP. If a `ViewModel` knew about `HttpClient`, status codes, JSON serialisation, or retry policy, two negative consequences would follow:

1. Unit-testing the ViewModel would require an in-memory HTTP server.
2. Migrating to a non-HTTP transport (gRPC, SignalR) would require rewriting every ViewModel.

### How — three concentric rings

```
┌────────────────────────────────────────────────────┐
│  ViewModel  ── consumes typed methods ──►          │
│                       AuthApiClient.LoginAsync(...)│
└──────────────────────────────┬─────────────────────┘
                               │   typed request, typed response
                               ▼
┌────────────────────────────────────────────────────┐
│  ApiClient  ── builds HttpRequestMessage ──►       │
│                       PostAsync<LoginResponse>(...) │
└──────────────────────────────┬─────────────────────┘
                               │   HTTP / JSON / Bearer
                               ▼
┌────────────────────────────────────────────────────┐
│  Api  (ASP.NET Core)  ── deserialises ──►          │
│                       AuthService.Login(...)        │
└────────────────────────────────────────────────────┘
```

The ViewModel never sees an `HttpResponseMessage`; the API host never sees a `HttpClient`. Both sides converge on the **shared model project** for request/response shapes.

---

## 3. The "ToHttp" Conversion Logic

The `ApiClientBase` is the single place where typed C# objects are converted into `HttpRequestMessage` instances and back. The conversion is the heart of the typed-client abstraction.

```csharp
// src/MentoringApp.ApiClient/Clients/ApiClientBase.cs (sketch)
public abstract class ApiClientBase(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected async Task<T> GetAsync<T>(string url)
    {
        using var resp = await http.GetAsync(url);
        await EnsureSuccessAsync(resp);
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts)
            ?? throw new ApiException("empty body");
    }

    protected async Task<T> PostAsync<T>(string url, object? body = null)
    {
        using var content = body is null
            ? null
            : JsonContent.Create(body, options: JsonOpts);
        using var resp = await http.PostAsync(url, content);
        await EnsureSuccessAsync(resp);
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts)
            ?? throw new ApiException("empty body");
    }

    protected async Task PutAsync<T>(string url, object? body = null) { /* … */ }
    protected async Task DeleteAsync(string url)                       { /* … */ }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var payload = await response.Content.ReadAsStringAsync();
        throw new ApiException(response.StatusCode, payload);
    }
}
```

### Why centralise the conversion

* **One serialisation contract.** `JsonSerializerDefaults.Web` is established once; every client inherits it. Casing rules, enum handling and naming policies cannot drift between clients.
* **One error contract.** `ApiException` is the only exception that escapes the API client surface. ViewModels handle exactly one exception type and translate it into `Result.Failure(...)` for the binding pipeline.
* **One auth contract.** A single `DelegatingHandler` (`BearerTokenHandler` in the ViewModel project) is registered against all clients via the `AddApiClientsWithAuth<THandler>` extension:

```csharp
public static IServiceCollection AddApiClientsWithAuth<THandler>(
    this IServiceCollection services, string baseUrl)
    where THandler : DelegatingHandler
{
    services.AddTransient<THandler>();
    services.AddHttpClient<UserApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<THandler>();
    services.AddHttpClient<PairApiClient>(c => c.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<THandler>();
    // … one HttpClient per typed client
    return services;
}
```

The `BearerTokenHandler` reads the JWT from `AuthTokenStore` and stamps the `Authorization: Bearer …` header. Adding a fresh client requires only a new line in this extension.

### How a typed client uses the base

```csharp
public sealed class AuthApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<LoginResponse> LoginAsync(LoginRequest req)
        => PostAsync<LoginResponse>("api/auth/login", req);

    public Task SendCodeAsync(SendCodeRequest req)
        => PostAsync<EmptyResponse>("api/auth/send-code", req);
}
```

The conversion from `LoginRequest` to an HTTP body, the addition of the bearer token, the deserialisation of `LoginResponse`, the propagation of failures — all of it is one line of code. The "ToHttp" plumbing is invisible at the call site.

---

## 4. The Server-Side Counterpart

The ASP.NET Core host (`MentoringApp.Api`) is built with **minimal APIs**. Endpoints are mapped per aggregate:

```csharp
// src/MentoringApp.Api/Program.cs (sketch)
builder.Services.AddDataLayer(builder.Configuration);
builder.Services.AddServices(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => /* TokenValidationParameters from JwtSettings */);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",         p => p.RequireRole("Admin"));
    options.AddPolicy("AdminOrSupervisor", p => p.RequireRole("Admin", "Supervisor"));
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPairEndpoints();
app.MapMatchingEndpoints();
app.MapIssueEndpoints();
app.MapReviewEndpoints();
app.MapReferenceEndpoints();
app.MapSettingsEndpoints();
```

Each `Map*Endpoints` extension lives next to the relevant feature and delegates straight to a service:

```csharp
public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
{
    app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
    {
        var result = await auth.LoginAsync(req.NationalId, req.Code);
        return result.Success
            ? Results.Ok(result.Data)
            : Results.BadRequest(new { result.ErrorMessage });
    });
    return app;
}
```

The endpoint code is *only* concerned with HTTP shape (route, status codes, model binding). The business logic lives entirely in `AuthService`. This mirrors the client-side rule: the API project never imports the data layer (it imports the service layer, which imports the data layer), so business rules cannot accidentally migrate into endpoint handlers.

---

## 5. Email Client

`src/MentoringApp.Service/EmailService.cs` provides the SMTP integration used for verification-code login.

```csharp
public sealed class EmailService
{
    private readonly string _smtpHost, _fromEmail, _fromPassword;
    private readonly int _smtpPort;

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                    EnableSsl   = true
                };
                await client.SendMailAsync(new MailMessage(_fromEmail, to, subject, htmlBody)
                {
                    IsBodyHtml = true
                });
                return true;
            }
            catch (SmtpException ex) when (IsTransient(ex) && attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s
            }
        }
        return false;
    }

    private static bool IsTransient(SmtpException ex)
        => (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500;
}
```

### Why this shape

* **Singleton.** Constructed once at startup with credentials read from `EmailSettings`, then injected wherever needed. SMTP credentials are an immutable host-level concern, not a per-request one.
* **Bounded retry.** SMTP servers commonly produce transient 4xx-class errors under throttling. Three attempts with exponential back-off (2 s, 4 s) catches the typical transient case without delaying the user excessively. Non-transient failures (5xx, authentication, malformed address) bypass the retry loop and surface immediately.
* **`bool` return rather than `Result<T>`.** The email subsystem is *fire-and-confirm*. The caller only needs to know whether the send eventually succeeded; structured validation errors do not arise from SMTP.

### How it is wired

```csharp
// src/MentoringApp.Service/DI/ServiceDependencyInjection.cs
var emailSection = configuration.GetSection("EmailSettings");
services.AddSingleton(sp => new EmailService(
    smtpHost:     emailSection["SmtpHost"],
    smtpPort:     int.Parse(emailSection["SmtpPort"] ?? "587"),
    fromEmail:    emailSection["FromEmail"],
    fromPassword: emailSection["FromPassword"]));
```

Empty values in the configuration are tolerated for development; the API host runs without a configured SMTP server, and `SendEmailAsync` simply returns `false`. `AuthService` then falls back to logging the verification code to the developer console via `ILogger`.

---

## 6. Stateless Architecture (Network Services Track)

The rubric's Network-Services track requires *"a stateless environment, e.g. ASP.NET"*. The codebase satisfies this requirement uniformly.

### 6.1 What "stateless" means here

* **No session affinity.** Two consecutive requests from the same client may be served by different threads, processes or hosts. Nothing per-user is held in memory between requests.
* **JWT carries identity.** Every authenticated request presents `Authorization: Bearer <jwt>`; the server reconstructs the user from the token claims without a session lookup.
* **Per-request DI scope.** Repositories, services, and DbContexts (when they exist) are `Scoped`, meaning they are constructed at the start of a request and disposed at the end. There is no cross-request mutable state.

### 6.2 Mechanics — the request lifecycle

```
HTTP request
    │
    ├── Authentication middleware reads JWT, builds ClaimsPrincipal
    ├── Authorization policy validates the route's [Authorize(Policy=…)]
    │
    ├── Endpoint handler resolved from minimal-API map
    │     ↳ ASP.NET creates a fresh DI scope
    │     ↳ Resolves Scoped services from that scope
    │
    ├── Service runs business logic, returns Result<T>
    └── Endpoint maps Result<T> → HTTP status code, scope is disposed
```

> **Reviewer note** — there is no `Session.Add(...)` anywhere in the codebase. Even verification codes (a stateful concept) are stored in the **database**, not in memory; that is what keeps the host genuinely horizontally scalable.

### 6.3 Comparison

| Stateful (anti-pattern) | Stateless (this codebase) |
|---|---|
| `HttpContext.Session["user"]` | JWT claims |
| In-process verification-code dictionary | `VerificationCodes` table |
| Server-side cart object | Client holds the cart, sends it on submit |

---

## 7. File Transfer Between Server & Client (Extension §9)

The rubric's section 9 includes *"transferring files (images / songs / documents) between server and client and/or vice versa"*. Two file-transfer features exist in production today.

### 7.1 Profile-picture upload — `multipart/form-data`

**Client side** — `UserApiClient.UploadProfilePictureAsync(int userId, string localPath)` builds a `MultipartFormDataContent`:

```csharp
public async Task<string> UploadProfilePictureAsync(int userId, string localPath)
{
    using var stream     = File.OpenRead(localPath);
    using var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

    using var form = new MultipartFormDataContent
    {
        { fileContent, "file", Path.GetFileName(localPath) }
    };

    using var resp = await _http.PostAsync($"api/users/{userId}/profile-picture", form);
    // …
}
```

**Server side** — endpoint receives an `IFormFile`:

```csharp
group.MapPost("/{id:int}/profile-picture", async (int id, IFormFile file, UserService svc) =>
{
    if (file.Length == 0) return Results.BadRequest();
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var saved = await svc.SaveProfilePictureAsync(id, ms.ToArray(), file.FileName);
    return saved.Success ? Results.Ok(new { url = saved.Data }) : Results.BadRequest();
});
```

> **Reviewer notes**
> - **Stream, not byte array, on the client.** `StreamContent` lets the runtime upload incrementally without buffering the entire file in memory.
> - **`MemoryStream` on the server.** Acceptable for small files (profile pictures are <1 MB). For larger documents, prefer `file.OpenReadStream()` and pipe directly to disk.
> - **Authorization.** The endpoint inherits `RequireAuthorization()` from the group; only authenticated users can upload.
> - **Content-Type validation belongs in the service layer.** Reject `image/svg+xml` and similar — see [`10-security.md`](10-security.md) §4.

### 7.2 Excel import — file system + ClosedXML

`ExcelImportService.ImportStudentsFromExcelAsync(string filePath)` is the bulk-onboarding pipeline.

```csharp
public async Task<Result<int>> ImportStudentsFromExcelAsync(string filePath)
{
    using var workbook = new XLWorkbook(filePath);
    var worksheet      = workbook.Worksheet(1);
    var rows           = worksheet.RowsUsed().Skip(1);  // skip header

    int successCount = 0;
    foreach (var row in rows)
    {
        var nationalId = row.Cell(1).GetString()?.Trim() ?? "";
        var email      = row.Cell(2).GetString()?.Trim() ?? "";
        var userName   = row.Cell(3).GetString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nationalId) ||
            string.IsNullOrWhiteSpace(email)      ||
            string.IsNullOrWhiteSpace(userName)) continue;

        var student = new StudentModel(0, email, userName, nationalId,
                                       new GradeModel { Id = 1, Name = "Imported", Num = 0 });
        if ((await _userService.CreateUserAsync(student)).Success) successCount++;
    }
    return Result<int>.Ok(successCount);
}
```

> **Why ClosedXML?** It is a managed wrapper over OpenXML — no Microsoft Office or COM automation is required, so the import works on a headless server.

### 7.3 Generalised file-transfer pattern

| Direction | Transport | Use site |
|---|---|---|
| Client → Server | `multipart/form-data` | Profile picture upload |
| Server → Client | `image/*` response with caching headers | Profile picture serving |
| Local → Local | File path + ClosedXML/IO | Excel import (admin-only) |

See [`09-file-management.md`](09-file-management.md) for the security-sensitive details (path traversal, content sniffing, virus scanning).

---

## 8. External Service Integration (Extension §10)

The rubric's section 10 mentions *"writing and using an external Service, as needed by the project"*. Two external services are integrated.

### 8.1 SMTP (Gmail)

Already covered in §5. The `EmailService` consumes the standard `System.Net.Mail.SmtpClient`; nothing in the calling code is aware of Gmail specifically — switching to a transactional-email vendor (SendGrid, Mailgun) is a one-class change.

### 8.2 Configuration-only switching

`EmailSettings` in `appsettings.json` is the only thing that has to change to redirect mail traffic:

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromEmail": "noreply@example.com",
    "FromPassword": "<app-password>"
  }
}
```

> **Reviewer note** — credentials are read **once** at startup; rotating them requires a host restart, which is acceptable for this app's deployment cadence. For zero-downtime rotation, wrap the credential read in `IOptionsMonitor<EmailSettings>`.

### 8.3 Future-friendly seams

The architecture is set up so that a future *AI service* (rubric extension §10) — for instance an LLM-driven mentor recommendation — can plug into `MatchingFlowService` behind a new interface (`ICompatibilityProvider`) without rewriting any caller. See [`12-bonus-and-extensions.md`](12-bonus-and-extensions.md).

---

## 9. Summary

The networking architecture is a strict three-ring layout: **typed client methods → `ApiClientBase` HTTP conversion → ASP.NET Core minimal APIs**. JSON, authentication, error handling and retry policy are each centralised in exactly one place. The email subsystem follows the same single-responsibility shape: a singleton service exposing one method, with retry semantics bounded to genuinely transient failures. The architecture is genuinely **stateless** — JWTs carry identity, scoped DI carries the per-request dependency graph, and there is no in-memory session store.

---

## 10. Curriculum Alignment

| Rubric concept | Realisation | Section |
|---|---|---|
| Server-Client web services (Network track) | `ApiClient` ↔ minimal-API host | §1, §4 |
| `Service` built by the student (Network track) | `*Service` classes invoked by endpoints | §4 |
| Stateless environment (Network track) | JWT + scoped DI, no session store | §6 |
| File transfer (extension §9) | Profile picture upload (`multipart/form-data`), Excel import | §7 |
| External Service (extension §10) | SMTP via `EmailService` | §8 |
