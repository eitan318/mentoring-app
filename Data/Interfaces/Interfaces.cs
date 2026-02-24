using MentoringApp.Model;

namespace MentoringApp.Data.Interfaces
{
    public interface IDbRepo
    {
        void Recreate();
    }

    public interface IUserRepo
    {
        Task<User?> LoadUserByNationalIdAsync(string nationalId);
        Task<User?> LoadUserByIdAsync(int userId);
        bool UserExists(string nationalId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        bool CreateUser(User user);
        Task<bool> UpdateAsync(User user);

        bool DeleteUser(int userId);
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
        Task<Grade?> GetByIdAsync(int id);
        Task<IEnumerable<Grade>> GetAllGradesAsync();
    }
}

