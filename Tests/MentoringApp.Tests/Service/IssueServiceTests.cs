using FluentAssertions;
using MentoringApp.Data.Dao;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using Moq;
using Xunit;

namespace MentoringApp.Tests.Service
{
    public class IssueServiceTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an IssueService with a no-op NotificationService (fire-and-forget calls
        /// are swallowed by NotificationService's own try/catch, so a dead SMTP config is safe).
        /// </summary>
        private static IssueService BuildService(
            Mock<IIssueRepo> issueRepo,
            Mock<IIssueCategoryRepo> categoryRepo)
        {
            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetAllUserDtosAsync()).ReturnsAsync(Array.Empty<UserDao>());

            var gradeRepo = new Mock<IGradeRepo>();
            gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((GradeDao?)null);
            gradeRepo.Setup(r => r.GetAllGradesAsync()).ReturnsAsync(Array.Empty<GradeDao>());

            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.GetByMentorIdAsync(It.IsAny<int>())).ReturnsAsync((PairDao?)null);
            pairRepo.Setup(r => r.GetByMenteeIdAsync(It.IsAny<int>())).ReturnsAsync((PairDao?)null);

            var userService = new UserService(
                userRepo.Object, gradeRepo.Object,
                issueRepo.Object, categoryRepo.Object,
                pairRepo.Object, new Mock<ISchoolClassRepo>().Object);

            var notificationService = new NotificationService(
                new EmailService("localhost", 25, "test@test.com", "pwd"),
                userService,
                pairRepo.Object);

            return new IssueService(issueRepo.Object, categoryRepo.Object, notificationService);
        }

        private static IssueCategoryDao MakeCategoryDto(int id = 1, string name = "General") =>
            new IssueCategoryDao { Id = id, Name = name };

        // ── CreateIssueAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateIssue_Fails_WhenDescriptionIsEmpty()
        {
            var sut = BuildService(new Mock<IIssueRepo>(), new Mock<IIssueCategoryRepo>());

            var result = await sut.CreateIssueAsync("", categoryId: 1, reportedByUserId: 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("empty");
        }

        [Fact]
        public async Task CreateIssue_Fails_WhenDescriptionIsWhitespace()
        {
            var sut = BuildService(new Mock<IIssueRepo>(), new Mock<IIssueCategoryRepo>());

            var result = await sut.CreateIssueAsync("   ", categoryId: 1, reportedByUserId: 1);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreateIssue_Fails_WhenRepoReturnsFalse()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync(false);

            var sut = BuildService(issueRepo, new Mock<IIssueCategoryRepo>());

            var result = await sut.CreateIssueAsync("Bullying incident", categoryId: 1, reportedByUserId: 5);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create");
        }

        [Fact]
        public async Task CreateIssue_Succeeds_WhenRepoReturnsTrue()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.CreateAsync("Valid description", 1, 5))
                     .ReturnsAsync(true);

            var sut = BuildService(issueRepo, new Mock<IIssueCategoryRepo>());

            var result = await sut.CreateIssueAsync("Valid description", categoryId: 1, reportedByUserId: 5);

            result.Success.Should().BeTrue();
            issueRepo.Verify(r => r.CreateAsync("Valid description", 1, 5), Times.Once);
        }

        // ── ResolveIssueAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task ResolveIssue_Fails_WhenIssueNotFound()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.ResolveAsync(42)).ReturnsAsync(false);

            var sut = BuildService(issueRepo, new Mock<IIssueCategoryRepo>());

            var result = await sut.ResolveIssueAsync(42);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task ResolveIssue_Succeeds_WhenRepoReturnsTrue()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.ResolveAsync(10)).ReturnsAsync(true);

            var sut = BuildService(issueRepo, new Mock<IIssueCategoryRepo>());

            var result = await sut.ResolveIssueAsync(10);

            result.Success.Should().BeTrue();
        }

        // ── ForwardIssueAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task ForwardIssue_Fails_WhenIssueNotFound()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.ForwardAsync(99, It.IsAny<int>())).ReturnsAsync(false);

            var sut = BuildService(issueRepo, new Mock<IIssueCategoryRepo>());

            var result = await sut.ForwardIssueAsync(99, supervisorId: 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task ForwardIssue_Succeeds_WhenRepoReturnsTrue()
        {
            const int issueId = 5, supervisorId = 2;

            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.ForwardAsync(issueId, supervisorId)).ReturnsAsync(true);
            issueRepo.Setup(r => r.GetByIdAsync(issueId))
                     .ReturnsAsync(new IssueDao { Id = issueId, Description = "Test", CategoryId = 1, ReportedByUserId = 3 });

            var categoryRepo = new Mock<IIssueCategoryRepo>();
            categoryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeCategoryDto());

            var sut = BuildService(issueRepo, categoryRepo);

            var result = await sut.ForwardIssueAsync(issueId, supervisorId);

            result.Success.Should().BeTrue();
            issueRepo.Verify(r => r.ForwardAsync(issueId, supervisorId), Times.Once);
        }

        // ── GetIssuesByUserAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetIssuesByUser_ReturnsEmpty_WhenNoIssuesExist()
        {
            var issueRepo = new Mock<IIssueRepo>();
            issueRepo.Setup(r => r.GetByReporterAsync(7)).ReturnsAsync(Array.Empty<IssueDao>());

            var categoryRepo = new Mock<IIssueCategoryRepo>();
            categoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<IssueCategoryDao>());

            var sut = BuildService(issueRepo, categoryRepo);

            var result = await sut.GetIssuesByUserAsync(7);

            result.Success.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }
    }
}
