using MentoringApp.Model;
using MentoringApp.Data;
using MentoringApp.Data.Interfaces;
using System.Reflection.Emit;
using MentoringApp.Service.Validation;

namespace MentoringApp.Service
{
    public class AuthService
    {
        private readonly UserService _userService; // Changed from IUserRepo
        private readonly IVerificationCodeRepo _verificationCodeRepository;
        private readonly EmailService _emailService;
        private readonly UserValidator _userValidator;

        public AuthService(
            UserService userService,
            IVerificationCodeRepo verificationCodeRepository,
            UserValidator userValidator,
            EmailService emailService)
        {
            _userService = userService;
            _verificationCodeRepository = verificationCodeRepository;
            _emailService = emailService;
            _userValidator = userValidator;
        }

        public async Task<Result> SendVerificationCodeAsync(string nationalId)
        {
            var userResult = await _userService.GetUserByNationalIdAsync(nationalId);

            if (!userResult.Success)
                return Result.Failure("User does not exist.");

            var user = userResult.Data;

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

            var userResult = await _userService.GetUserByNationalIdAsync(nationalId);

            if (!userResult.Success)
                return Result<User>.Failure("User does not exist.");

            var user = userResult.Data;

            return Result<User>.Ok(user);
        }


        public async Task<Result> VerificationCodeValid(string verificationCodeInput)
        {
            if (string.IsNullOrWhiteSpace(verificationCodeInput))
                return Result.Failure("Please enter the code.");

            var userId = await _verificationCodeRepository.GetUserIdByCodeAsync(verificationCodeInput);
            if (userId == null)
                return Result.Failure("Invalid verification code.");

            var userResult = await _userService.GetUserByIdAsync(userId.Value);
            if (!userResult.Success) return Result.Failure("User not found.");

            var user = userResult.Data;

            bool isExpired = (DateTime.Now - user.CurrentVerificationCode.CreationDate).TotalMinutes > 10;
            if (isExpired) return Result.Failure("Code expired.");

            bool cleared = await _verificationCodeRepository.DeleteAsync(userId.Value);

            return cleared ? Result.Ok() : Result.Failure("Database error.");
        }

        public async Task<Result<User>> Register(User user)
        {
            // 1. Validation (Stay the same - keep this in AuthService)
            var validationResult = await _userValidator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.First().ErrorMessage);
                return Result<User>.ValidationFailure(errors);
            }

            // 2. Business Check (Use the UserService now)
            var existingUser = await _userService.GetUserByNationalIdAsync(user.NationalId);
            if (existingUser.Success)
            {
                return Result<User>.Failure("User already exists.");
            }

            // 3. Delegation (Let UserService handle the complex multi-table SQL)
            var createdResult = await _userService.CreateUserAsync(user);

            return createdResult.Success
                ? Result<User>.Ok(user)
                : Result<User>.Failure("Failed to create user account.");
        }
    }
}
