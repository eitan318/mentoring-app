using FluentAssertions;
using MentoringApp.Model;
using MentoringApp.Model.User;
using Xunit;

namespace MentoringApp.Tests
{
    public class SupervisorModelTests
    {
        private static SupervisorModel MakeSupervisor() =>
            new SupervisorModel(1, "s@test.com", "Sup", "123456789");

        private static IssueCategoryModel MakeCategory() =>
            new IssueCategoryModel("General");

        private static IssueModel MakeIssue(bool isResolved) =>
            new IssueModel("Test issue", MakeCategory(), isResolved, reportedByUserId: 1);

        private static Grade MakeGrade(int num) =>
            new Grade { Id = num, Name = $"Grade {num}", Num = num };

        private static SchoolClass MakeSchoolClass(int gradeNum, int classNum) =>
            new SchoolClass { Grade = MakeGrade(gradeNum), ClassNum = classNum };

        [Fact]
        public void PendingCount_ReturnsZero_WhenIssuesNull()
        {
            var supervisor = MakeSupervisor();
            supervisor.Issues = null;

            supervisor.PendingCount.Should().Be(0);
        }

        [Fact]
        public void PendingCount_CountsOnlyUnresolved()
        {
            var supervisor = MakeSupervisor();
            supervisor.Issues = new[]
            {
                MakeIssue(isResolved: false),
                MakeIssue(isResolved: false),
                MakeIssue(isResolved: true)
            };

            supervisor.PendingCount.Should().Be(2);
        }

        [Fact]
        public void ResolvedCount_CountsOnlyResolved()
        {
            var supervisor = MakeSupervisor();
            supervisor.Issues = new[]
            {
                MakeIssue(isResolved: false),
                MakeIssue(isResolved: true),
                MakeIssue(isResolved: true)
            };

            supervisor.ResolvedCount.Should().Be(2);
        }

        [Fact]
        public void PendingIssues_ReturnsEmpty_WhenIssuesNull()
        {
            var supervisor = MakeSupervisor();
            supervisor.Issues = null;

            supervisor.PendingIssues.Should().BeEmpty();
        }

        [Fact]
        public void PendingIssues_FiltersToUnresolved()
        {
            var pending1 = MakeIssue(isResolved: false);
            var pending2 = MakeIssue(isResolved: false);
            var resolved = MakeIssue(isResolved: true);

            var supervisor = MakeSupervisor();
            supervisor.Issues = new[] { pending1, pending2, resolved };

            supervisor.PendingIssues.Should().BeEquivalentTo(new[] { pending1, pending2 });
        }

        [Fact]
        public void ResolvedIssues_FiltersToResolved()
        {
            var pending = MakeIssue(isResolved: false);
            var resolved1 = MakeIssue(isResolved: true);
            var resolved2 = MakeIssue(isResolved: true);

            var supervisor = MakeSupervisor();
            supervisor.Issues = new[] { pending, resolved1, resolved2 };

            supervisor.ResolvedIssues.Should().BeEquivalentTo(new[] { resolved1, resolved2 });
        }

        [Fact]
        public void Problematicness_ReturnsPendingCount()
        {
            var supervisor = MakeSupervisor();
            supervisor.Issues = new[]
            {
                MakeIssue(isResolved: false),
                MakeIssue(isResolved: false),
                MakeIssue(isResolved: true)
            };

            supervisor.Problematicness().Should().Be(supervisor.PendingCount);
        }

        [Fact]
        public void Grade_ReturnsNull_WhenNoAssignedClasses()
        {
            var supervisor = MakeSupervisor();

            supervisor.Grade.Should().BeNull();
        }

        [Fact]
        public void Grade_ReturnsFirstAssignedClassGrade()
        {
            var supervisor = MakeSupervisor();
            var grade = MakeGrade(10);
            supervisor.AssignedClasses.Add(new SchoolClass { Grade = grade, ClassNum = 2 });
            supervisor.AssignedClasses.Add(new SchoolClass { Grade = MakeGrade(11), ClassNum = 3 });

            supervisor.Grade.Should().Be(grade);
        }

        [Fact]
        public void ClassNum_ReturnsZero_WhenNoAssignedClasses()
        {
            var supervisor = MakeSupervisor();

            supervisor.ClassNum.Should().Be(0);
        }

        [Fact]
        public void ClassNum_ReturnsFirstAssignedClassNum()
        {
            var supervisor = MakeSupervisor();
            supervisor.AssignedClasses.Add(MakeSchoolClass(9, 5));
            supervisor.AssignedClasses.Add(MakeSchoolClass(10, 3));

            supervisor.ClassNum.Should().Be(5);
        }
    }
}
