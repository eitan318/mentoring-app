# MentoringApp

A full-stack mentoring management platform for schools that automates mentor-mentee pairing, session tracking, and issue escalation — available as both a native Windows desktop app and a cross-platform web client.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Run with Docker](#run-with-docker)
  - [Run Locally](#run-locally)
- [Configuration](#configuration)
- [Testing](#testing)
- [CI/CD](#cicd)
- [Documentation](#documentation)

---

## Overview

MentoringApp manages peer-mentoring programs within schools. It automates the process of pairing student mentors with student mentees, provides supervisors with oversight tools, and gives administrators full control over users, pairs, and system settings.

**Three client interfaces ship from one codebase:**

| Client | Technology | Primary Users |
|--------|-----------|---------------|
| Desktop | WPF (.NET 9, Windows) | Admins, Supervisors |
| Web | Blazor WebAssembly | Students, Supervisors |
| API | ASP.NET Core 9 | Shared backend |

---

## Features

### User Roles
- **Students** — can be a mentor (teach), a mentee (learn), or both
- **Supervisors** — oversee assigned pairs and handle escalated issues
- **Admins** — manage users, pairs, settings, and bulk-import via Excel

### 5-Tier Matching Algorithm
Pairs are created through a cascade:
1. **Direct Request** — mentee explicitly requests a mentor
2. **Auto-Match** — `CompatibilityScorer` ranks mentors by subject, grade, and gender preference
3. **Supervisor-Assisted** — supervisor manually selects from candidate list
4. **Admin Manual** — administrator creates a pair directly
5. **Flagged** — incomplete profiles are queued for review

### Sessions & Reviews
- Mentors log session hours and submit feedback per session
- Supervisors review session quality

### Issue Tracking
- Any user can report a problem against a pair
- Issues escalate: Student → Supervisor → Admin

### Internationalization
- Full English and Hebrew (RTL) support via `.resx` resource files
- Flow direction bound dynamically in XAML

### Bulk Import
- Admins import users from Excel via ClosedXML

---

## Architecture

```
Desktop (WPF) ──────────────────────────────┐
                                             │
Web (Blazor WASM) ──▶ ApiClient ──▶ REST API ──▶ PostgreSQL / SQLite
                                             │
                               Service Layer─┘
                              (Matching, Email, Validation)
```

**Layered dependency flow:**

```
View → ViewModel → Service → Repository → Database
```

| Layer | Project | Responsibility |
|-------|---------|----------------|
| View | `MentoringApp.Desktop`, `MentoringApp.Web` | UI, DI bootstrap |
| ViewModel | `MentoringApp.ViewModel` | MVVM state, navigation, commands |
| Service | `MentoringApp.Service` | Business rules, matching, email, Excel |
| Model | `MentoringApp.Model` | Domain entities, enums, Result pattern |
| Data | `MentoringApp.Data` | Repository interfaces + SQLite/ADO.NET impl |
| API | `MentoringApp.Api` | REST endpoints, JWT auth, Swagger |
| ApiClient | `MentoringApp.ApiClient` | Typed HTTP client for web/desktop |

**Key design decisions:**
- **No ORM** — raw ADO.NET with typed repository interfaces; EF Core used only for schema management
- **Result\<T\>** — services return explicit success/failure instead of exceptions
- **Stack-based navigation** — `INavigationService` with `OnNavigatedTo`/`OnNavigatedFrom` lifecycle hooks; identical interface on WPF and Blazor
- **CommunityToolkit.Mvvm** — source-generator-based ViewModels (zero reflection at runtime)
- **Scoped repositories, Transient ViewModels, Singleton services** — DI lifetimes enforced in `App.xaml.cs`

---

## Project Structure

```
MentoringApp.sln
├── src/
│   ├── MentoringApp.Model/          Domain models & enums
│   ├── MentoringApp.Data/           Repository pattern & SQLite access
│   │   └── Resources/Database/      mentoring.db (checked in)
│   ├── MentoringApp.Service/        Business logic, matching, email, Excel
│   ├── MentoringApp.ViewModel/      MVVM ViewModels, navigation, localization
│   ├── MentoringApp.Desktop/        WPF app (Views, Styles, DI, appsettings)
│   ├── MentoringApp.Api/            ASP.NET Core REST API
│   ├── MentoringApp.ApiClient/      Typed HTTP client
│   ├── MentoringApp.Components/     Shared Blazor components
│   └── MentoringApp.Web/            Blazor WebAssembly client
├── Tests/
│   ├── MentoringApp.Tests/          Unit tests (Service, Model, Data)
│   ├── MentoringApp.ViewModel.Tests/ Unit tests (ViewModels)
│   └── MentoringApp.E2eTests/       Playwright E2E tests (Web client)
├── docs/architecture/               Architecture decision records
├── docker-compose.yml
├── Dockerfile
└── .gitlab-ci.yml
```

---

## Tech Stack

| Concern | Library / Framework | Version |
|---------|-------------------|---------|
| Runtime | .NET | 9.0 |
| Desktop UI | WPF | net9.0-windows |
| Web UI | Blazor WebAssembly | net9.0 |
| API | ASP.NET Core | 9.0 |
| MVVM | CommunityToolkit.Mvvm | 8.4.0 |
| Authentication | JWT Bearer | 9.0.4 |
| API Docs | Swashbuckle (Swagger) | 6.9.0 |
| Validation | FluentValidation | 12.1.1 |
| Excel | ClosedXML | 0.105.0 |
| Database | SQLite (dev) / PostgreSQL (prod) | — |
| Unit Tests | xUnit + Moq + FluentAssertions | 2.9.3 / 4.20.72 / 7.0.0 |
| E2E Tests | Playwright + NUnit | 1.59.0 |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Windows (required for the WPF desktop client)
- Docker & Docker Compose (optional, for containerized API + PostgreSQL)

### Run with Docker

```bash
# Start PostgreSQL + API
docker-compose up --build

# API available at http://localhost:8080
# Swagger UI at http://localhost:8080/swagger
```

Copy `.env.example` (or set environment variables) for secrets before running:

```
JWT_SECRET=your-32-char-minimum-secret-here
SMTP_FROM_EMAIL=you@gmail.com
SMTP_FROM_PASSWORD=your-app-password
```

### Run Locally

**1. Start the API**

```bash
dotnet run --project src/MentoringApp.Api/MentoringApp.Api.csproj
```

**2. Run the Web client**

```bash
dotnet run --project src/MentoringApp.Web/MentoringApp.Web.csproj
```

**3. Run the Desktop client** (Windows only)

```bash
dotnet run --project src/MentoringApp.Desktop/MentoringApp.csproj
```

The desktop app reads `src/MentoringApp.Desktop/appsettings.json` for the API base URL (`https://localhost:7233` by default).

> **Note:** On first launch, the application drops and recreates the SQLite database, then seeds it with demo data. This is controlled by the `recreateInitialDb` flag in `App.xaml.cs`.

---

## Configuration

### API — `src/MentoringApp.Api/appsettings.json`

```jsonc
{
  "DataProvider": "postgres",          // "sqlite" for local dev
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=mentoringapp;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "CHANGE_ME_TO_A_32_CHAR_MIN_SECRET",
    "Issuer": "MentoringApp",
    "Audience": "MentoringApp",
    "ExpiryHours": 8
  },
  "AllowedOrigins": "http://localhost:5173,http://localhost:5041",
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "FromEmail": "",
    "FromPassword": ""       // Use a Gmail App Password
  }
}
```

### Desktop — `src/MentoringApp.Desktop/appsettings.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7233"
  }
}
```

---

## Testing

```bash
# Unit tests (Service, Model, Data layers)
dotnet test Tests/MentoringApp.Tests/MentoringApp.Tests.csproj

# ViewModel unit tests
dotnet test Tests/MentoringApp.ViewModel.Tests/MentoringApp.ViewModel.Tests.csproj

# E2E tests — requires the API and Web client running first
dotnet test Tests/MentoringApp.E2eTests/MentoringApp.E2eTests.csproj
```

**Unit test coverage includes:**
- `CompatibilityScorerTests` — matching algorithm scoring
- `MatchingFlowServiceTests` — 5-tier cascade logic
- `PairServiceTests`, `AuthServiceTests`, `IssueServiceTests`, `ReviewServiceTests`
- `Result<T>` pattern, domain model invariants, data mappers

**E2E test coverage (Playwright):**
- Login flow
- Admin dashboard, pair creation, pair management
- Student dashboard
- Supervisor dashboard

---

## CI/CD

GitLab CI pipeline (`.gitlab-ci.yml`) runs on `main` and `develop`:

| Stage | What it does |
|-------|-------------|
| **build** | `dotnet build`, `dotnet test`, publishes API artifact |
| **migrate** | Runs EF Core migrations against PostgreSQL |
| **deploy** | Placeholder for SSH / kubectl / Docker stack deployment |

Docker image: `mcr.microsoft.com/dotnet/sdk:9.0`

---

## Documentation

Detailed architecture documentation lives in [`docs/architecture/`](docs/architecture/):

| Document | Topic |
|----------|-------|
| `00-overview.md` | System purpose, solution topology, layer flow |
| `01-design-patterns.md` | MVVM, CommunityToolkit patterns, service philosophy |
| `02-data-persistence.md` | Repository pattern, DAOs, mappers, validators |
| `03-navigation-and-ui.md` | Stack-based navigation, WPF/Blazor parity |
| `04-networking-integrations.md` | API client, HTTP layer, email subsystem |
| `05-engineering-tooling.md` | DI wiring, project independence, testing strategy |
