namespace MentoringApp.Model;

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

public record CreatePairRequest(int SupervisorId, int MentorId, int MenteeId);





public record SendPairRequestBody(int MenteeId, int MentorId);

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
public record AcceptRequestBody(int SupervisorId);

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

public record GalleryPickRequest(int MenteeId, int MentorId, int SupervisorId);
/// <summary>Result of running the full matching pipeline (Tiers 1–5).</summary>
public record PipelineMatchResponse(int PairsCreated);



public record CreateIssueRequest(string Description, int CategoryId, int ReportedByUserId);
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
    int AuthorUserId,
    double AmountOfHours);

// ── Reference ─────────────────────────────────────────────────────────────────
public record AddSchoolClassRequest(int GradeId, int ClassNum);

// ── Settings ──────────────────────────────────────────────────────────────────
public record SettingsResponse(
    string? Phase1Deadline,
    string? Phase2Deadline,
    bool IsPhase1Complete,
    bool IsProcessComplete,
    bool IsSchoolConfigured,
    bool IsUsersImported,
    double MeetingHoursBarrier);

public record DeadlineRequest(DateTime? Deadline);
public record BoolSettingRequest(bool Value);
public record DeadlineBody(DateTime? Deadline);
public record BoolBody(bool Value);
public record SendRequestBody(int MenteeId, int MentorId);
public record GalleryPickBody(int MenteeId, int MentorId, int SupervisorId);

public record ErrorBody(string? Error);

public record UploadResult(string Path);
public record ImportResult(int Imported);


public record SupervisorIdResponse(int SupervisorId);


public record AddSchoolClassBody(int GradeId, int ClassNum);