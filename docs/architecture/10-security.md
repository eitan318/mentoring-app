# 10 — Security Architecture

> **Curriculum context.** The *Integrated Data Applications* rubric carries security touchpoints in multiple sections:
> * Mandatory #8 — *"the project handles multiple permission levels … the UI must contain only the options corresponding to the permission level."*
> * Section 10 — *"Encryption — using encryption (and other tools) to protect 'sensitive' data such as passwords or credit-card numbers."*
> * Section 10 — *"Protection against database hacks via SQL Injection — using parameters."*
>
> This document is the consolidated security view of the codebase: authentication, authorization, input handling, secret management, transport, and the anti-injection / anti-tampering posture.

---

## 1. Authentication

### 1.1 Verification-code login flow

The codebase uses a **two-step email verification** flow rather than passwords. The decision is intentional: passwords are the highest-risk credential to manage; an email-bound code is much harder to misuse.

```
Client            API                           DB / Email
  │ POST /api/auth/send-code                    │
  │ { nationalId }                              │
  ├──────────────────►                          │
  │                  AuthService.Send…          │
  │                  ├── lookup user by NID ──► │
  │                  ├── generate 6-digit code  │
  │                  ├── persist (UserId, Code, │
  │                  │    CreatedAt) ────────► │
  │                  └── EmailService.Send ───► │ (SMTP)
  │ ◄──── 200 OK ───┤                           │
  │                  │                           │
  │ POST /api/auth/login                        │
  │ { nationalId, code }                        │
  ├──────────────────►                          │
  │                  AuthService.Login          │
  │                  ├── verify code, expiry ◄─ │
  │                  ├── delete code (one-shot) │
  │                  └── JwtHelper.GenerateToken│
  │ ◄── { token } ──┤                           │
```

**Properties:**

| Property | Value |
|---|---|
| Code length | 6 digits |
| Code TTL | 10 minutes (`AuthService.VerificationCodeValid`) |
| Reuse | One-shot; deleted on first valid use |
| Brute-force window | 10 minutes × 1 000 000 codes = ~6 yr to expect a hit; per-IP rate limit recommended |
| Storage | `VerificationCodes` table with `UserId`, `Code`, `CreationDate` |

> **Reviewer note** — the current schema stores the code in clear-text. For higher assurance, hash with `Rfc2898DeriveBytes` (PBKDF2) using a per-row salt. The verify path then hashes the input and compares.

### 1.2 JWT issuance

Once the code is validated, `JwtHelper.GenerateToken` returns a signed token:

```csharp
var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
    issuer:             settings.Issuer,
    audience:           settings.Audience,
    claims:             claims,
    expires:            DateTime.UtcNow.AddHours(settings.ExpiryHours),
    signingCredentials: creds);
```

Settings:

```json
"JwtSettings": {
    "Secret":      "<256-bit-secret>",
    "Issuer":      "MentoringApp",
    "Audience":    "MentoringAppClients",
    "ExpiryHours": 12
}
```

> **Reviewer rule** — the `Secret` must be sourced from an environment variable in production, never committed. The repo's value is for development only.

### 1.3 Token validation

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,
    ValidateAudience         = true,
    ValidateLifetime         = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer              = jwtSettings.Issuer,
    ValidAudience            = jwtSettings.Audience,
    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
};
```

`ValidateIssuerSigningKey: true` is essential — without it, tokens forged with a different signing key would still pass validation.

---

## 2. Authorization

Detailed in [`07-authorization-and-permissions.md`](07-authorization-and-permissions.md). Summary:

- Three role policies declared in `Program.cs`: `AdminOnly`, `AdminOrSupervisor`, `AnyAuthenticated`.
- Endpoints attach policies via `RequireAuthorization("…")`.
- Resource-based decisions (e.g. *"this supervisor only sees their own classes"*) are inline checks, not generic policies.
- UI gating is convenience, never security.

---

## 3. Input Handling

### 3.1 SQL Injection — parameterised queries

Covered in detail in [`02-data-persistence.md`](02-data-persistence.md) §7. Summary:

- Every `SqlXxxRepo` builds queries with named parameters (`@UserId`, `@Email`, …).
- `SqliteCommand.Parameters.AddWithValue` performs type-safe binding, never string substitution.
- No code path in the data layer allows raw concatenation.

### 3.2 HTML / JavaScript injection

The Blazor client renders bound strings through Razor's automatic HTML-encoding. Email bodies that include user-controlled text use `WebUtility.HtmlEncode(...)` explicitly:

```csharp
private static string BuildIssueCreatedBody(...) => $"""
    <p><strong>Issue:</strong> {WebUtility.HtmlEncode(description)}</p>
    """;
```

> **Reviewer rule** — every `${userInput}` interpolated into HTML must be encoded.

### 3.3 Cross-Origin Resource Sharing (CORS)

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(','))
        .AllowAnyHeader()
        .AllowAnyMethod());
});
```

`AllowedOrigins` is comma-delimited list configured per environment.

> **Reviewer note** — `AllowAnyOrigin()` is forbidden when credentials are sent. The codebase uses `WithOrigins` with an explicit allow-list.

---

## 4. Sensitive Data Protection (rubric §10 "Encryption")

| Concern | Status |
|---|---|
| Passwords | Not stored — verification-code flow eliminates them |
| Verification codes | Cleartext today; PBKDF2 recommended (see §1.1) |
| JWT secret | HMAC-SHA256 signing; secret loaded from configuration / env vars |
| SMTP credentials | Loaded from `EmailSettings` via configuration; recommend env var in production |
| Database file | Filesystem permissions; encrypt-at-rest is OS-level |
| Sensitive PII (email, national-id) | Stored in cleartext; production hardening would encrypt these columns at rest with a server-managed key |

### 4.1 Hashing approach for future password fields

If passwords are ever introduced, use `Microsoft.AspNetCore.Identity.PasswordHasher<T>`:

```csharp
// [Placeholder] never invent a hash scheme — use the framework's:
var hasher = new PasswordHasher<UserModel>();
var hashed = hasher.HashPassword(user, plaintext);
// At login:
var result = hasher.VerifyHashedPassword(user, hashed, plaintext);
```

`PasswordHasher` runs PBKDF2 with HMAC-SHA256, 100 000 iterations, by default.

### 4.2 Encryption-at-transit

HTTPS is enforced at the host level: `app.UseHttpsRedirection()` in `Program.cs`. JWT tokens, file uploads, and verification submissions all travel over TLS in any non-development environment.

---

## 5. Secret Management

| Secret | Where today | Where in production |
|---|---|---|
| JWT signing key | `appsettings.json` (dev only) | Environment variable, e.g. `JwtSettings__Secret` |
| SMTP password | `appsettings.json` (dev only) | Environment variable, e.g. `EmailSettings__FromPassword` |
| DB connection string | `appsettings.json` | Environment variable; consider Azure Key Vault / AWS SSM in cloud deployments |

> **Reviewer rule** — committing secrets is a P0 incident. The repo's `.gitignore` excludes `appsettings.Production.json` and `*.user`; CI rejects PRs that change tracked secret files.

---

## 6. Logging Hygiene

Every log statement must avoid leaking:

- JWTs in any context (no `Authorization` header dumps)
- Verification codes (the `AuthService` logs only "code generated" / "code validated")
- Email addresses **except** when they are the subject of the operation (e.g. user creation)
- File contents

The `ILogger` template format `{UserId}` is used so structured-logging sinks can apply per-field redaction policies.

---

## 7. Threat Model Summary

| Threat | Vector | Control |
|---|---|---|
| Credential theft | None — no passwords | Verification-code flow |
| Token theft | Stolen JWT | Short expiry, future blocklist |
| SQL injection | Hostile input on search/filter | Parameterised queries everywhere |
| XSS | User-supplied strings rendered | Razor encoding + explicit `HtmlEncode` |
| CSRF | Cross-site form post | JWT bearer token (not cookie) — natively immune |
| File upload abuse | Malformed / oversized image | Content-type allow-list, size cap, server-side filename |
| Path traversal | Filename in upload | Filenames computed from `userId`, never user-controlled |
| Privilege escalation | Manipulated UI | Server-side `RequireAuthorization("…")` policies |
| MITM | Plaintext network | HTTPS / TLS, JWT signed |
| Brute force on codes | Repeated guesses | 10-minute TTL, one-shot delete; per-IP rate limit recommended |

---

## 8. Reviewer Checklist

- [ ] No string-concatenated SQL anywhere.
- [ ] Every endpoint that mutates state has a `RequireAuthorization("…")` policy.
- [ ] HTTPS redirection enabled in non-dev environments.
- [ ] JWT secret is sourced from an environment variable in production.
- [ ] Verification codes are deleted after first valid use.
- [ ] User-supplied filenames are never used in server-side paths.
- [ ] User-supplied strings rendered into HTML are encoded.
- [ ] CORS policy uses an explicit origin allow-list.
- [ ] `appsettings.{Environment}.json` is .gitignored.

---

## 9. Curriculum Alignment

| Rubric phrase | Realisation | Section |
|---|---|---|
| "Multiple permission levels" (mandatory #8) | Three roles + sub-roles + JWT claims | §2 |
| "Encryption … sensitive data" (§10) | JWT HMAC-SHA256, HTTPS, planned PBKDF2 codes | §1, §4 |
| "Protection against SQL Injection" (§10) | Parameterised queries throughout the data layer | §3.1 |
| "External service" — SMTP secured (§10) | TLS-enabled SMTP, retry on transient | covered in `04-networking-integrations.md` §5 |
