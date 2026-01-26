using MentoringApp.Model;
using MentoringApp.Data;
using MentoringApp.Data.Interfaces;

namespace MentoringApp.Service
{
    public class AuthService
    {
        IUserRepository _userRepository;
        public AuthService(IUserRepository repo)
        {
            _userRepository = repo;
        }

        public User? Login(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
                return null;

            return _userRepository.LoadUserByNationalId(nationalId);
        }

        public bool Register(User user)
        {
            if (_userRepository.UserExists(user.NationalId))
                return false;

            return _userRepository.CreateUser(user);
        }
    }
}
