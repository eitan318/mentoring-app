using System;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class UserService
    {
        private readonly IUserRepo _userRepository;

        public UserService(IUserRepo userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<Result> DeleteUserAsync(int userId)
        {
            var user = _userRepository.LoadUserByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found.");

            bool deleted = await Task.Run(() => _userRepository.DeleteUser(userId));

            return deleted
                ? Result.Ok()
                : Result.Failure("Failed to delete the user from the database.");
        }
            
        public async Task<Result<User>> GetUserByIdAsync(int userId)
        {
            var user = await Task.Run(() => _userRepository.LoadUserByIdAsync(userId));

            return user != null
                ? Result<User>.Ok(user)
                : Result<User>.Failure("User not found.");
        }

        public async Task<Result> UpdateUserAsync(User user)
        {
            if (user == null) return Result.Failure("User data is null.");

            bool updated = await _userRepository.UpdateAsync(user);
            return updated ? Result.Ok() : Result.Failure("Update failed.");
        }
    }
}

