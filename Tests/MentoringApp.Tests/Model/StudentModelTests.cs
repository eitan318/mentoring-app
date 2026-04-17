using FluentAssertions;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;
using Xunit;

namespace MentoringApp.Tests.Model
{
    public class StudentModelTests
    {
        private static Grade MakeGrade(int num) =>
            new Grade { Id = num, Name = $"Grade {num}", Num = num };

        private static StudentModel MakeStudent(int gradeNum = 9) =>
            new StudentModel(1, "test@test.com", "Test", "123456789", MakeGrade(gradeNum));

        [Fact]
        public void IsMentor_ReturnsFalse_WhenMentorProfileNull()
        {
            var student = MakeStudent();
            student.MentorProfile = null;

            student.IsMentor.Should().BeFalse();
        }

        [Fact]
        public void IsMentor_ReturnsTrue_WhenMentorProfileSet()
        {
            var student = MakeStudent();
            student.MentorProfile = new MentorProfile { SubjectToTeach = 1 };

            student.IsMentor.Should().BeTrue();
        }

        [Fact]
        public void IsMentee_ReturnsFalse_WhenMenteeProfileNull()
        {
            var student = MakeStudent();
            student.MenteeProfile = null;

            student.IsMentee.Should().BeFalse();
        }

        [Fact]
        public void IsMentee_ReturnsTrue_WhenMenteeProfileSet()
        {
            var student = MakeStudent();
            student.MenteeProfile = new MenteeProfile { SubjectToLearn = 2 };

            student.IsMentee.Should().BeTrue();
        }

        [Fact]
        public void CanHaveMentorProfile_ReturnsFalse_ForGradeBelow9()
        {
            var student = MakeStudent(gradeNum: 8);

            student.CanHaveMentorProfile().Should().BeFalse();
        }

        [Fact]
        public void CanHaveMentorProfile_ReturnsTrue_ForGrade9()
        {
            var student = MakeStudent(gradeNum: 9);

            student.CanHaveMentorProfile().Should().BeTrue();
        }

        [Fact]
        public void CanHaveMentorProfile_ReturnsTrue_ForGradeAbove9()
        {
            var student = MakeStudent(gradeNum: 12);

            student.CanHaveMentorProfile().Should().BeTrue();
        }

        [Fact]
        public void DefaultPreferredMentorGender_IsNoPreference()
        {
            var student = MakeStudent();

            student.PreferredMentorGender.Should().Be(GenderPreference.NoPreference);
        }

        [Fact]
        public void DefaultPreferredMenteeGender_IsNoPreference()
        {
            var student = MakeStudent();

            student.PreferredMenteeGender.Should().Be(GenderPreference.NoPreference);
        }
    }
}
