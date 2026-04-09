using MentoringApp.Model;
using MentoringApp.Data;
using MentoringApp.Data.Interfaces;
using System.Reflection.Emit;
using MentoringApp.Service.Validation;
using MentoringApp.Model.User;

namespace MentoringApp.Service
{
    /// <summary>
    /// Handles the two-step email verification login flow and user registration.
    /// Login: caller first calls <see cref="SendVerificationCodeAsync"/> (sends a 6-digit code
    /// to the user's email), then calls <see cref="VerificationCodeValid"/> to consume the code,
    /// then calls <see cref="LoginAsync"/> to obtain the <see cref="UserModel"/>.
    /// </summary>
    public class AuthService
    {
        private readonly UserService _userService;
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
            var creationDate = DateTime.Now;
        
            bool saved = await _verificationCodeRepository.SaveAsync(user.Id, code, creationDate);
            if (!saved) 
                return Result.Failure("System error: Could not save verification code.");

            // Send Email
            string body = $"<h1>Your code is {code}</h1>";
            bool sent = await _emailService.SendEmailAsync(user.Email, "Verification code", body);
        
            return sent ? Result.Ok() : Result.Failure("Failed to send email. Please check your connection.");
        }

        public async Task<Result<UserModel>> LoginAsync(string nationalId)
        { 
            if (string.IsNullOrWhiteSpace(nationalId))
                return Result<UserModel>.Failure("National ID cannot be empty.");

            var userResult = await _userService.GetUserByNationalIdAsync(nationalId);

            if (!userResult.Success)
                return Result<UserModel>.Failure("User does not exist.");

            var user = userResult.Data;

            return Result<UserModel>.Ok(user);
        }


        /// <summary>
        /// Validates a verification code and, if valid, deletes it so it cannot be reused.
        /// Codes expire after 10 minutes.
        /// </summary>
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

            // Code valid for 10 minutes from creation
            bool isExpired = (DateTime.Now - user.CurrentVerificationCode.CreationDate).TotalMinutes > 10;
            if (isExpired) return Result.Failure("Code expired.");

            // Consume the code so it cannot be reused
            bool cleared = await _verificationCodeRepository.DeleteAsync(userId.Value);

            return cleared ? Result.Ok() : Result.Failure("Database error.");
        }

        public async Task<Result<UserModel>> Register(UserModel user)
        {
            var validationResult = await _userValidator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.First().ErrorMessage);
                return Result<UserModel>.ValidationFailure(errors);
            }

            var existingUser = await _userService.GetUserByNationalIdAsync(user.NationalId);
            if (existingUser.Success)
            {
                return Result<UserModel>.Failure("User already exists.");
            }

            var createdResult = await _userService.CreateUserAsync(user);

            return createdResult.Success
                ? Result<UserModel>.Ok(user)
                : Result<UserModel>.Failure("Failed to create user account.");
        }
    }
}
