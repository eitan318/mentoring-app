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
        Task<bool> CreateAsync(int supervisorId, int mentorId, int menteeId);
        Task<bool> DeleteAsync(int pairId);
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
}

