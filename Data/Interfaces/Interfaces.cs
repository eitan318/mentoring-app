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
        Task<bool> SaveAsync(int userId, VerificationCode verificationCode);
        Task<int?> GetUserIdByCodeAsync(string code);
        Task<bool> DeleteAsync(int userId);
    }

    public interface IPairRepo
    {
        Task<IEnumerable<Pair>> GetAllAsync();
        Pair? GetById(int id);
        Task<Pair?> GetByMentorIdAsync(int mentorId);
        Task<Pair?> GetByMenteeIdAsync(int menteeId);
        IEnumerable<Pair> GetBySupervisorId(int supervisorId);
        Task<bool> CreateAsync(Pair pair, int supervisorId, int mentorId, int menteeId);
        bool Delete(int pairId);
    }

    public interface IIssueRepo
    {
        IEnumerable<Issue> GetAll();
        Issue? GetById(int id);
        IEnumerable<Issue> GetByReporter(int userId);
        IEnumerable<Issue> GetBySupervisor(int supervisorId);
        IEnumerable<IssueCategory> GetCategories();
        bool Create(Issue issue, int reportedByUserId);
        bool Resolve(int issueId);
    }

    public interface IReviewRepo
    {
        IEnumerable<Review> GetByPair(int pairId);
        IEnumerable<Review> GetByAuthor(int authorUserId);
        bool Create(Review review, int pairId, int authorUserId);
    }

    public interface ISubjectRepo
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
    }

    public interface IGradeRepo
    {
        Task<GradeDto?> GetByIdAsync(int id);

        Task<IEnumerable<GradeDto>> GetAllGradesAsync();

    }
}

