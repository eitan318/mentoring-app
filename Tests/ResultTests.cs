using FluentAssertions;
using MentoringApp.Service;
using Xunit;

namespace MentoringApp.Tests
{
    public class ResultTests
    {
        [Fact]
        public void Ok_SetsSuccessTrue_NoErrorMessage()
        {
            var result = Result.Ok();

            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Failure_SetsSuccessFalse_WithMessage()
        {
            var result = Result.Failure("Something went wrong");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Something went wrong");
        }

        [Fact]
        public void ValidationFailure_SetsSuccessFalse_PopulatesErrors()
        {
            var errors = new Dictionary<string, string>
            {
                { "Email", "Email is required" },
                { "Name", "Name is too short" }
            };

            var result = Result.ValidationFailure(errors);

            result.Success.Should().BeFalse();
            result.ValidationErrors.Should().BeEquivalentTo(errors);
            result.ErrorMessage.Should().Be("Validation failed.");
        }

        [Fact]
        public void GenericOk_ContainsData_AndSuccess()
        {
            var result = Result<string>.Ok("hello");

            result.Success.Should().BeTrue();
            result.Data.Should().Be("hello");
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void GenericFailure_HasNoData()
        {
            var result = Result<string>.Failure("error occurred");

            result.Success.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("error occurred");
        }

        [Fact]
        public void GenericValidationFailure_HasErrors_MessageIsValidationFailed()
        {
            var errors = new Dictionary<string, string>
            {
                { "Field", "Field is invalid" }
            };

            var result = Result<int>.ValidationFailure(errors);

            result.Success.Should().BeFalse();
            result.ValidationErrors.Should().BeEquivalentTo(errors);
            result.ErrorMessage.Should().Be("Validation failed.");
            result.Data.Should().Be(default(int));
        }
    }
}
