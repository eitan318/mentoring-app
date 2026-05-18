# 07 — Authorization & Permission Hierarchy

> **Curriculum context.** Mandatory requirement #8 of the Israeli Ministry of Education *Integrated Data Applications* rubric:
> > "The project must handle multiple permission levels, in line with the project's logic. The user interface must contain only the options corresponding to the permission level."

This document describes the **role/permission model**, where it is enforced, and how the UI and the API stay in sync without duplicating logic.

---

## 1. The Role Model

### 1.1 Three primary roles

| Role | Class | Distinguishing capability |
|---|---|---|
| Admin | `AdminModel` | Manage users, configure settings, run matching, forward issues |
| Supervisor | `SupervisorModel` | Monitor pairs in their assigned classes, resolve issues, approve overrides |
| Student | `StudentModel` | Manage their own profile, request mentors / mentees, raise issues, write reviews |

### 1.2 Two student sub-roles via composition

`StudentModel` does **not** branch into `MentorStudent` / `MenteeStudent`; instead it carries optional sub-profiles:

```csharp
public partial class StudentModel : UserModel
{
    public MentorProfile? MentorProfile { get; set; }
    public MenteeProfile? MenteeProfile { get; set; }

    public bool IsMentor => MentorProfile is not null;
    public bool IsMentee => MenteeProfile is not null;
}
```

> **Why composition?** A real student can be **both** a mentor (for a younger pupil in maths) and a mentee (for a harder topic, e.g. physics). Inheritance would make this impossible; composition makes it natural.

### 1.3 Capability map

| Action | Admin | Supervisor | Mentor | Mentee |
|---|:-:|:-:|:-:|:-:|
| Configure system settings (matching weights, phase) | ✓ | | | |
| Run the matching cascade | ✓ | | | |
| Create / delete users | ✓ | | | |
| View all pairs | ✓ | own classes | own pair | own pair |
| Resolve / forward issues | ✓ | for own pairs | reporter only | reporter only |
| Approve manual override pair | ✓ | ✓ | | |
| Send mentor request | | | | ✓ |
| Accept / reject request | | | ✓ | |
| Write review | | | ✓ | ✓ |

---

## 2. Identity & Authentication Pipeline

The user is identified by **JWT claims** issued at login, after a two-step email verification flow.

### 2.1 Token issuance

```csharp
// src/MentoringApp.Api/Helpers/JwtHelper.cs
var role = user switch
{
    AdminModel      => "Admin",
    SupervisorModel => "Supervisor",
    StudentModel    => "Student",
    _               => "Student"
};

var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Name, user.UserName),
    new Claim("role",                       role),
    new Claim("language",                   user.Language),
    new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
};
```

Token is HMAC-SHA256 signed with the secret from `JwtSettings`.

> **Note** — the token does **not** carry the mentor/mentee distinction; that is loaded from the database when the server resolves `UserService.GetUserByIdAsync(claims.Sub)`. Sub-roles are too volatile to bake into a JWT.

### 2.2 Token validation

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidAudience            = jwtSettings.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });
```

> **Reviewer note** — `ValidateLifetime: true` is the default-but-easy-to-forget flag. Without it, expired tokens are accepted.

---

## 3. Server-Side Authorization

### 3.1 Policies declared once

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",         p => p.RequireRole("Admin"));
    options.AddPolicy("AdminOrSupervisor", p => p.RequireRole("Admin", "Supervisor"));
    options.AddPolicy("AnyAuthenticated",  p => p.RequireAuthenticatedUser());
});
```

### 3.2 Endpoint application

```csharp
// src/MentoringApp.Api/Endpoints/UserEndpoints.cs
group.MapGet("/", ...).RequireAuthorization("AdminOnly");
group.MapGet("/{id:int}", ...);                          // any authenticated user
group.MapPost("/", ...).RequireAuthorization("AdminOnly");
group.MapDelete("/{id:int}", ...).RequireAuthorization("AdminOnly");
group.MapPut("/{id:int}/grade-class", ...).RequireAuthorization("AdminOnly");
```

### 3.3 Resource-based authorization (custom)

For *"the supervisor can see students in their own classes only"* — a generic policy isn't enough; the rule depends on the resource. Implemented inline:

```csharp
group.MapGet("/students/by-supervisor/{supervisorId:int}",
    async (int supervisorId, ClaimsPrincipal user, UserService userService, ISchoolClassRepo classRepo) =>
{
    var callerId = int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    if (user.IsInRole("Supervisor") && callerId != supervisorId)
        return Results.Forbid();
    // ... fetch students ...
});
```

> **Reviewer note** — Resource-based authorization always belongs **next to the resource access**, not in middleware. Keep the predicate inline.

---

## 4. UI-Side Permission Awareness

The PDF rubric explicitly demands *"the UI must contain only the options corresponding to the permission level"*. Three layers of UI gating are used.

### 4.1 Shell selection at login (already covered in `03-navigation-and-ui.md` §8.1)

A Student literally cannot reach the Admin shell because the dispatch on `UserStore.Current` only resolves the matching shell ViewModel.

### 4.2 ViewModel-computed booleans

```csharp
public partial class IssueDetailViewModel : ObservableObject, INavigatable<IssueModel>
{
    public bool CanForwardToAdmin =>
        _userStore.Current is SupervisorModel sv &&
        _issue.AssignedSupervisorId == sv.Id &&
        !_issue.IsResolved;

    public bool CanResolve =>
        _userStore.Current is AdminModel ||
        (_userStore.Current is SupervisorModel sv2 && _issue.AssignedSupervisorId == sv2.Id);
}
```

Bound from XAML through the standard `BoolToVisibilityConverter`:

```xml
<Button Content="{Binding [Forward_To_Admin], Source={x:Static loc:TranslationSource.Instance}}"
        Visibility="{Binding CanForwardToAdmin, Converter={StaticResource BoolToVis}}"/>
```

### 4.3 `DataTemplate` selection by role

The role-specific dashboard is selected via the `DataTemplate`-per-VM-type mapping in `ViewModelViewMap.xaml` (see `03-navigation-and-ui.md`). A user never even has the wrong View loaded into memory.

---

## 5. The Cardinal Rule — Defence in Depth

> **Never trust the UI.** Hidden buttons are convenience, not security.

Every UI gate has a **mandatory** server-side counterpart. The client-side check exists only to:

1. Save a round trip on a forbidden action.
2. Avoid showing inert controls that would confuse the user.

If a malicious client unhid the button and submitted the request, the server would still reject it via `RequireAuthorization("…")` or an inline resource check. Removing the UI gate is a UX regression; removing the server check is a security incident.

### 5.1 Defence-in-depth checklist

- [ ] Every `[RequireAuthorization]` policy is declared in **one** place (`Program.cs`).
- [ ] Every endpoint that mutates state has a policy attached.
- [ ] Resource-based checks (`isOwner`, `isSupervisorOfClass`) are performed inside the endpoint, before the service call.
- [ ] The UI gate uses the *same* condition; if the predicate is non-trivial, both sides import a shared `Predicates` helper to stay aligned.
- [ ] Tests cover the negative case — a Student calling an Admin endpoint must receive `403`, not `200`.

---

## 6. Auditing & Logging

JWT claims (`JwtRegisteredClaimNames.Sub`, `Jti`) are surfaced in every log line written from the API host. The **`Jti`** (token unique id) is the auditing primitive — multiple actions performed under the same JWT can be correlated, and a leaked token can be revoked by adding its `Jti` to a blocklist (not currently implemented, listed as a future hardening item).

---

## 7. Future Hardening (out of scope today)

| Item | Risk addressed |
|---|---|
| Refresh tokens with rotation | Reduce JWT validity window |
| Server-side `Jti` blocklist | Force logout after suspected compromise |
| Per-IP rate limits on `/api/auth/send-code` | Brute-force resistance |
| Two-factor for Admin role | Privileged-account compromise |
| Permission audit log table | Forensics after incident |

---

## 8. Curriculum Alignment

| Rubric concept | Realisation | Section |
|---|---|---|
| Multiple permission levels (mandatory #8) | `AdminModel` / `SupervisorModel` / `StudentModel{Mentor,Mentee}` | §1 |
| UI shows only allowed options (mandatory #8) | Shell selection + `CanXxx` bindings | §4 |
| Server-side enforcement | JWT + `RequireAuthorization` policies | §3 |
| Inheritance for identity (extension §9) | `UserModel` polymorphic root | §1 |
| Encryption of sensitive data (extension §10) | JWT HMAC-SHA256 signing | §2 |
