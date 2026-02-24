using MentoringApp.Model;
using MentoringApp.Data;
using MentoringApp.Data.Interfaces;
using System.Reflection.Emit;
using MentoringApp.Service.Validation;

namespace MentoringApp.Service
{
    public class AuthService
    {
        IUserRepo _userRepository;
        IVerificationCodeRepo _verificationCodeRepository;
        EmailService _emailService;
        UserValidator _userValidator;
        public AuthService(IUserRepo repo, IVerificationCodeRepo verificationCodeRepository, UserValidator userValidator, EmailService emailService)
        {
            _userValidator = userValidator;
            _verificationCodeRepository = verificationCodeRepository;
            _emailService = emailService;
            _userRepository = repo;
        }

        public async Task<Result> SendVerificationCodeAsync(string nationalId)
        {
            // Initial Checks
            var user = _userRepository.LoadUserByNationalId(nationalId);
            if (user == null) 
                return Result.Failure("User does not exist.");

            // Generate and Save Code
            string code = new Random().Next(100000, 999999).ToString();
            var verification = new VerificationCode { CreationDate = DateTime.Now, Code = code };
        
            bool saved = await _verificationCodeRepository.SaveAsync(user.Id, verification);
            if (!saved) 
                return Result.Failure("System error: Could not save verification code.");

            // Send Email
            string body = $"<h1>Your code is {code}</h1>";
            bool sent = await _emailService.SendEmailAsync(user.Email, "Verification code", body);
        
            return sent ? Result.Ok() : Result.Failure("Failed to send email. Please check your connection.");
        }

        public async Task<Result<User>> LoginAsync(string nationalId)
        { 
            if (string.IsNullOrWhiteSpace(nationalId))
                return Result<User>.Failure("National ID cannot be empty.");

            var user = await Task.Run(() => _userRepository.LoadUserByNationalId(nationalId));
            if (user == null)
                return Result<User>.Failure("No user found with this National ID.");

            return Result<User>.Ok(user);
        }


        public async Task<Result> VerificationCodeValid(string verificationCodeInput)
        {
            if (string.IsNullOrWhiteSpace(verificationCodeInput))
                return Result.Failure("Please enter the code.");

            var userId = await _verificationCodeRepository.GetUserIdByCodeAsync(verificationCodeInput);
            if (userId == null) 
                return Result.Failure("Invalid verification code.");

            var user = _userRepository.LoadUserById(userId.Value);
        
            // Check Expiry
            bool isExpired = (DateTime.Now - user.CurrentVerificationCode.CreationDate).TotalMinutes > 10;
            if (isExpired)
                return Result<bool>.Failure("Verification code has expired. Please request a new one.");

            // Clear code and update user
            user.CurrentVerificationCode = null; 
            bool updated = await _userRepository.UpdateAsync(user);
        
            return updated ? Result.Ok() : Result.Failure("Database error during verification.");
        }

        public async Task<Result<User>> Register(User user)
        {
            var validationResult = await _userValidator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.First().ErrorMessage
                    );
                return Result<User>.ValidationFailure(errors);
            }

            if (_userRepository.UserExists(user.NationalId))
                return Result<User>.Failure("User already exists.");

            bool created = _userRepository.CreateUser(user);
            return created ? Result<User>.Ok(user) : Result<User>.Failure("Failed to create user account.");
        }
    }
}
