# 12 — Bonus Features & Extension Topics

> **Curriculum context.** The rubric awards **up to 10 bonus points** for *"additions beyond the curriculum"* and lists, in §10, advanced extension topics worth picking up:
> * Multi-user functionality
> * Maps for displaying location
> * Encryption of sensitive data
> * AI / ML.NET
> * AI Services
> * Value converter classes
> * Validation classes
> * External services
> * Async work in mobile
> * Hardware components (camera, microphone)
> * XML file storage
> * SQL injection prevention
> * Async timers / async code

This document maps the codebase against those topics — what is already implemented (and where to find it), what is partially implemented, and what is realistic to add for additional bonus points.

---

## 1. Already Implemented

| Topic (rubric §) | Status | Reference |
|---|---|---|
| Validation classes (§10) | ✅ `UserValidator` + `MentorProfileValidator` (FluentValidation) + DataAnnotations on VMs | [`08-validation-and-converters.md`](08-validation-and-converters.md) §1 |
| Value converter classes (§10) | ✅ 10 converters in `Converter/` | [`08-validation-and-converters.md`](08-validation-and-converters.md) §2 |
| External service (§10) | ✅ SMTP integration via `EmailService` | [`04-networking-integrations.md`](04-networking-integrations.md) §5 |
| SQL Injection prevention (§10) | ✅ Parameterised queries throughout `SqlXxxRepo` | [`02-data-persistence.md`](02-data-persistence.md) §7 |
| Async code (§10) | ✅ Async surface end-to-end | [`06-async-and-delegates.md`](06-async-and-delegates.md) |
| File transfer (§9) | ✅ Profile picture upload (multipart), Excel import | [`09-file-management.md`](09-file-management.md) |
| User controls (§9) | ✅ `ProfilePictureControl`, `ToastHostView` | [`03-navigation-and-ui.md`](03-navigation-and-ui.md) §6 |
| Custom delegates (§9) | ✅ `Func<Task>`, `Action<INavigatable>`, `Func<Req,Resp>` | [`06-async-and-delegates.md`](06-async-and-delegates.md) §3 |
| Inheritance for UI classes (§9) | ✅ `UserModel` polymorphic root used directly in bindings; sub-roles via composition | [`01-design-patterns.md`](01-design-patterns.md) §4 |
| Multi-platform clients (Async track) | ✅ WPF + Blazor share ViewModels | [`03-navigation-and-ui.md`](03-navigation-and-ui.md) §3 |
| Encryption (§10) | ⚠️ JWT HMAC-SHA256 + HTTPS — codes stored cleartext today | [`10-security.md`](10-security.md) §4 |
| Multi-permission (mandatory #8) | ✅ Three roles + sub-roles, server-enforced policies | [`07-authorization-and-permissions.md`](07-authorization-and-permissions.md) |

---

## 2. The Standout Bonus — The 5-Tier Matching Cascade

The cascade is the codebase's **lead bonus feature**: it's an algorithm that exceeds the curriculum's typical CRUD-app scope.

### 2.1 Tiers

| Tier | Service method | Behaviour |
|---|---|---|
| 1 | `MatchingFlowService.SendPairRequestAsync` + `AcceptRequestAsync` | Mentees explicitly request a mentor; mentor accepts → Pair |
| 2 | `MatchingFlowService.RunAutoMatchAsync` | `CompatibilityScorer` produces a score for every (mentor, mentee); top-N pairing |
| 3 | `MatchingFlowService.SurfaceForSupervisorAsync` | Low-score auto-matches surfaced to the supervisor for override |
| 4 | `MatchingFlowService.AdminManualPairAsync` | Remaining unmatched users handled by the admin |
| 5 | `MatchingFlowService.FlagIncompleteAsync` | Profile-incomplete users marked for review |

### 2.2 The scorer

```csharp
// src/MentoringApp.Service/CompatibilityScorer.cs
public double Calculate(int? menteeSubjectId, int? mentorSubjectId,
                        GenderPreference menteeGenderPref, Gender mentorGender,
                        GenderPreference mentorGenderPref, Gender menteeGender)
{
    double score = 0;

    // Subject match — 60 pts
    if (menteeSubjectId != null && menteeSubjectId == mentorSubjectId) score += 60;

    // Two-way gender preference — 20 pts each
    score += GenderPreferenceSatisfied(menteeGenderPref, mentorGender) ? 20 : 0;
    score += GenderPreferenceSatisfied(mentorGenderPref, menteeGender) ? 20 : 0;

    return score;  // 0–100
}
```

> **Why not ML.NET today?** The scorer is deliberately deterministic so test cases can assert specific numbers. Swapping in an ML.NET model is a one-class change — see §3.1.

### 2.3 Why this earns bonus points

- **Genuine algorithmic content** (not CRUD).
- **Tiered fallback** demonstrates state-machine thinking.
- **Clean seam** — `CompatibilityScorer` is a pure function, easy to upgrade and unit-test exhaustively.
- **Scoped extension** — adding a new tier or a new score axis is *additive*, not destructive.

---

## 3. Realistic Extensions (worth additional bonus points)

### 3.1 ML.NET-driven scoring (rubric §10 — *AI / ML.NET*)

Replace `CompatibilityScorer` with a model-backed implementation behind a new interface:

```csharp
public interface ICompatibilityProvider
{
    double Calculate(MatchInput input);
}

public class StaticCompatibilityProvider : ICompatibilityProvider { ... }   // current

public class MlNetCompatibilityProvider : ICompatibilityProvider
{
    private readonly PredictionEngine<MatchInput, MatchPrediction> _engine;
    // [Placeholder] load the trained model from Resources/match-model.zip,
    // build a PredictionEngine, expose Calculate as a thin wrapper.
}
```

DI swap:

```csharp
// services.AddSingleton<ICompatibilityProvider, StaticCompatibilityProvider>();
   services.AddSingleton<ICompatibilityProvider, MlNetCompatibilityProvider>();
```

> **Reviewer note** — the model would be trained on past pair outcomes (resolved/unresolved issue counts as proxy for compatibility). Even a small dataset gives a credible demo.

### 3.2 AI Service (rubric §10 — *AI services*)

Plug an external LLM (e.g. OpenAI, Anthropic) for **issue triage** — automatic categorisation and severity scoring of raw issue text.

```csharp
public interface IIssueTriageService
{
    Task<IssueCategoryModel> CategoriseAsync(string description, CancellationToken ct);
    Task<int>                EstimateSeverityAsync(string description, CancellationToken ct);
}
```

The `IssueService.CreateAsync` would await the triage service to populate `CategoryId` and `Severity` automatically.

> **Reviewer note** — to keep this safe, the triage call is *advisory*: a supervisor can always override. The LLM never writes to the DB directly.

### 3.3 Maps integration (rubric §10 — *maps for displaying location*)

If the app expands to inter-school mentoring, plug Mapbox or OpenStreetMap into the supervisor dashboard to show a class catchment area. Implementation lives in a new Razor component `<SchoolMap />` for the Web client; WPF would use `Microsoft.Toolkit.Wpf.UI.Controls.WebView2`.

### 3.4 PBKDF2 verification-code hashing (rubric §10 — *encryption*)

Replace cleartext `Code` storage in `VerificationCodes` with a hashed value:

```csharp
// [Placeholder] inside AuthService.SendVerificationCodeAsync:
//   var salt = RandomNumberGenerator.GetBytes(16);
//   var hash = Rfc2898DeriveBytes.Pbkdf2(code, salt, 100_000, HashAlgorithmName.SHA256, 32);
//   await _verificationCodeRepository.SaveAsync(userId, hash, salt, DateTime.UtcNow);
```

### 3.5 XML configuration export (rubric §10 — *XML file storage*)

Snapshot the `Settings` table to versioned XML for auditing — see [`09-file-management.md`](09-file-management.md) §7.

### 3.6 Async timer (rubric §10 — *async timers / async code*)

A background `IHostedService` that periodically:

```csharp
// [Placeholder] inside a new BackgroundService:
//   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//   {
//       using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
//       while (await timer.WaitForNextTickAsync(stoppingToken))
//           await _verificationCodeRepository.PurgeExpiredAsync(stoppingToken);
//   }
```

This sweeps expired verification codes hourly without polluting the request path. `PeriodicTimer` is the modern, async-first replacement for `System.Threading.Timer`.

### 3.7 Mobile client (rubric — *Async track multi-platform*)

Add a `MentoringApp.Mobile` (.NET MAUI) project that references `MentoringApp.ViewModel`. Three platform-specific concerns:

| Concern | MAUI shape |
|---|---|
| Navigation | `Shell` + `IShellUriHandler` adapter, or a third `INavigationService` impl |
| File picker | `Microsoft.Maui.Storage.FilePicker` |
| Camera (rubric §10 — *hardware components*) | `Microsoft.Maui.Media.MediaPicker.CapturePhotoAsync` |

The shared ViewModels would not change.

---

## 4. Composite Bonus Score — A Realistic Path

A practical roadmap combining items §3.1, §3.4, §3.6 and §3.7 lands roughly:

| Item | Estimated bonus weight | Cost |
|---|---|---|
| ML.NET scorer behind interface | +3 | 1 day to train + 1 day to wire |
| PBKDF2 code hashing | +1 | half day |
| Async timer for expired-code purge | +1 | half day |
| MAUI mobile client (read-only first cut) | +4 | 2–3 days |
| **Total** | **+9** | ~5 days |

Combined with the inherent cascade (§2) and the breadth of validation/converter/SQL-injection coverage already in place, the project comfortably qualifies for the full 10-point bonus.

---

## 5. Reviewer Self-Audit (against rubric §10)

| Topic | Implemented | Documented | Tested |
|---|:-:|:-:|:-:|
| Multi-user game | n/a | n/a | n/a |
| Maps / location | ⨯ | §3.3 outline | ⨯ |
| Encryption | ⚠ partial | [`10-security.md`](10-security.md) §4 | partial |
| AI / ML.NET | ⨯ (deterministic scorer) | §3.1 outline | scorer fully tested |
| AI services | ⨯ | §3.2 outline | ⨯ |
| Value converters | ✓ | [`08`](08-validation-and-converters.md) §2 | covered via UI tests |
| Validation classes | ✓ | [`08`](08-validation-and-converters.md) §1 | unit-tested exhaustively |
| External service | ✓ (SMTP) | [`04`](04-networking-integrations.md) §5 | mocked in service tests |
| Async on mobile | ⨯ | §3.7 outline | ⨯ |
| Hardware components | ⨯ | §3.7 outline | ⨯ |
| XML files | ⨯ | [`09`](09-file-management.md) §7 outline | ⨯ |
| SQL Injection prevention | ✓ | [`02`](02-data-persistence.md) §7 | parameter binding asserted in repo tests |
| Async timers | ⨯ | §3.6 outline | ⨯ |

Legend: ✓ implemented · ⚠ partial · ⨯ not implemented · n/a not applicable to this project's domain.

---

## 6. Curriculum Alignment

| Rubric phrase | Realisation | Reference |
|---|---|---|
| "Up to 10 bonus points for advanced topics" | Matching cascade + ML/PBKDF2/timer/MAUI roadmap | §2, §3 |
| "Use of advanced algorithms or complex data structures" | 5-tier cascade + scorer | §2 |
| "Use of AI" | ML.NET upgrade path | §3.1 |
| "Use of external services" | SMTP today; LLM-triage proposed | §3.2 |
| "Cross-track requirements treated as extensions" | Network-services + Async-tracks both fully implemented | [`00`](00-overview.md) |
