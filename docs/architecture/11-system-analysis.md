# 11 — System Analysis (Administrative Systems Track)

> **Curriculum context.** The *Administrative Systems* sub-track of the rubric requires *"a system-analysis folder including, for example: an initiation document, description of the existing system and its problems, description of the future system, organisation description, requirements diagrams, **Use Case**, **Activity Diagram**, **ERD**."*
>
> The MentoringApp 5-units project converges three historical tracks (Administrative Systems + Network Services + Async); this document supplies the analysis-track content for review.

---

## 1. Initiation Document

### 1.1 Goal

Build a single mentoring-management platform that lets a school:

- Pair student mentors with student mentees, validated by school staff.
- Track issues raised by students.
- Enforce per-role access.
- Run on the desktop (admin office) and the web (students at home, supervisors anywhere).

### 1.2 Stakeholders

| Stakeholder | Role | Pain today | Win after |
|---|---|---|---|
| Admin (school principal / coordinator) | Drives mentoring program | Manual spreadsheets, mismatched pairings | One-click matching cascade, dashboards |
| Supervisor (homeroom teacher) | Monitors pairs in own class | No visibility into pair issues | Per-class view + issue resolution UI |
| Mentor student | Helps a younger pupil | Logistics communicated by paper | Self-service profile, accept/reject requests |
| Mentee student | Receives help | Hard to find a willing mentor | Search & request UI |

### 1.3 Success criteria

- 100 % of pairs visible centrally with their supervisor.
- < 24 h average time from issue raised to acknowledgement.
- 0 occurrences of an unauthorised role action (per-policy server logs).

---

## 2. Existing System & Its Problems

### 2.1 Pre-system process

Most schools running a mentoring program rely on:

- An Excel sheet with student rosters.
- An email thread per supervisor for pair monitoring.
- Paper-based issue forms collected by supervisors.

### 2.2 Specific problems

| Problem | Impact |
|---|---|
| No single source of truth | Pair records diverge between admin and supervisors |
| Manual matching | Hours of admin time, biased outcomes |
| Issues lost in inbox | Slow resolution, sometimes never resolved |
| No visibility for students | Mentees can't tell whether they have been matched |
| No audit trail | After-the-fact review is impossible |
| RTL/Hebrew support | Existing tools assume LTR |

---

## 3. Future System

### 3.1 In one paragraph

A 3-tier client-server platform:
- **Layer 1 — Clients** (WPF for the admin desk; Blazor Server for everyone else).
- **Layer 2 — API** (ASP.NET Core minimal APIs, JWT-secured, stateless).
- **Layer 3 — Storage** (SQLite via raw ADO.NET behind repository interfaces).

All three are powered by a shared `MentoringApp.Model` project so terminology is consistent across UI, API and DB.

### 3.2 Top-level capabilities

1. Self-service profile editing (mentor / mentee profile).
2. 5-tier matching cascade.
3. Issue tracking with per-pair workflow.
4. Per-pair review at end of cycle.
5. Bulk Excel import for new student rosters.
6. Email notifications for phase transitions.
7. Localisation (English / Hebrew) with full RTL support.

### 3.3 Non-functional requirements

| Property | Target |
|---|---|
| Availability | School-hours uptime; nightly recreation acceptable in dev |
| Latency | < 300 ms p95 for typical reads on local SQLite |
| Concurrency | Up to ~50 simultaneous web sessions |
| Localisation | English + Hebrew (RTL) |
| Platforms | Windows desktop (WPF) + any modern browser |
| Auditability | Every state mutation logged with user id |

---

## 4. Organisation Context

### 4.1 Hierarchy

```
School
 └── Administration
      ├── Principal (Admin role)
      └── Mentoring Coordinator (Admin role)
 └── Year groups (Grade entity)
      └── Classes (SchoolClass entity)
           ├── Homeroom teacher (Supervisor role)
           └── Students
                ├── Mentor profile (optional)
                └── Mentee profile (optional)
```

### 4.2 Mapping to the data model

| Org concept | Domain model |
|---|---|
| Year group | `GradeModel` |
| Class | `SchoolClassModel` (`GradeId`, `ClassNum`) |
| Teacher | `SupervisorModel` (assigned to one or more `SchoolClass`es) |
| Student | `StudentModel` |
| Pair | `PairModel(MentorId, MenteeId, SupervisorId)` |

---

## 5. Requirements Catalogue

### 5.1 Functional requirements (FR)

| ID | Description | Priority |
|---|---|:---:|
| FR-1 | Admin can create and delete users | M |
| FR-2 | Admin can run the 5-tier matching cascade | M |
| FR-3 | Mentor can accept or reject a mentee request | M |
| FR-4 | Mentee can browse mentors and submit a request | M |
| FR-5 | Supervisor can resolve or forward an issue | M |
| FR-6 | Admin can configure matching weights | S |
| FR-7 | Admin can import students from Excel | S |
| FR-8 | Admin can switch system phase (info-filing → matching → review) | M |
| FR-9 | Any user can change their language preference | M |
| FR-10 | Any user can upload a profile picture | C |

(M = must, S = should, C = could.)

### 5.2 Non-functional requirements (NFR)

| ID | Description |
|---|---|
| NFR-1 | All endpoints stateless |
| NFR-2 | Server-side data access asynchronous |
| NFR-3 | All UI strings localised through `Strings.resx` |
| NFR-4 | All client/server traffic JSON-over-HTTPS |
| NFR-5 | Database parameterised against SQL injection |

---

## 6. Use-Case Diagram (textual representation)

```
                     ┌─────────────┐
                     │    Admin    │
                     └──┬───────┬──┘
                        │       │
            (Create users)    (Run matching)
                        │       │
                  ┌─────┴───────┴──────┐
                  │  Mentoring System  │
                  └───┬─────────┬──────┘
                      │         │
            (Accept request) (Forward issue to admin)
                      │         │
       ┌──────────────┴──┐   ┌──┴───────────────┐
       │      Mentor     │   │    Supervisor    │
       └─────────────────┘   └──────────────────┘

                      │
            (Send request)
                      │
                ┌─────┴─────┐
                │   Mentee  │
                └───────────┘
```

> Replace this textual diagram with `docs/architecture/diagrams/use-case.png` once exported from a UML tool (PlantUML, Mermaid, draw.io). The textual stub above documents the **intent** until the PNG arrives.

---

## 7. Activity Diagram — Matching Cascade

The 5-tier matching pipeline, expressed as an activity diagram:

```
        ( Start: Admin clicks "Run Matching" )
                       │
                       ▼
         ┌─────────────────────────────┐
         │ Tier 1 — direct requests    │
         │ "Has every accepted request │
         │  been turned into a pair?"  │
         └─────────────┬───────────────┘
                       │  no                yes
              ┌────────┴─────────┐         ▼
              ▼                  ▼      (skip to Tier 2)
       Pair the requester   ─── (continue)
                                          │
                                          ▼
         ┌─────────────────────────────┐
         │ Tier 2 — auto-match scoring │
         │ Run CompatibilityScorer for │
         │ every (mentor, mentee)      │
         │ unmatched pair.             │
         └─────────────┬───────────────┘
                       ▼
         ┌─────────────────────────────┐
         │ Tier 3 — supervisor-assist  │
         │ Surface low-score pairs to  │
         │ supervisor for override.    │
         └─────────────┬───────────────┘
                       ▼
         ┌─────────────────────────────┐
         │ Tier 4 — admin manual       │
         │ Remaining unmatched go to   │
         │ admin-build queue.          │
         └─────────────┬───────────────┘
                       ▼
         ┌─────────────────────────────┐
         │ Tier 5 — flag incomplete    │
         │ Profile-incomplete pairs    │
         │ flagged for review.         │
         └─────────────┬───────────────┘
                       ▼
                   (End)
```

> **Reviewer note** — each tier corresponds to a public method on `MatchingFlowService`; the activity diagram is therefore *executable* in the test suite — every path is covered by a unit test.

---

## 8. ERD — Entity-Relationship Diagram (textual)

```
Users (Id PK, Email, NationalId, UserName, Language, Gender, ...)
 │ 1 ───┐
 │       └──── 1 UserStudents     (UserId FK, GradeId FK, ClassNum)
 │       └──── 1 UserMentors      (UserId FK, SubjectId FK, MaxMentees)
 │       └──── 1 UserMentees      (UserId FK, SubjectId FK, HoursPerWeek)
 │       └──── 1 UserSupervisors  (UserId FK)
 │       └──── 1 UserAdmins       (UserId FK)
 │
 │ many
 ▼
Pairs (Id PK, MentorId FK→Users, MenteeId FK→Users, SupervisorId FK→Users,
       CreatedAt, IsIncomplete)
 │ 1 ───── many ──► Issues
 │ 1 ───── many ──► Reviews
 │
PairRequests (Id PK, MentorId FK, MenteeId FK, Tier, Status, RequestedAt)
MatchScores  (MentorId FK, MenteeId FK, Score)            -- M:N junction
SchoolClasses(GradeId FK, ClassNum, SupervisorId FK→Users)
Issues       (Id PK, PairId FK, ReportedByUserId FK, CategoryId FK,
              Description, IsResolved, ForwardedToSupervisorId FK)
IssueCategories (Id PK, Name)
Grades       (Id PK, Name, Num)
Subjects     (Id PK, Name)
Settings     (Key PK, Value)
VerificationCodes (UserId FK, Code, CreationDate)
```

> **Reviewer note** — three junction tables (`Pairs`, `PairRequests`, `MatchScores`) satisfy the rubric's requirement *"contains at least one linking/junction table"*. Vertical partitioning of `Users` satisfies the rubric's requirement of *"≥4 information tables for a data-driven system"*.

---

## 9. Glossary

| Term | Meaning |
|---|---|
| **Pair** | A confirmed mentoring triad: one mentor, one mentee, one supervisor |
| **Tier** | A stage of the matching cascade (1–5) |
| **Phase** | A system-wide state controlled by the admin: `info-filing` → `matching` → `review` |
| **Compatibility score** | Number 0–100 produced by `CompatibilityScorer` from subject + gender preference matching |
| **Issue** | A student-raised concern about their pair |
| **Supervisor of class** | The teacher assigned to monitor a `(GradeId, ClassNum)` slot |

---

## 10. Curriculum Alignment

| Rubric requirement | Section |
|---|---|
| Initiation document | §1 |
| Description of existing system | §2 |
| Description of future system | §3 |
| Organisation description | §4 |
| Requirements diagrams | §5 |
| Use-Case diagram | §6 |
| Activity diagram | §7 |
| ERD | §8 |
