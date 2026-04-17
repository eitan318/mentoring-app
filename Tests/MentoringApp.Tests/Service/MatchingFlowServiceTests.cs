using FluentAssertions;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using Moq;
using Xunit;

namespace MentoringApp.Tests.Service
{
    /// <summary>
    /// Unit tests for <see cref="MatchingFlowService"/>.
    ///
    /// Strategy: all repository dependencies are mocked via Moq.
    /// <see cref="UserService"/> and <see cref="SupervisorAssignmentService"/> are
    /// concrete classes whose methods are NOT virtual, so they are constructed with
    /// mocked repositories to control their behaviour indirectly.
    /// </summary>
    public class MatchingFlowServiceTests
    {
        // ── Shared mock objects ──────────────────────────────────────────────
        private readonly Mock<IPairRepo>        _pairRepo        = new(MockBehavior.Strict);
        private readonly Mock<IPairRequestRepo> _pairRequestRepo = new(MockBehavior.Strict);
        private readonly Mock<IMatchScoreRepo>  _matchScoreRepo  = new(MockBehavior.Strict);
        private readonly Mock<IUserRepo>        _userRepo        = new(MockBehavior.Loose);
        private readonly Mock<IGradeRepo>       _gradeRepo       = new(MockBehavior.Loose);
        private readonly Mock<ISubjectRepo>     _subjectRepo     = new(MockBehavior.Loose);
        private readonly Mock<IIssueRepo>       _issueRepo       = new(MockBehavior.Loose);
        private readonly Mock<IIssueCategoryRepo> _issueCategoryRepo = new(MockBehavior.Loose);
        private readonly Mock<ISchoolClassRepo> _schoolClassRepo = new(MockBehavior.Loose);

        // ── Default stub return values ───────────────────────────────────────

        public MatchingFlowServiceTests()
        {
            // Default loose stubs so callers don't need to set everything up.
            _gradeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                      .ReturnsAsync(new GradeDto { Id = 1, Name = "Grade 10", Num = 10 });
            _gradeRepo.Setup(r => r.GetAllGradesAsync())
                      .ReturnsAsync(new List<GradeDto>());
            _issueRepo.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(new List<IssueDto>());
            _issueCategoryRepo.Setup(r => r.GetAllAsync())
                              .ReturnsAsync(new List<IssueCategoryDto>());
            _schoolClassRepo.Setup(r => r.GetBySupervisorAsync(It.IsAny<int>()))
                            .ReturnsAsync(new List<SchoolClassDto>());
            _subjectRepo.Setup(r => r.GetAllSubjectsAsync())
                        .ReturnsAsync(new List<SubjectDto>());
        }

        // ── Factory helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Constructs a real <see cref="UserService"/> using the current mock repos.
        /// </summary>
        private UserService BuildUserService() =>
            new UserService(
                _userRepo.Object,
                _gradeRepo.Object,
                _issueRepo.Object,
                _issueCategoryRepo.Object,
                _pairRepo.Object,
                _schoolClassRepo.Object);

        /// <summary>
        /// Constructs the service under test with all dependencies wired up.
        /// </summary>
        private MatchingFlowService BuildService()
        {
            var userService = BuildUserService();
            var supervisorAssignment = new SupervisorAssignmentService(userService);
            return new MatchingFlowService(
                _pairRepo.Object,
                _pairRequestRepo.Object,
                _matchScoreRepo.Object,
                _userRepo.Object,
                _gradeRepo.Object,
                _subjectRepo.Object,
                userService,
                new CompatibilityScorer(),
                supervisorAssignment);
        }

        /// <summary>
        /// Creates a minimal <see cref="UserDto"/> representing a mentor student.
        /// </summary>
        private static UserDto MakeMentorDto(int id, int maxMentees = 2, int mentorSubjectId = 1) =>
            new UserDto
            {
                Id = id,
                UserName = $"Mentor{id}",
                Email = $"mentor{id}@test.com",
                NationalId = $"M{id:D4}",
                Role = UserRoleType.Student,
                GradeId = 1,
                ClassNum = 1,
                Gender = (int)Gender.Male,
                MentorSubjectId = mentorSubjectId,
                MaxMentees = maxMentees,
                PreferredMenteeGender = (int)GenderPreference.NoPreference
            };

        /// <summary>
        /// Creates a minimal <see cref="UserDto"/> representing a mentee student.
        /// </summary>
        private static UserDto MakeMenteeDto(int id, int menteeSubjectId = 1) =>
            new UserDto
            {
                Id = id,
                UserName = $"Mentee{id}",
                Email = $"mentee{id}@test.com",
                NationalId = $"E{id:D4}",
                Role = UserRoleType.Student,
                GradeId = 1,
                ClassNum = 1,
                Gender = (int)Gender.Female,
                MenteeSubjectId = menteeSubjectId,
                PreferredMentorGender = (int)GenderPreference.NoPreference
            };

        // ════════════════════════════════════════════════════════════════════
        // TIER 1 – SendPairRequestAsync
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task SendPairRequest_Fails_WhenMenteeAlreadyMatched()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int> { menteeId });

            var result = await BuildService().SendPairRequestAsync(menteeId, mentorId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("already matched");
        }

        [Fact]
        public async Task SendPairRequest_Fails_WhenMentorAtCapacity()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            // Mentor already has 1 mentee; MaxMentees = 1
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>
                     {
                         new PairDto { Id = 1, MentorId = mentorId, MenteeId = 99, SupervisorId = 1 }
                     });
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId))
                     .ReturnsAsync(MakeMentorDto(mentorId, maxMentees: 1));

            var result = await BuildService().SendPairRequestAsync(menteeId, mentorId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("no longer available");
        }

        [Fact]
        public async Task SendPairRequest_Fails_WhenRequestAlreadyExists()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId))
                     .ReturnsAsync(MakeMentorDto(mentorId, maxMentees: 3));
            _pairRequestRepo.Setup(r => r.ExistsAsync(menteeId, mentorId))
                            .ReturnsAsync(true);

            var result = await BuildService().SendPairRequestAsync(menteeId, mentorId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("pending request");
        }

        [Fact]
        public async Task SendPairRequest_Succeeds_WhenAllConditionsMet()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId))
                     .ReturnsAsync(MakeMentorDto(mentorId, maxMentees: 3));
            _pairRequestRepo.Setup(r => r.ExistsAsync(menteeId, mentorId))
                            .ReturnsAsync(false);
            _pairRequestRepo.Setup(r => r.CreateAsync(menteeId, mentorId, (int)MatchTier.Direct))
                            .ReturnsAsync(true);

            var result = await BuildService().SendPairRequestAsync(menteeId, mentorId);

            result.Success.Should().BeTrue();
            _pairRequestRepo.Verify(r => r.CreateAsync(menteeId, mentorId, (int)MatchTier.Direct), Times.Once);
        }

        [Fact]
        public async Task SendPairRequest_Fails_WhenRepoCreateFails()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId))
                     .ReturnsAsync(MakeMentorDto(mentorId, maxMentees: 3));
            _pairRequestRepo.Setup(r => r.ExistsAsync(menteeId, mentorId))
                            .ReturnsAsync(false);
            _pairRequestRepo.Setup(r => r.CreateAsync(menteeId, mentorId, (int)MatchTier.Direct))
                            .ReturnsAsync(false);

            var result = await BuildService().SendPairRequestAsync(menteeId, mentorId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create");
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 1 – RejectPairRequestAsync
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RejectPairRequest_Succeeds_WhenUpdateSucceeds()
        {
            const int requestId = 5;

            _pairRequestRepo.Setup(r => r.UpdateStatusAsync(requestId, "Rejected"))
                            .ReturnsAsync(true);

            var result = await BuildService().RejectPairRequestAsync(requestId);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task RejectPairRequest_Fails_WhenUpdateFails()
        {
            const int requestId = 5;

            _pairRequestRepo.Setup(r => r.UpdateStatusAsync(requestId, "Rejected"))
                            .ReturnsAsync(false);

            var result = await BuildService().RejectPairRequestAsync(requestId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Request not found");
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 4 – RunAutoMatchAsync
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task RunAutoMatch_Returns0_WhenNoAvailableMentees()
        {
            // Return no users at all → GetAvailableMenteesAsync returns empty
            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto>());
            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());

            var result = await BuildService().RunAutoMatchAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Fact]
        public async Task RunAutoMatch_Returns0_WhenNoAvailableMentors()
        {
            // Only mentees, no mentors
            var menteeDto = MakeMenteeDto(10);
            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto> { menteeDto });
            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());

            var result = await BuildService().RunAutoMatchAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Fact]
        public async Task RunAutoMatch_CreatesOnePair_ForOneMatchingScore()
        {
            const int menteeId = 10;
            const int mentorId = 20;
            const int supervisorId = 1;

            var menteeDto = MakeMenteeDto(menteeId, menteeSubjectId: 1);
            var mentorDto = MakeMentorDto(mentorId, maxMentees: 2, mentorSubjectId: 1);
            // Supervisor needed for SupervisorAssignmentService fallback
            var supervisorDto = new UserDto
            {
                Id = supervisorId,
                UserName = "Supervisor1",
                Email = "sup@test.com",
                NationalId = "S0001",
                Role = UserRoleType.Supervisor,
                GradeId = 1,
                ClassNum = 1,
                Gender = (int)Gender.Male
            };

            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto> { menteeDto, mentorDto, supervisorDto });
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(menteeDto);
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId)).ReturnsAsync(mentorDto);
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(supervisorDto);

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync())
                     .ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(new List<PairDto>());
            _pairRepo.Setup(r => r.GetBySupervisorIdAsync(supervisorId))
                     .ReturnsAsync(new List<PairDto>());

            _matchScoreRepo.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(new List<MatchScoreDto>
                           {
                               new MatchScoreDto { Id = 1, MenteeId = menteeId, MentorId = mentorId, ScorePercent = 80 }
                           });

            _pairRepo.Setup(r => r.CreateWithTierAsync(It.IsAny<int>(), mentorId, menteeId, (int)MatchTier.AutoMatch, false))
                     .ReturnsAsync(true);

            var result = await BuildService().RunAutoMatchAsync();

            result.Success.Should().BeTrue();
            result.Data.Should().Be(1);
            _pairRepo.Verify(r => r.CreateWithTierAsync(It.IsAny<int>(), mentorId, menteeId, (int)MatchTier.AutoMatch, false), Times.Once);
        }

        [Fact]
        public async Task RunAutoMatch_SkipsAlreadyMatchedMentee_InSameRun()
        {
            // Two scores for the same mentee — only the first (higher score) should be used.
            const int menteeId = 10;
            const int mentorId1 = 20;
            const int mentorId2 = 21;
            const int supervisorId = 1;

            var menteeDto  = MakeMenteeDto(menteeId);
            var mentorDto1 = MakeMentorDto(mentorId1, maxMentees: 2);
            var mentorDto2 = MakeMentorDto(mentorId2, maxMentees: 2);
            var supervisorDto = new UserDto
            {
                Id = supervisorId, UserName = "Supervisor1",
                Email = "sup@test.com", NationalId = "S0001",
                Role = UserRoleType.Supervisor, GradeId = 1, ClassNum = 1,
                Gender = (int)Gender.Male
            };

            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto> { menteeDto, mentorDto1, mentorDto2, supervisorDto });
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(menteeId)).ReturnsAsync(menteeDto);
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId1)).ReturnsAsync(mentorDto1);
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(mentorId2)).ReturnsAsync(mentorDto2);
            _userRepo.Setup(r => r.GetUserDtoByIdAsync(supervisorId)).ReturnsAsync(supervisorDto);

            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync()).ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PairDto>());
            _pairRepo.Setup(r => r.GetBySupervisorIdAsync(supervisorId)).ReturnsAsync(new List<PairDto>());

            // Higher score first (sorted descending)
            _matchScoreRepo.Setup(r => r.GetAllAsync())
                           .ReturnsAsync(new List<MatchScoreDto>
                           {
                               new MatchScoreDto { Id = 1, MenteeId = menteeId, MentorId = mentorId1, ScorePercent = 90 },
                               new MatchScoreDto { Id = 2, MenteeId = menteeId, MentorId = mentorId2, ScorePercent = 60 }
                           });

            _pairRepo.Setup(r => r.CreateWithTierAsync(It.IsAny<int>(), mentorId1, menteeId, (int)MatchTier.AutoMatch, false))
                     .ReturnsAsync(true);

            var result = await BuildService().RunAutoMatchAsync();

            result.Data.Should().Be(1);
            // First mentor gets the pair
            _pairRepo.Verify(r => r.CreateWithTierAsync(It.IsAny<int>(), mentorId1, menteeId, (int)MatchTier.AutoMatch, false), Times.Once);
            // Second mentor should NOT be tried for the same mentee
            _pairRepo.Verify(r => r.CreateWithTierAsync(It.IsAny<int>(), mentorId2, menteeId, It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 2 – GenerateScoreMatrixAsync
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GenerateScoreMatrix_Fails_WhenNoMenteesOrMentors()
        {
            // No users available → both lists empty
            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto>());
            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync()).ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PairDto>());
            _matchScoreRepo.Setup(r => r.ClearAllAsync()).Returns(Task.CompletedTask);

            var result = await BuildService().GenerateScoreMatrixAsync();

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("No unmatched users");
        }

        [Fact]
        public async Task GenerateScoreMatrix_ClearsAndBulkInserts_WhenUsersAvailable()
        {
            const int menteeId = 10;
            const int mentorId = 20;

            var menteeDto = MakeMenteeDto(menteeId, menteeSubjectId: 1);
            var mentorDto = MakeMentorDto(mentorId, maxMentees: 2, mentorSubjectId: 1);

            _userRepo.Setup(r => r.GetAllUserDtosAsync())
                     .ReturnsAsync(new List<UserDto> { menteeDto, mentorDto });
            _pairRepo.Setup(r => r.GetMatchedMenteeIdsAsync()).ReturnsAsync(new List<int>());
            _pairRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PairDto>());

            _matchScoreRepo.Setup(r => r.ClearAllAsync()).Returns(Task.CompletedTask);
            _matchScoreRepo.Setup(r => r.BulkInsertAsync(It.IsAny<IEnumerable<MatchScoreDto>>()))
                           .Returns(Task.CompletedTask);

            var result = await BuildService().GenerateScoreMatrixAsync();

            result.Success.Should().BeTrue();
            _matchScoreRepo.Verify(r => r.ClearAllAsync(), Times.Once);
            _matchScoreRepo.Verify(r => r.BulkInsertAsync(It.Is<IEnumerable<MatchScoreDto>>(
                scores => scores.Any(s => s.MenteeId == menteeId && s.MentorId == mentorId))), Times.Once);
        }
    }
}
