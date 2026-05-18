using MentoringApp.Data.Dao;
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
        Task<IEnumerable<UserDao>> GetAllUserDtosAsync();
        Task<UserDao?> GetUserDtoByNationalIdAsync(string nationalId);
        Task<UserDao?> GetUserDtoByIdAsync(int userId);
        Task<IEnumerable<SupervisorStatsDao>> GetSupervisorStatisticsAsync();
        

        Task<bool> DeleteUserAsync(int userId);

        Task<bool> UpdateBaseInfoAsync(int id, string name, string email, string nationalId, string? phoneNumber, int gender);

        Task UpdateStudentGradeAndClassAsync(int userId, int gradeId, int classNum);
        Task UpdateStudentPreferredGendersAsync(int userId, int preferredMentorGender, int preferredMenteeGender);
        Task UpdateSupervisorClassesAsync(int supervisorId, IEnumerable<int> schoolClassIds);

        Task UpsertMentorProfileAsync(int userId, int subjectId);
        Task UpsertMenteeProfileAsync(int userId, int subjectId);

        Task<bool> UpdateProfilePictureAsync(int userId, string? path);
        Task<bool> UpdateLanguageAsync(int userId, string language);
    }

    public interface IVerificationCodeRepo
    {
        Task<bool> SaveAsync(int userId, string code, DateTime creationDate);
        Task<int?> GetUserIdByCodeAsync(string code);
        Task<string?> GetCodeByUserIdAsync(int userId);
        Task<bool> DeleteAsync(int userId);
    }

    public interface IPairRepo
    {
        Task<IEnumerable<PairDao>> GetAllAsync();
        Task<PairDao?> GetByIdAsync(int id);
        Task<PairDao?> GetByMentorIdAsync(int mentorId);
        Task<PairDao?> GetByMenteeIdAsync(int menteeId);
        Task<IEnumerable<PairDao>> GetBySupervisorIdAsync(int supervisorId);

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
        Task<IEnumerable<PairDao>> GetProfileIncompleteAsync();

        /// <summary>Deletes all pairs and their associated reviews and pair requests.</summary>
        Task DeleteAllAsync();
    }

    public interface IIssueRepo
    {
        Task<IEnumerable<IssueDao>> GetAllAsync();
        Task<IssueDao?> GetByIdAsync(int id);
        Task<IEnumerable<IssueDao>> GetByReporterAsync(int userId);
        Task<IEnumerable<IssueDao>> GetBySupervisorAsync(int supervisorId);
        Task<bool> CreateAsync(string description, int categoryId, int reportedByUserId);
        Task<bool> ResolveAsync(int issueId);
        Task<bool> ForwardAsync(int issueId, int supervisorId);
        Task<IEnumerable<IssueDao>> GetForwardedAsync();
    }

    public interface IIssueCategoryRepo
    {
        Task<IEnumerable<IssueCategoryDao>> GetAllAsync();
        Task<IssueCategoryDao?> GetByIdAsync(int categoryId);
    }

    public interface IReviewRepo
    {
        Task<IEnumerable<ReviewDao>> GetByPairAsync(int pairId);
        Task<IEnumerable<ReviewDao>> GetByAuthorAsync(int authorUserId);
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
        Task<IEnumerable<SubjectDao>> GetAllSubjectsAsync();
    }

    public interface IGradeRepo
    {
        Task<GradeDao?> GetByIdAsync(int id);

        Task<IEnumerable<GradeDao>> GetAllGradesAsync();

    }

    public interface IYearAdvanceRepo
    {
        /// <summary>
        /// Returns user IDs of all students enrolled in the grade with the highest Num value.
        /// These are the graduating students who will be removed at year-end.
        /// </summary>
        Task<IEnumerable<int>> GetGraduatingStudentIdsAsync();

        /// <summary>
        /// For every student, moves their GradeId to the next grade (ordered by Num).
        /// Students already in the highest grade are unaffected (they should be deleted first).
        /// </summary>
        Task AdvanceStudentGradesAsync();
    }

    // ── NEW: pair request repo ────────────────────────────────────────────────
    public interface IPairRequestRepo
    {
        Task<bool> CreateAsync(int menteeId, int mentorId, int tier);
        Task<IEnumerable<PairRequestDao>> GetByMentorAsync(int mentorId);
        Task<IEnumerable<PairRequestDao>> GetByMenteeAsync(int menteeId);
        Task<bool> UpdateStatusAsync(int requestId, string status);
        /// <summary>Cancels any pending requests that involve either user (after a pair is formed).</summary>
        Task CancelPendingForUsersAsync(int menteeId, int mentorId);
        Task<bool> ExistsAsync(int menteeId, int mentorId);
    }

    // ── NEW: match score repo ─────────────────────────────────────────────────
    public interface IMatchScoreRepo
    {
        Task BulkInsertAsync(IEnumerable<MatchScoreDao> scores);
        Task<IEnumerable<MatchScoreDao>> GetTopForMenteeAsync(int menteeId, int limit = 3);
        Task<IEnumerable<MatchScoreDao>> GetAllAsync();
        Task ClearAllAsync();
    }
    // ── School Class repo ─────────────────────────────────────────────────
    public interface ISchoolClassRepo
    {
        Task<IEnumerable<SchoolClassDao>> GetAllAsync();
        Task<IEnumerable<SchoolClassDao>> GetBySupervisorAsync(int supervisorId);
        Task<bool> AddAsync(int gradeId, int classNum);
        Task<bool> DeleteAsync(int schoolClassId);
        /// <summary>Replaces all class assignments for a supervisor atomically.</summary>
        Task SetSupervisorClassesAsync(int supervisorId, IEnumerable<int> schoolClassIds);
        /// <summary>Returns the ID of the supervisor responsible for the student's class, or null if not found.</summary>
        Task<int?> GetSupervisorIdForStudentAsync(int studentId);
    }
}
