namespace MentoringApp.Model;

// ─────────────────────────────────────────────────────────────────────────────
// Data Transfer Objects (DTOs) for the HTTP API. Each *Request is a body the
// desktop client POSTs/PUTs; each *Response is the JSON the API returns. They
// are immutable C# records and exist to keep the wire contract separate from
// the rich domain models. Used by MentoringApp.Api (endpoints) and
// MentoringApp.ApiClient (typed clients).
// ─────────────────────────────────────────────────────────────────────────────

// ── App config (fetched by clients on startup) ────────────────────────────────
/// <summary>Server flags the client reads at startup (e.g. whether the email verification step is skipped in dev).</summary>
public record AppConfigResponse(bool RecreateDbOnStartup, bool SkipVerificationCode);

// ── Auth ──────────────────────────────────────────────────────────────────────
public record SendCodeRequest(string NationalId);
/// <param name="DevCode">Populated only in dev mode; empty string in production.</param>
public record SendCodeResponse(string? DevCode);
public record LoginRequest(string NationalId, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);
public record RegisterRequest(
    string UserName, string Email, string NationalId, string? PhoneNumber,
    int Gender, string Role,
    int? GradeId, int? ClassNum,
    int? PreferredMentorGender, int? PreferredMenteeGender,
    int? MentorSubjectId, int? MaxMentees,
    int? MenteeSubjectId);

// ── Users ─────────────────────────────────────────────────────────────────────
/// <summary>Per-supervisor summary counts shown on the admin dashboard's supervisor list.</summary>
public record SupervisorStatsResponse(int Id, string UserName, int PendingIssuesCount, int ResolvedIssuesCount, int PairsCount);

public record CreateUserRequest(
    string UserName,
    string Email,
    string NationalId,
    string? PhoneNumber,
    int Gender,
    string Role);

public record UpdateBaseInfoRequest(
    string UserName,
    string Email,
    string NationalId,
    string? PhoneNumber,
    int Gender);

public record UpdateLanguageRequest(string Language);
public record UpdateGradeClassRequest(int GradeId, int ClassNum);
public record UpdateGenderPreferencesRequest(int PreferredMentorGender, int PreferredMenteeGender);
public record UpdateMentorProfileRequest(int SubjectId);
public record UpdateMenteeProfileRequest(int SubjectId);
public record UpdateSupervisorClassesRequest(IEnumerable<int> ClassIds);

public record CreatePairRequest(int MentorId, int MenteeId);


/// <summary>Body a mentee sends to request pairing with a chosen mentor (Tier 1 direct request).</summary>
public record SendPairRequestBody(int MenteeId, int MentorId);

/// <summary>A pending pair request with display data, shown in the supervisor's approval queue.</summary>
public record PairRequestResponse(
    int Id,
    int MenteeId,
    int MentorId,
    string Status,
    int Tier,
    string CreatedAt,
    string MenteeName,
    string MentorName,
    string MenteeProfilePicturePath,
    int MenteeGender,
    string MenteeSubjectName);

/// <summary>Body sent when a supervisor accepts a pending pair request.</summary>
public record AcceptRequestBody();

/// <summary>One recommended mentor (with compatibility %) shown to a mentee in their selection gallery.</summary>
public record MatchRecommendationResponse(
    int Id,
    int MenteeId,
    int MentorId,
    double ScorePercent,
    string MentorName,
    string MentorProfilePicturePath,
    int MentorGender,
    string MentorSubjectName,
    string MenteeSubjectName);

/// <summary>Body a mentee sends when picking a mentor from the recommendation gallery (Tier 3).</summary>
public record GalleryPickRequest(int MenteeId, int MentorId);
/// <summary>Result of running the full matching pipeline (Tiers 1–5).</summary>
public record PipelineMatchResponse(int PairsCreated);

// ── Issues ────────────────────────────────────────────────────────────────────
public record CreateIssueRequest(string Description, int CategoryId);
/// <summary>Body a supervisor sends to escalate an issue to another supervisor/admin.</summary>
public record ForwardIssueRequest(int SupervisorId);

// ── Reviews ───────────────────────────────────────────────────────────────────
public record ReviewResponse(
    int Id,
    int PairId,
    int AuthorUserId,
    string Content,
    string Date,
    double AmountOfHours);

public record CreateReviewRequest(
    string Content,
    DateTime Date,
    int PairId,
    double AmountOfHours);

// ── Reference ─────────────────────────────────────────────────────────────────
public record AddSchoolClassRequest(int GradeId, int ClassNum);

// ── Settings ──────────────────────────────────────────────────────────────────
/// <summary>Global process state + deadlines that drive the admin overview stepper and phase gating.</summary>
public record SettingsResponse(
    string? Phase1Deadline,
    string? Phase2Deadline,
    bool IsPhase1Complete,
    bool IsProcessComplete,
    bool IsSchoolConfigured,
    bool IsSupervisorsAssigned,
    bool IsUsersImported,
    double MeetingHoursBarrier);

public record DeadlineRequest(DateTime? Deadline);
public record BoolSettingRequest(bool Value);

// ── Generic results ─────────────────────────────────────────────────────────────
/// <summary>Standard error payload returned with non-success HTTP responses.</summary>
public record ErrorBody(string? Error);

/// <summary>Server path of a file the client just uploaded (e.g. a profile picture).</summary>
public record UploadResult(string Path);
/// <summary>How many users were created by an Excel import.</summary>
public record ImportResult(int Imported);

public record SupervisorIdResponse(int SupervisorId);


