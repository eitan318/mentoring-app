using MentoringApp.Model;

namespace MentoringApp.Data.Interfaces
{
    public interface IDbRepo
    {
        void Recreate();
    }

    public interface IUserRepo
    {
        User? LoadUserByNationalId(string nationalId);
        User? LoadUserById(int userId);
        bool UserExists(string nationalId);
        List<User> GetAllUsers();
        bool CreateUser(User user);
        Task<bool> UpdateAsync(User user);
    }

    public interface IVerificationCodeRepo
    {
        Task<bool> SaveAsync(int userId, VerificationCode verificationCode);
        Task<int?> GetUserIdByCodeAsync(string code);
        Task<bool> DeleteAsync(int userId);
    }
}
