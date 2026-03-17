using MentoringApp.Data.DTO;
using MentoringApp.Model;

namespace MentoringApp.Data.Interfaces
{
    public interface IDbRepo
    {
        void Recreate();
    }

    public interface IUserRepo
    {
        Task<bool> CreateUserAsync(User user);
        Task<IEnumerable<UserDto>> GetAllUserDtosAsync();
        Task<UserDto?> GetUserDtoByNationalIdAsync(string nationalId);
        Task<UserDto?> GetUserDtoByIdAsync(int userId);

        Task<bool> DeleteUserAsync(int userId);

        Task<bool> UpdateBaseInfoAsync(int id, string name, string email, string nationalId);

        Task UpdateStudentGradeAsync(int userId, int gradeId);

        Task UpsertMentorProfileAsync(int userId, int subjectId);
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
        bool Delete(int pairId);
    }

    public interface IIssueRepo
    {
        IEnumerable<IssueDto> GetAll();
        IssueDto? GetById(int id);
        IEnumerable<IssueDto> GetByReporter(int userId);
        IEnumerable<IssueDto> GetBySupervisor(int supervisorId);
        IEnumerable<IssueCategoryDto> GetCategories();
        IssueCategoryDto? GetCategoryById(int categoryId);
        bool Create(string description, int categoryId, int reportedByUserId);
        bool Resolve(int issueId);
    }

    public interface IReviewRepo
    {
        IEnumerable<ReviewDto> GetByPair(int pairId);
        IEnumerable<ReviewDto> GetByAuthor(int authorUserId);
        bool Create(string content, DateTime date, int pairId, int authorUserId);
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

