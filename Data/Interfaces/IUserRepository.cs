using MentoringApp.Model;

namespace MentoringApp.Data.Interfaces
{
    public interface IUserRepository
    {
        bool CreateUser(User user);
        User? LoadUserByNationalId(string nationalId);
        bool UserExists(string nationalId);
        List<User> GetAllUsers();
    }

}
