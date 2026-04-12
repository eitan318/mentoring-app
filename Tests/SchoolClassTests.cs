using FluentAssertions;
using MentoringApp.Model;
using Xunit;

namespace MentoringApp.Tests
{
    public class SchoolClassTests
    {
        private static Grade MakeGrade(int num, string name) =>
            new Grade { Id = num, Name = name, Num = num };

        [Fact]
        public void DisplayName_FormatsGradeNameAndClassNum()
        {
            var grade = MakeGrade(10, "Grade 10");
            var schoolClass = new SchoolClass { Grade = grade, ClassNum = 3 };

            schoolClass.DisplayName.Should().Be("Grade 10 \u2013 Class 3");
        }

        [Fact]
        public void DisplayName_UsesEmDash()
        {
            var grade = MakeGrade(9, "Grade 9");
            var schoolClass = new SchoolClass { Grade = grade, ClassNum = 1 };

            schoolClass.DisplayName.Should().Contain("\u2013");
            schoolClass.DisplayName.Should().NotContain("-");
        }
    }
}
