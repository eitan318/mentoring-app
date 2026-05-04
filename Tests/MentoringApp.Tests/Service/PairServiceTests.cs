using FluentAssertions;
using MentoringApp.Data.Dao;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;
using MentoringApp.Service;
using Moq;
using Xunit;

namespace MentoringApp.Tests.Service
{
    public class PairServiceTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private static UserService BuildUserService(Mock<IUserRepo> userRepo)
        {
            var gradeRepo = new Mock<IGradeRepo>();
            gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync(new GradeDao { Id = 1, Name = "Grade 9", Num = 9 });
            gradeRepo.Setup(r => r.GetAllGradesAsync()).ReturnsAsync(Array.Empty<GradeDao>());

            return new UserService(
                userRepo.Object,
                gradeRepo.Object,
                new Mock<IIssueRepo>().Object,
                new Mock<IIssueCategoryRepo>().Object,
                new Mock<IPairRepo>().Object,
                new Mock<ISchoolClassRepo>().Object);
        }

        private static UserDao MakeSupervisorDto(int id) =>
            new UserDao { Id = id, UserName = $"Sup{id}", Email = $"s{id}@test.com", NationalId = $"S{id}", Role = UserRoleType.Supervisor, GradeId = 1, ClassNum = 1, Gender = 1 };

        private static UserDao MakeMentorDto(int id) =>
            new UserDao { Id = id, UserName = $"Mentor{id}", Email = $"m{id}@test.com", NationalId = $"M{id}", Role = UserRoleType.Student, GradeId = 1, ClassNum = 1, Gender = 1, MentorSubjectId = 1 };

        private static UserDao MakeMenteeDto(int id) =>
            new UserDao { Id = id, UserName = $"Mentee{id}", Email = $"e{id}@test.com", NationalId = $"E{id}", Role = UserRoleType.Student, GradeId = 1, ClassNum = 1, Gender = 2, MenteeSubjectId = 1 };

        // ── CreatePairAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task CreatePair_Fails_WhenSupervisorNotFound()
        {
            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(It.IsAny<int>())).ReturnsAsync((UserDao?)null);
            var pairRepo = new Mock<IPairRepo>();

            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(1, 2, 3);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("supervisor");
        }

        [Fact]
        public async Task CreatePair_Fails_WhenSupervisorIsAStudent()
        {
            const int supervisorId = 1, mentorId = 2, menteeId = 3;

            var userRepo = new Mock<IUserRepo>();
            // Returns a Student for the supervisor slot → wrong role
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeMentorDto(supervisorId));

            var pairRepo = new Mock<IPairRepo>();
            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(supervisorId, mentorId, menteeId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("supervisor");
        }

        [Fact]
        public async Task CreatePair_Fails_WhenMentorHasNoMentorProfile()
        {
            const int supervisorId = 1, mentorId = 2, menteeId = 3;

            // mentorDto has no MentorSubjectId → not a mentor
            var notAMentorDto = MakeMenteeDto(mentorId);

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeSupervisorDto(supervisorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(notAMentorDto);

            var pairRepo = new Mock<IPairRepo>();
            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(supervisorId, mentorId, menteeId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("mentor");
        }

        [Fact]
        public async Task CreatePair_Fails_WhenMenteeHasNoMenteeProfile()
        {
            const int supervisorId = 1, mentorId = 2, menteeId = 3;

            // menteeDto has no MenteeSubjectId → not a mentee
            var notAMenteeDto = MakeMentorDto(menteeId);

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeSupervisorDto(supervisorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(MakeMentorDto(mentorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(notAMenteeDto);

            var pairRepo = new Mock<IPairRepo>();
            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(supervisorId, mentorId, menteeId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("mentee");
        }

        [Fact]
        public async Task CreatePair_Fails_WhenRepoCreateReturnsFalse()
        {
            const int supervisorId = 1, mentorId = 2, menteeId = 3;

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeSupervisorDto(supervisorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(MakeMentorDto(mentorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(MakeMenteeDto(menteeId));

            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.CreateAsync(supervisorId, mentorId, menteeId)).ReturnsAsync(false);

            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(supervisorId, mentorId, menteeId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create");
        }

        [Fact]
        public async Task CreatePair_Succeeds_WhenAllRolesValidAndRepoSucceeds()
        {
            const int supervisorId = 1, mentorId = 2, menteeId = 3;

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeSupervisorDto(supervisorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(MakeMentorDto(mentorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(MakeMenteeDto(menteeId));

            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.CreateAsync(supervisorId, mentorId, menteeId)).ReturnsAsync(true);

            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.CreatePairAsync(supervisorId, mentorId, menteeId);

            result.Success.Should().BeTrue();
            pairRepo.Verify(r => r.CreateAsync(supervisorId, mentorId, menteeId), Times.Once);
        }

        // ── SeparatePairAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task SeparatePair_Fails_WhenPairNotFound()
        {
            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.DeleteAsync(99)).ReturnsAsync(false);

            var sut = new PairService(pairRepo.Object, BuildUserService(new Mock<IUserRepo>()));

            var result = await sut.SeparatePairAsync(99);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task SeparatePair_Succeeds_WhenPairExists()
        {
            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.DeleteAsync(5)).ReturnsAsync(true);

            var sut = new PairService(pairRepo.Object, BuildUserService(new Mock<IUserRepo>()));

            var result = await sut.SeparatePairAsync(5);

            result.Success.Should().BeTrue();
        }

        // ── GetPairById ────────────────────────────────────────────────────────

        [Fact]
        public async Task GetPairById_Fails_WhenPairNotFound()
        {
            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((PairDao?)null);

            var sut = new PairService(pairRepo.Object, BuildUserService(new Mock<IUserRepo>()));

            var result = await sut.GetPairByIdAsync(7);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not found");
        }

        [Fact]
        public async Task GetPairById_Succeeds_WhenPairAndUsersExist()
        {
            const int pairId = 1, mentorId = 2, menteeId = 3, supervisorId = 4;

            var pairDto = new PairDao { Id = pairId, MentorId = mentorId, MenteeId = menteeId, SupervisorId = supervisorId };

            var userRepo = new Mock<IUserRepo>();
            userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(MakeMentorDto(mentorId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(MakeMenteeDto(menteeId));
            userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(MakeSupervisorDto(supervisorId));

            var pairRepo = new Mock<IPairRepo>();
            pairRepo.Setup(r => r.GetByIdAsync(pairId)).ReturnsAsync(pairDto);

            var sut = new PairService(pairRepo.Object, BuildUserService(userRepo));

            var result = await sut.GetPairByIdAsync(pairId);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(pairId);
            result.Data.Mentor.Id.Should().Be(mentorId);
            result.Data.Mentee.Id.Should().Be(menteeId);
        }
    }
}
