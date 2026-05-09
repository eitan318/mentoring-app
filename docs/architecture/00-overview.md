# MentoringApp — Architecture Overview

> A deep-dive series for engineers joining the MentoringApp codebase. The goal is to convey not only *what* the system does but *why* each architectural choice was made, and *how* it is realized in C#.

---

## 1. System Purpose

MentoringApp is a mentoring-management platform that pairs student mentors with student mentees, supervised by school staff and governed by administrators. It is delivered through three coordinated front-ends:

| Front-end | Project | Target | Role |
|---|---|---|---|
| Native desktop | `MentoringApp.Desktop` (WPF) | `net9.0-windows` | Primary administrative client |
| Web | `MentoringApp.Web` (Blazor Server) | `net9.0` | Cross-platform browser client |
| Backend | `MentoringApp.Api` (ASP.NET Core) | `net9.0` | JSON/JWT REST surface |

All three are powered by a single set of business rules, domain models, and ViewModels.

## 2. Solution Topology

```
src/
├── MentoringApp.Model/        ← Domain models, enums, records (the universal language)
├── MentoringApp.Data/         ← Repository abstractions + raw-ADO.NET SQLite implementation
├── MentoringApp.Service/      ← Business logic, validators, mappers, Result<T>
├── MentoringApp.ViewModel/    ← MVVM, navigation, stores, localization, IViewService contracts
├── MentoringApp.ApiClient/    ← Strongly-typed HttpClient wrappers
├── MentoringApp.Api/          ← ASP.NET Core minimal-API host with JWT auth
├── MentoringApp.Desktop/      ← WPF shell, Views, Styles, Converters, DI bootstrap
└── MentoringApp.Web/          ← Blazor Server shell + WebNavigationService

tests/
├── MentoringApp.Tests/             ← Service + Data unit tests (Xunit)
├── MentoringApp.ViewModel.Tests/   ← ViewModel-level tests with fake HttpHandlers
└── MentoringApp.E2eTests/          ← Playwright E2E covering the Web client
```

## 3. Layered Flow

The runtime composes a unidirectional layer stack:

```
View (XAML / Razor)
    │ binds to
    ▼
ViewModel  ─────────► UserStore / NavigationService (singletons)
    │ calls
    ▼
ApiClient (Desktop/Web)         ◄──────►   Service (Api host)
                                              │
                                              ▼
                                          Repository (interfaces)
                                              │
                                              ▼
                                          SQLite (raw ADO.NET)
```

The crucial inversion: **clients never reach a repository directly**; they cross the network through `ApiClient` → `Api` → `Service` → `Repository`. This is the seam that lets the same ViewModels run unmodified on WPF and Blazor.

## 4. Document Map

### 4.1 Core architecture (read first)

| File | Theme |
|---|---|
| [`01-design-patterns.md`](01-design-patterns.md) | MVVM toolkit, Service Layer separation, modelling philosophy, inheritance vs. records, **events & delegates** |
| [`02-data-persistence.md`](02-data-persistence.md) | Repository pattern, DAO separation, mappers, validators, configuration storage, **DB normalisation, async DB, SQL injection prevention** |
| [`03-navigation-and-ui.md`](03-navigation-and-ui.md) | Stack-of-stacks navigation, WPF/Web parity, DataTemplates, shared ViewModels, **user controls, value converters, permission-aware UI** |
| [`04-networking-integrations.md`](04-networking-integrations.md) | API client base, HTTP conversion logic, email subsystem, **stateless architecture, file transfer, external services** |
| [`05-engineering-tooling.md`](05-engineering-tooling.md) | DI container wiring, project independence, advanced C#, testing strategy, **async tooling** |

### 4.2 Deep-dive modules (rubric-mapped)

| File | Curriculum touchpoint |
|---|---|
| [`06-async-and-delegates.md`](06-async-and-delegates.md) | Async-track requirement: "asynchronous mechanism using delegates" |
| [`07-authorization-and-permissions.md`](07-authorization-and-permissions.md) | Mandatory #8: "multiple permission levels with role-aware UI" |
| [`08-validation-and-converters.md`](08-validation-and-converters.md) | Extension §10: "value converter classes" + "validation classes" |
| [`09-file-management.md`](09-file-management.md) | Extension §9: "file transfer between server and client" + §10: "XML storage" |
| [`10-security.md`](10-security.md) | Mandatory #8 + Extension §10 ("encryption", "SQL injection prevention") |
| [`11-system-analysis.md`](11-system-analysis.md) | Administrative-Systems track: initiation doc, ERD, Use-Case, Activity Diagram |
| [`12-bonus-and-extensions.md`](12-bonus-and-extensions.md) | Bonus 10 pts: matching cascade + ML.NET / MAUI / async-timer roadmap |

## 5. Reading Order

Read `00` then `01`. After that the other documents are independent and may be consumed in any order driven by your task:

- **Front-end work** → `03`, `08`, `09`
- **Back-end work** → `02`, `04`, `06`
- **Security review** → `07`, `10`
- **Examiner / project review** → `11`, `12` plus the alignment tables in every chapter

## 6. Curriculum Alignment Index

Every chapter ends with a *Curriculum Alignment* table mapping the chapter's content to specific clauses of the *Eval_Int_Async_DB* rubric (Network-Services + Async + Administrative-Systems tracks). The fastest way to verify rubric coverage is to scan those tables in order.
