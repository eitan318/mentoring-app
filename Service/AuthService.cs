using MentoringApp.Model;
using MentoringApp.Data;
using MentoringApp.Data.Interfaces;
using System.Reflection.Emit;

namespace MentoringApp.Service
{
    public class AuthService
    {
        IUserRepo _userRepository;
        IVerificationCodeRepo _verificationCodeRepository;
        EmailService _emailService;
        public AuthService(IUserRepo repo, IVerificationCodeRepo verificationCodeRepository, EmailService emailService)
        {
            _verificationCodeRepository = verificationCodeRepository;
            _emailService = emailService;
            _userRepository = repo;
        }

        public User? Login(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
                return null;

            if (!_userRepository.UserExists(nationalId))
                return null;

            return _userRepository.LoadUserByNationalId(nationalId);
        }

        public bool Register(User user)
        {
            if (_userRepository.UserExists(user.NationalId))
                return false;

            return _userRepository.CreateUser(user);
        }

        public async Task<bool> SendVerificationCodeAsync(string nationalId) { 
            if (string.IsNullOrWhiteSpace(nationalId))
                return false;

            if (!_userRepository.UserExists(nationalId))
                return false;

            User? user = _userRepository.LoadUserByNationalId(nationalId);
            if (user == null) return false;

            string code = new Random().Next(100000, 999999).ToString();
            user.CurrentVerificationCode = new VerificationCode { CreationDate = DateTime.Now, Code = code};
            bool saved = await _verificationCodeRepository.SaveAsync(user.Id, user.CurrentVerificationCode);
            if (!saved) return false;

            string body = $"<h1>Your code is {code}</h1>";
            return await _emailService.SendEmailAsync(user.Email, "Verification code", body);

        }

        public async Task<bool> VerificationCodeValid(string verificationCodeInput)
        {
            var userId = await _verificationCodeRepository.GetUserIdByCodeAsync(verificationCodeInput);
            if (userId == null) return false;

            var user = _userRepository.LoadUserById(userId.Value);

            bool isExpired = (DateTime.Now - user.CurrentVerificationCode.CreationDate).TotalMinutes > 10;

            if (!isExpired)
            {
                user.CurrentVerificationCode = null; 
                return await _userRepository.UpdateAsync(user);
            }
            return false;
        }
    }
}
