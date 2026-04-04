using MentoringApp.Data.DTO;
using MentoringApp.Model;
using MentoringApp.Model.User;

namespace MentoringApp.Data.Interfaces
{
    public interface IDbRepo
    {
        void Recreate();
    }

    public interface IUserRepo
    {
        Task<bool> CreateUserAsync(UserModel user);
        Task<IEnumerable<UserDto>> GetAllUserDtosAsync();
        Task<UserDto?> GetUserDtoByNationalIdAsync(string nationalId);
        Task<UserDto?> GetUserDtoByIdAsync(int userId);
        Task<IEnumerable<SupervisorStatsDto>> GetSupervisorStatisticsAsync();
        

        Task<bool> DeleteUserAsync(int userId);

        Task<bool> UpdateBaseInfoAsync(int id, string name, string email, string nationalId);

        Task UpdateStudentGradeAsync(int userId, int gradeId);

        Task UpsertMentorProfileAsync(int userId, int subjectId);

        Task<bool> UpdateProfilePictureAsync(int userId, string? path);
        Task<bool> UpdateLanguageAsync(int userId, string language);
    }

    public interface IVerificationCodeRepo
    {
        Task<bool> SaveAsync(int userId, string code, DateTime creationDate);
        Task<int?> GetUserIdByCodeAsync(string code);
        Task<bool> DeleteAsync(int userId);
    }

    public interface IPairRepo
    {
        Task<IEnumerable<PairDto>> GetAllAsync();
        Task<PairDto?> GetByIdAsync(int id);
        Task<PairDto?> GetByMentorIdAsync(int mentorId);
        Task<PairDto?> GetByMenteeIdAsync(int menteeId);
        Task<IEnumerable<PairDto>> GetBySupervisorIdAsync(int supervisorId);

        /// <summary>Admin/legacy create – no tier tracking.</summary>
        Task<bool> CreateAsync(int supervisorId, int mentorId, int menteeId);

        /// <summary>Tier-aware create used by the matching flow.</summary>
        Task<bool> CreateWithTierAsync(int supervisorId, int mentorId, int menteeId, int matchTier, bool isProfileIncomplete);

        Task<bool> DeleteAsync(int pairId);

        /// <summary>Returns IDs of mentors that already have a pair.</summary>
        Task<IEnumerable<int>> GetMatchedMentorIdsAsync();

        /// <summary>Returns IDs of mentees that already have a pair.</summary>
        Task<IEnumerable<int>> GetMatchedMenteeIdsAsync();

        /// <summary>Returns all pairs flagged as profile-incomplete (Tier 5).</summary>
        Task<IEnumerable<PairDto>> GetProfileIncompleteAsync();
    }

    public interface IIssueRepo
    {
        Task<IEnumerable<IssueDto>> GetAllAsync();
        Task<IssueDto?> GetByIdAsync(int id);
        Task<IEnumerable<IssueDto>> GetByReporterAsync(int userId);
        Task<IEnumerable<IssueDto>> GetBySupervisorAsync(int supervisorId);
        Task<bool> CreateAsync(string description, int categoryId, int reportedByUserId);
        Task<bool> ResolveAsync(int issueId);
    }

    public interface IIssueCategoryRepo
    {
        Task<IEnumerable<IssueCategoryDto>> GetAllAsync();
        Task<IssueCategoryDto?> GetByIdAsync(int categoryId);
    }

    public interface IReviewRepo
    {
        Task<IEnumerable<ReviewDto>> GetByPairAsync(int pairId);
        Task<IEnumerable<ReviewDto>> GetByAuthorAsync(int authorUserId);
        Task<bool> CreateAsync(string content, DateTime date, int pairId, int authorUserId, double amountOfHours);
    }

    public interface ISettingsRepo
    {
        Task<double> GetDoubleAsync(string key, double defaultValue = 0);
        Task SetDoubleAsync(string key, double value);
        Task<string> GetStringAsync(string key, string defaultValue = "");
        Task SetStringAsync(string key, string value);
    }

    public interface ISubjectRepo
    {
        Task<IEnumerable<SubjectDto>> GetAllSubjectsAsync();
    }

    public interface IGradeRepo
    {
        Task<GradeDto?> GetByIdAsync(int id);

        Task<IEnumerable<GradeDto>> GetAllGradesAsync();

    }

    // ── NEW: pair request repo ────────────────────────────────────────────────
    public interface IPairRequestRepo
    {
        Task<bool> CreateAsync(int menteeId, int mentorId, int tier);
        Task<IEnumerable<PairRequestDto>> GetByMentorAsync(int mentorId);
        Task<IEnumerable<PairRequestDto>> GetByMenteeAsync(int menteeId);
        Task<bool> UpdateStatusAsync(int requestId, string status);
        /// <summary>Cancels any pending requests that involve either user (after a pair is formed).</summary>
        Task CancelPendingForUsersAsync(int menteeId, int mentorId);
        Task<bool> ExistsAsync(int menteeId, int mentorId);
    }

    // ── NEW: match score repo ─────────────────────────────────────────────────
    public interface IMatchScoreRepo
    {
        Task BulkInsertAsync(IEnumerable<MatchScoreDto> scores);
        Task<IEnumerable<MatchScoreDto>> GetTopForMenteeAsync(int menteeId, int limit = 3);
        Task<IEnumerable<MatchScoreDto>> GetAllAsync();
        Task ClearAllAsync();
    }
}


