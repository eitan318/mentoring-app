using FluentValidation;
using MentoringApp.Model;
using MentoringApp.Model.User;

namespace MentoringApp.Service.Validation
{
    public class UserValidator : AbstractValidator<UserModel>
    {
        public UserValidator()
        {
            // --- Common Rules for all Users ---
            RuleFor(user => user.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.");

            RuleFor(user => user.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(user => user.NationalId)
                .NotEmpty()
                .Length(9).WithMessage("National ID must be exactly 9 digits.")
                .Matches("^[0-9]*$").WithMessage("National ID must contain only numbers.");

            RuleFor(user => (user as StudentModel).Grade.Num)
                .InclusiveBetween(1, 12).WithMessage("Grade must be between 1 and 12.")
                .When(user => user is StudentModel);

            RuleFor(user => (user as StudentModel).MentorProfile.SubjectToTeach)
                .NotEmpty().WithMessage("Please select a subject to teach.")
                .When(user => user is StudentModel s && s.IsMentor);
        }
    }
}
