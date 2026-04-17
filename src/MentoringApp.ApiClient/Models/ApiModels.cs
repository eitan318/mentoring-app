namespace MentoringApp.ApiClient.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────
public record SendCodeRequest(string NationalId);
public record LoginRequest(string NationalId, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);

// ── Users ─────────────────────────────────────────────────────────────────────
public record UserResponse(
    int Id,
    string UserName,
    string Email,
    string NationalId,
    string? ProfilePicturePath,
    string Language,
    string? PhoneNumber,
    int Gender,
    string Role,
    int? GradeId,
    int? ClassNum,
    int? PreferredMentorGender,
    int? PreferredMenteeGender,
    int? MentorSubjectId,
    int? MaxMentees,
    int? MenteeSubjectId);

public record SupervisorStatsResponse(int Id, string UserName, int PendingIssuesCount, int PairsCount);

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

// ── Pairs ─────────────────────────────────────────────────────────────────────
public record PairResponse(
    int Id,
    int MentorId,
    int MenteeId,
    int SupervisorId,
    string CreatedAt,
    int MatchTier,
    bool IsProfileIncomplete);

public record CreatePairRequest(int SupervisorId, int MentorId, int MenteeId);

// ── Matching ──────────────────────────────────────────────────────────────────
public record AvailableMentorResponse(
    int Id,
    string UserName,
    string Email,
    int Gender,
    int? SubjectToTeach,
    int? MaxMentees,
    string? ProfilePicturePath);

public record AvailableMenteeResponse(
    int Id,
    string UserName,
    string Email,
    int Gender,
    int? SubjectToLearn,
    string? ProfilePicturePath);

public record SendPairRequestBody(int MenteeId, int MentorId);

public record PairRequestResponse(
    int Id,
    int MenteeId,
    int MentorId,
    string Status,
    int Tier,
    string CreatedAt);

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
public record PipelineMatchResponse(int PairsCreated);

// ── Issues ────────────────────────────────────────────────────────────────────
public record IssueResponse(
    int Id,
    string Description,
    int CategoryId,
    int ReportedByUserId,
    int IsResolved,
    string CreationDate,
    int? ForwardedBySupervisorId);

public record IssueCategoryResponse(int Id, string Name);
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
public record SubjectResponse(int Id, string Name);
public record GradeResponse(int Id, string Name, int Num);
public record SchoolClassResponse(int Id, int GradeId, int ClassNum);
