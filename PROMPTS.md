# MentoringApp Migration Prompts

Work through these tasks in order. Each is self-contained — paste it into a fresh session.
The repo is at the current working directory. Run `dotnet build MentoringApp.sln` after each task to confirm nothing broke.

---

## Task 1 — Create MentoringApp.Api with JWT auth and Swagger

### Context
- This is the REST backend for the Blazor WASM web client
- Must reference `Service`, `Data`, `Model` projects
- WPF project must continue to build independently — do not modify it
- `Service/DI/ServiceDependencyInjection.cs` has `AddServices(IServiceCollection, IConfiguration)`
- `Data/DI/DataDepedencyInjection.cs` has `AddDataLayer(IServiceCollection, IConfiguration)` (added in Task 3)

### Steps
1. Scaffold and add to solution:
   ```bash
   dotnet new webapi -n MentoringApp.Api -o MentoringApp.Api --framework net9.0 --use-minimal-apis
   dotnet sln MentoringApp.sln add MentoringApp.Api/MentoringApp.Api.csproj
   ```

2. Add project references in `MentoringApp.Api.csproj`:
   ```xml
   <ProjectReference Include="..\Service\MentoringApp.Service.csproj" />
   <ProjectReference Include="..\Data\MentoringApp.Data.csproj" />
   <ProjectReference Include="..\Model\MentoringApp.Model.csproj" />
   ```

3. Add NuGet packages:
   ```bash
   dotnet add MentoringApp.Api package Microsoft.AspNetCore.Authentication.JwtBearer
   dotnet add MentoringApp.Api package Swashbuckle.AspNetCore
   ```

4. Create `MentoringApp.Api/appsettings.json`:
   ```json
   {
     "DataProvider": "postgres",
     "ConnectionStrings": {
       "Postgres": "Host=localhost;Port=5432;Database=mentoringapp;Username=postgres;Password=postgres"
     },
     "JwtSettings": {
       "Secret": "CHANGE_ME_TO_A_32_CHAR_MIN_SECRET",
       "Issuer": "MentoringApp",
       "Audience": "MentoringApp",
       "ExpiryHours": 8
     },
     "AllowedOrigins": "http://localhost:5173",
     "EmailSettings": {
       "SmtpHost": "smtp.gmail.com",
       "SmtpPort": 587,
       "FromEmail": "",
       "FromPassword": ""
     }
   }
   ```

5. Write `MentoringApp.Api/Program.cs`:
   - `builder.Services.AddDataLayer(builder.Configuration)`
   - `builder.Services.AddServices(builder.Configuration)`
   - JWT Bearer auth using `JwtSettings` from config
   - CORS policy `"WebClient"` allowing origins from `config["AllowedOrigins"]`
   - Swagger with Bearer token support (`SecurityScheme` + `SecurityRequirement`)
   - `app.UseAuthentication(); app.UseAuthorization();`
   - `app.UseCors("WebClient");`
   - `app.UseSwagger(); app.UseSwaggerUI();` inside `if (app.Environment.IsDevelopment())`

6. Create `MentoringApp.Api/Endpoints/AuthEndpoints.cs`:
   - `POST /api/auth/login` — body: `{ NationalId, Password }`. Call `AuthService.LoginAsync(...)`. On success generate a JWT with claims: `sub = user.Id`, `name = user.UserName`, `role = "Admin"|"Supervisor"|"Student"`, `language = user.Language`. Return `{ token, expiresAt }`. Return `401` on failure.
   - Map via `app.MapAuthEndpoints()` extension method called from `Program.cs`.

7. Create `MentoringApp.Api/Helpers/JwtHelper.cs` with a `GenerateToken(UserModel user, JwtSettings settings)` static method.

### Acceptance Criteria
- [ ] `dotnet run --project MentoringApp.Api` starts without errors
- [ ] Swagger UI accessible at `https://localhost:{port}/swagger`
- [ ] `POST /api/auth/login` with valid credentials returns `{ token, expiresAt }`
- [ ] Invalid credentials return `401`
- [ ] `dotnet build MentoringApp.sln` builds all projects

---

## Task 2 — Implement all API endpoint groups

### Context
- `MentoringApp.Api` exists with JWT auth wired
- All business logic is in `Service/` — inject services directly into endpoint handlers
- Pattern: one static class per domain in `MentoringApp.Api/Endpoints/`, each with a `MapXxxEndpoints(this WebApplication app)` extension method
- All endpoints require `.RequireAuthorization()` unless noted
- Create `MentoringApp.Api/Helpers/ResultExtensions.cs` first:
  ```csharp
  public static IResult ToHttp<T>(this Result<T> r) =>
      r.Success ? Results.Ok(r.Data) : Results.BadRequest(new { error = r.Error });
  public static IResult ToHttp(this Result r) =>
      r.Success ? Results.Ok() : Results.BadRequest(new { error = r.Error });
  ```

### Steps
Create the following endpoint files and map them all in `Program.cs`.

**UserEndpoints.cs** (`/api/users`):
- `GET /` — Admin — all users as `IEnumerable<UserDto>`
- `GET /{id}` — single `UserDto`, `404` if not found
- `POST /` — Admin — body: `{ userName, email, nationalId, phoneNumber, gender, role }`, return `201`
- `DELETE /{id}` — Admin — `204` or `404`
- `PUT /{id}/base-info` — body: `{ userName, email, nationalId, phoneNumber, gender }`
- `PUT /{id}/language` — body: `{ language }`
- `PUT /{id}/grade-class` — body: `{ gradeId, classNum }`
- `PUT /{id}/gender-preferences` — body: `{ preferredMentorGender, preferredMenteeGender }`
- `PUT /{id}/mentor-profile` — body: `{ subjectId }`
- `GET /supervisors/stats` — Admin

**PairEndpoints.cs** (`/api/pairs`):
- `GET /` — Admin/Supervisor
- `GET /{id}`, `GET /by-mentor/{mentorId}`, `GET /by-mentee/{menteeId}`, `GET /by-supervisor/{supervisorId}`
- `POST /` — Admin — body: `{ supervisorId, mentorId, menteeId }`
- `DELETE /{id}` — Admin

**MatchingEndpoints.cs** (`/api/matching`):
- `GET /available-mentors`, `GET /available-mentees`
- `POST /requests` — Student — body: `{ menteeId, mentorId }`
- `GET /requests/for-mentee/{menteeId}`, `GET /requests/for-mentor/{mentorId}`
- `PUT /requests/{requestId}/accept` — Supervisor/Admin — body: `{ supervisorId }`
- `PUT /requests/{requestId}/reject` — Supervisor/Admin
- `DELETE /requests/{requestId}` — Student (cancel)
- `GET /recommendations/{menteeId}` — top-3 gallery recommendations
- `POST /gallery-pick` — Student — body: `{ menteeId, mentorId, supervisorId }`
- `POST /pipeline/generate-scores` — Admin
- `POST /pipeline/auto-match` — Admin — returns `{ pairsCreated }`
- `POST /pipeline/fallback-match` — Admin — returns `{ pairsCreated }`

**IssueEndpoints.cs** (`/api/issues`):
- `GET /` — Admin
- `GET /by-user/{userId}`, `GET /by-supervisor/{supervisorId}`
- `GET /forwarded` — Admin
- `GET /categories`
- `POST /` — body: `{ description, categoryId, reportedByUserId }`
- `PUT /{id}/resolve` — Admin/Supervisor
- `PUT /{id}/forward` — Supervisor — body: `{ supervisorId }`

**ReviewEndpoints.cs** (`/api/reviews`):
- `GET /by-pair/{pairId}`, `GET /by-author/{userId}`
- `POST /` — Student — body: `{ content, date, pairId, authorUserId, amountOfHours }`

**ReferenceEndpoints.cs** (`/api/reference`):
- `GET /subjects`, `GET /grades`
- `GET /school-classes`, `GET /school-classes/by-supervisor/{supervisorId}`

### Acceptance Criteria
- [ ] All endpoint groups appear in Swagger UI with correct schemas
- [ ] Admin-only endpoints return `403` for non-admin JWT
- [ ] `dotnet build MentoringApp.Api` passes

---

## Task 3 — Add CORS, global error handler, and validation

### Context
- `MentoringApp.Api` has all endpoints but no centralized error handling
- Unhandled exceptions return raw stack traces
- CORS must allow the Blazor WASM origin to call the API

### Steps
1. In `Program.cs` add before endpoint mapping:
   ```csharp
   app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
   {
       var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
       ctx.Response.StatusCode = 500;
       ctx.Response.ContentType = "application/problem+json";
       var msg = app.Environment.IsDevelopment() ? ex?.Message : "An unexpected error occurred.";
       await ctx.Response.WriteAsJsonAsync(new { error = msg });
   }));
   ```

2. Confirm `app.UseCors("WebClient")` is called before `app.UseAuthentication()`.

3. Create `MentoringApp.Api/appsettings.Development.json`:
   ```json
   { "AllowedOrigins": "http://localhost:5173" }
   ```

4. In endpoint handlers: return `Results.NotFound()` when a service lookup returns null. Return `Results.BadRequest(new { error = "..." })` for missing required fields.

### Acceptance Criteria
- [ ] Unhandled exceptions return `{ error: "..." }` JSON, not a stack trace
- [ ] `Result.Failure(msg)` → `400` with `{ error: msg }`
- [ ] Blazor WASM origin can call API without CORS errors
- [ ] `dotnet build MentoringApp.Api` passes

---

## Task 4 — Create MentoringApp.ApiClient shared library

### Context
- Both Blazor WASM and a future MAUI app will use this library
- All HTTP client logic lives here — no direct `HttpClient` calls in client projects
- Plain .NET class library — no Razor, MAUI, or WPF dependencies

### Steps
1. Create and add to solution:
   ```bash
   dotnet new classlib -n MentoringApp.ApiClient -o MentoringApp.ApiClient --framework net9.0
   dotnet sln MentoringApp.sln add MentoringApp.ApiClient/MentoringApp.ApiClient.csproj
   ```
   Add NuGet: `System.Net.Http.Json`.

2. Create `MentoringApp.ApiClient/Models/` with C# `record` types for every API request/response:
   ```csharp
   public record LoginRequest(string NationalId, string Password);
   public record LoginResponse(string Token, DateTime ExpiresAt);
   public record UserResponse(int Id, string UserName, string Email, string Role, string Language, string? ProfilePicturePath);
   // ... one record per endpoint request body and response shape
   ```

3. Create `MentoringApp.ApiClient/Exceptions/ApiException.cs`:
   ```csharp
   public class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
   {
       public HttpStatusCode StatusCode { get; } = statusCode;
   }
   ```

4. Create typed clients in `MentoringApp.ApiClient/Clients/`:
   `AuthApiClient`, `UserApiClient`, `PairApiClient`, `MatchingApiClient`, `IssueApiClient`, `ReviewApiClient`, `ReferenceApiClient`

   Each accepts `HttpClient` via constructor. Uses `GetFromJsonAsync`, `PostAsJsonAsync`, etc. On non-2xx: read `{ error }` from body and throw `ApiException`.

5. Create `MentoringApp.ApiClient/Extensions/ApiClientServiceExtensions.cs`:
   ```csharp
   public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl)
   {
       services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(baseUrl));
       // ... repeat for each client
       return services;
   }
   ```

### Acceptance Criteria
- [ ] Zero references to MAUI, WPF, or Blazor
- [ ] Every API endpoint from Task 8 has a typed client method
- [ ] `dotnet build MentoringApp.ApiClient` passes

---

## Task 5 — Create MentoringApp.Components Razor Class Library

### Context
- All Razor pages live here — shared between WASM now, MAUI later
- References only `MentoringApp.ApiClient` — NO reference to Service, Data, or Model
- Defines `IAuthService` so each shell implements token storage its own way

### Steps
1. Create and add to solution:
   ```bash
   dotnet new razorclasslib -n MentoringApp.Components -o MentoringApp.Components --framework net9.0
   dotnet sln MentoringApp.sln add MentoringApp.Components/MentoringApp.Components.csproj
   ```
   Add project reference to `MentoringApp.ApiClient`.

2. Create `MentoringApp.Components/Auth/IAuthService.cs`:
   ```csharp
   public interface IAuthService
   {
       Task<bool> LoginAsync(string nationalId, string password);
       Task LogoutAsync();
       bool IsAuthenticated { get; }
       string? Token { get; }
       string? Role { get; }
       int? UserId { get; }
       string? Language { get; }
   }
   ```

3. Create `MentoringApp.Components/Services/LayoutStateService.cs` — singleton holding `string Language` with `event Action? OnChange`.

4. Create `MentoringApp.Components/Shared/MainLayout.razor` (`@inherits LayoutComponentBase`):
   - `<div dir="@_dir" lang="@_lang">` wrapping `@Body`
   - Role-conditional sidebar nav links
   - Language toggle button
   - Logout link

5. Create pages in `MentoringApp.Components/Pages/`:

   **Login.razor** (`/login`): NationalId + Password fields. Calls `IAuthService.LoginAsync`, navigates by role on success.

   **Admin/AdminDashboard.razor** (`/admin`): User table with search, delete (with confirmation), create-user inline form.

   **Admin/MatchingPipeline.razor** (`/admin/matching`): Three pipeline action cards (Generate Scores, Auto-Match, Fallback). Pair table with incomplete-profile highlighting.

   **Supervisor/SupervisorDashboard.razor** (`/supervisor`): Pairs table + issues queue with Resolve and Forward buttons.

   **Student/Profile.razor** (`/student/profile`): Independent save sections — Base Info, Grade & Class, Gender Preferences, Mentor Profile (conditional), Mentee Profile (conditional).

   **Student/Matching.razor** (`/student/matching`): If paired → show pair info. Else: Tier 3 gallery (top-3 mentor cards with score bars + Choose) + Tier 1 requests (send/cancel) + mentor inbox (accept/reject) if student is also a mentor.

   **User/Issues.razor** (`/issues`): Collapsible report form + filterable list (by category and status).

   **Student/Reviews.razor** (`/student/reviews`): Holding message if not paired. Else: review timeline + submit form.

6. Create `MentoringApp.Components/wwwroot/css/app.css`:
   ```css
   [dir="rtl"] { text-align: right; }
   [dir="rtl"] .sidebar { left: auto; right: 0; }
   [dir="rtl"] .nav-item { flex-direction: row-reverse; }
   ```

### Acceptance Criteria
- [ ] No dependency on Service, Data, Model, MAUI, or WPF
- [ ] `IAuthService` defined; all pages use it via `@inject`
- [ ] 8 page files exist under `Pages/`
- [ ] `dotnet build MentoringApp.Components` passes

---

## Task 6 — Create MentoringApp.Web Blazor WASM shell

### Context
- Thin browser shell — provides `IAuthService` (in-memory JWT) and wires DI
- All pages come from `MentoringApp.Components` — do NOT add Razor pages here
- JWT stored in memory only (not localStorage)

### Steps
1. Create and add to solution:
   ```bash
   dotnet new blazorwasm -n MentoringApp.Web -o MentoringApp.Web --framework net9.0
   dotnet sln MentoringApp.sln add MentoringApp.Web/MentoringApp.Web.csproj
   ```
   Add references to `MentoringApp.Components` and `MentoringApp.ApiClient` only.

2. Delete all Blazor template boilerplate pages and sample components.

3. Create `MentoringApp.Web/Services/WasmAuthService.cs` implementing `IAuthService`:
   - `string? _token` — private, in-memory
   - `LoginAsync`: calls `AuthApiClient.LoginAsync`, stores token, parses JWT claims
   - `LogoutAsync`: clears `_token`
   - Claim properties (`Role`, `UserId`, `Language`): read from base64url-decoded JWT payload

4. Create `MentoringApp.Web/Services/BearerTokenHandler.cs` (`DelegatingHandler`):
   ```csharp
   protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
   {
       if (_authService.Token is not null)
           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.Token);
       return await base.SendAsync(request, ct);
   }
   ```

5. Write `MentoringApp.Web/Program.cs`:
   ```csharp
   builder.Services.AddScoped<WasmAuthService>();
   builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<WasmAuthService>());
   builder.Services.AddScoped<BearerTokenHandler>();
   builder.Services.AddSingleton<LayoutStateService>();
   builder.Services.AddApiClients(builder.Configuration["ApiBaseUrl"]!);
   // Add BearerTokenHandler to each HttpClient registration
   ```

6. Update `App.razor` to route from the Components assembly:
   ```razor
   <Router AppAssembly="typeof(MentoringApp.Components._Imports).Assembly">
       <Found Context="routeData">
           <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
       </Found>
       <NotFound><p>Page not found.</p></NotFound>
   </Router>
   ```

7. Create `MentoringApp.Web/wwwroot/appsettings.json`:
   ```json
   { "ApiBaseUrl": "https://localhost:7001" }
   ```

### Acceptance Criteria
- [ ] `dotnet run --project MentoringApp.Web` serves login page at `http://localhost:5173`
- [ ] Login navigates to correct role dashboard
- [ ] All API calls include `Authorization: Bearer` header
- [ ] No `.razor` pages defined in this project
- [ ] `dotnet build MentoringApp.sln` builds all 7 projects

---

## Task 7 — Docker and docker-compose

### Context
- One container: `MentoringApp.Api` — serves both the REST API and the Blazor WASM static files (hosted WASM pattern)
- WPF is NOT containerized

### Steps
1. Add to `MentoringApp.Api.csproj`:
   ```xml
   <ProjectReference Include="..\MentoringApp.Web\MentoringApp.Web.csproj" />
   ```

2. In `MentoringApp.Api/Program.cs` after `app.UseStaticFiles()`:
   ```csharp
   app.UseBlazorFrameworkFiles();
   app.MapFallbackToFile("index.html");
   ```

3. Update `MentoringApp.Web/wwwroot/appsettings.json` for same-origin production use:
   ```json
   { "ApiBaseUrl": "" }
   ```

4. Create `Dockerfile` at repo root:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
   WORKDIR /src
   COPY . .
   RUN dotnet publish MentoringApp.Api/MentoringApp.Api.csproj \
       -c Release -o /app/publish --no-self-contained

   FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
   WORKDIR /app
   COPY --from=build /app/publish .
   EXPOSE 8080
   ENTRYPOINT ["dotnet", "MentoringApp.Api.dll"]
   ```

5. Create `docker-compose.yml` at repo root:
   ```yaml
   services:
     db:
       image: postgres:16-alpine
       environment:
         POSTGRES_DB: mentoringapp
         POSTGRES_USER: postgres
         POSTGRES_PASSWORD: postgres
       volumes:
         - pgdata:/var/lib/postgresql/data
       ports:
         - "5432:5432"

     api:
       build: .
       depends_on:
         - db
       ports:
         - "8080:8080"
       environment:
         ASPNETCORE_URLS: http://+:8080
         DataProvider: postgres
         ConnectionStrings__Postgres: "Host=db;Port=5432;Database=mentoringapp;Username=postgres;Password=postgres"
         JwtSettings__Secret: ${JWT_SECRET}
         JwtSettings__Issuer: MentoringApp
         JwtSettings__Audience: MentoringApp
         JwtSettings__ExpiryHours: 8
         AllowedOrigins: http://localhost:8080
         EmailSettings__SmtpHost: ${SMTP_HOST}
         EmailSettings__FromEmail: ${FROM_EMAIL}
         EmailSettings__FromPassword: ${FROM_PASSWORD}

   volumes:
     pgdata:
   ```

6. Create `.env.example`:
   ```
   JWT_SECRET=replace_with_32_char_minimum_secret_here
   SMTP_HOST=smtp.gmail.com
   FROM_EMAIL=your@email.com
   FROM_PASSWORD=your_app_password
   ```

7. Add `.env` to `.gitignore`. Create `.dockerignore`:
   ```
   **/bin/
   **/obj/
   **/*.db
   .env
   MentoringApp/
   MentoringApp.ViewModel/
   ```

### Acceptance Criteria
- [ ] `docker compose up` starts db and api
- [ ] `http://localhost:8080` serves the WASM login page
- [ ] `http://localhost:8080/swagger` serves Swagger UI
- [ ] No secrets in any tracked file

---

## Task 8 — GitLab CI/CD pipeline

### Context
- Migrations must run on Postgres before deploying the new API version
- All secrets via GitLab CI/CD variables

### Steps
Create `.gitlab-ci.yml` at repo root:

```yaml
stages:
  - build
  - migrate
  - deploy

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet restore MentoringApp.sln
    - dotnet build MentoringApp.sln -c Release --no-restore
    - dotnet test Tests/MentoringApp.Tests.csproj -c Release --no-build || true
    - dotnet publish MentoringApp.Api/MentoringApp.Api.csproj -c Release -o publish/
  artifacts:
    paths:
      - publish/
    expire_in: 1 hour
  only:
    - main
    - develop

migrate:
  stage: migrate
  image: mcr.microsoft.com/dotnet/sdk:9.0
  variables:
    ConnectionStrings__Postgres: $PROD_POSTGRES_CONNECTION
  script:
    - dotnet tool install --global dotnet-ef
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - dotnet ef database update
        --project Data/MentoringApp.Data.csproj
        --context MentoringDbContext
  only:
    - main

deploy:
  stage: deploy
  needs:
    - build
    - migrate
  script:
    - echo "Deploy publish/ to your server — configure SSH/kubectl/docker stack here"
  only:
    - main
```

Create `docs/ci-variables.md`:

| Variable | Description |
|---|---|
| `PROD_POSTGRES_CONNECTION` | Full Postgres connection string for production |
| `JWT_SECRET` | 32+ character random secret for JWT signing |
| `SMTP_HOST` | SMTP server hostname |
| `FROM_EMAIL` | Sender email address |
| `FROM_PASSWORD` | SMTP app password |

### Acceptance Criteria
- [ ] Pipeline triggers on `main` and `develop`
- [ ] `build` stage fails if `dotnet build` fails
- [ ] `migrate` runs only on `main`
- [ ] No secrets in `.gitlab-ci.yml`
