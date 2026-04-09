using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;

namespace MentoringApp.Service
{
    /// <summary>
    /// Orchestrates the 5-tier pair-matching pipeline.
    /// Each public method corresponds to one tier of the process.
    /// </summary>
    public class MatchingFlowService
    {
        private readonly IPairRepo _pairRepo;
        private readonly IPairRequestRepo _pairRequestRepo;
        private readonly IMatchScoreRepo _matchScoreRepo;
        private readonly IUserRepo _userRepo;
        private readonly IGradeRepo _gradeRepo;
        private readonly ISubjectRepo _subjectRepo;
        private readonly UserService _userService;
        private readonly CompatibilityScorer _scorer;
        private readonly SupervisorAssignmentService _supervisorAssignment;

        public MatchingFlowService(
            IPairRepo pairRepo,
            IPairRequestRepo pairRequestRepo,
            IMatchScoreRepo matchScoreRepo,
            IUserRepo userRepo,
            IGradeRepo gradeRepo,
            ISubjectRepo subjectRepo,
            UserService userService,
            CompatibilityScorer scorer,
            SupervisorAssignmentService supervisorAssignment)
        {
            _pairRepo = pairRepo;
            _pairRequestRepo = pairRequestRepo;
            _matchScoreRepo = matchScoreRepo;
            _userRepo = userRepo;
            _gradeRepo = gradeRepo;
            _subjectRepo = subjectRepo;
            _userService = userService;
            _scorer = scorer;
            _supervisorAssignment = supervisorAssignment;
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 1 – Direct Request Window
        // ════════════════════════════════════════════════════════════════════

        public async Task<IEnumerable<StudentModel>> GetAvailableMentorsAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var allPairs = await _pairRepo.GetAllAsync();

            var mentorPairCounts = allPairs
                .GroupBy(p => p.MentorId)
                .ToDictionary(g => g.Key, g => g.Count());

            return allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentor &&
                       mentorPairCounts.GetValueOrDefault(s.Id, 0) < (s.MentorProfile?.MaxMentees ?? 1));
        }

        public async Task<IEnumerable<StudentModel>> GetAvailableMenteesAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var matchedMenteeIds = (await _pairRepo.GetMatchedMenteeIdsAsync()).ToHashSet();

            return allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentee && !matchedMenteeIds.Contains(s.Id));
        }

        public async Task<Result> SendPairRequestAsync(int menteeId, int mentorId)
        {
            var matchedMentees = await _pairRepo.GetMatchedMenteeIdsAsync();
            if (matchedMentees.Contains(menteeId))
                return Result.Failure("You are already matched with a mentor.");

            var allPairs = await _pairRepo.GetAllAsync();
            var currentMentees = allPairs.Count(p => p.MentorId == mentorId);
            var mentorUser = await _userService.GetUserByIdAsync(mentorId);
            var maxMentees = (mentorUser.Data as StudentModel)?.MentorProfile?.MaxMentees ?? 1;

            if (currentMentees >= maxMentees)
                return Result.Failure("That mentor is no longer available.");

            bool exists = await _pairRequestRepo.ExistsAsync(menteeId, mentorId);
            if (exists)
                return Result.Failure("A pending request to that mentor already exists.");

            bool created = await _pairRequestRepo.CreateAsync(menteeId, mentorId, (int)MatchTier.Direct);
            return created ? Result.Ok() : Result.Failure("Failed to create request.");
        }

        /// <summary>
        /// Accepts a pending pair request: validates mentor capacity, creates the pair, then
        /// cancels all other pending requests for both the mentee and mentor to avoid duplicates.
        /// </summary>
        /// <remarks>
        /// The request lookup iterates all mentors because <see cref="IPairRequestRepo"/>
        /// does not yet expose a GetByIdAsync method. Replace with that once available.
        /// </remarks>
        public async Task<Result> AcceptPairRequestAsync(int requestId, int supervisorId)
        {
            // TODO: replace loop with _pairRequestRepo.GetByIdAsync(requestId)
            //       once that method is added to IPairRequestRepo.
            var allUsers = await _userService.GetAllUsersAsync();
            var mentorIds = allUsers.OfType<StudentModel>().Where(s => s.IsMentor).Select(s => s.Id);

            PairRequestDto? req = null;
            foreach (var mentorId in mentorIds)
            {
                var reqs = await _pairRequestRepo.GetByMentorAsync(mentorId);
                req = reqs.FirstOrDefault(r => r.Id == requestId);
                if (req != null) break;
            }

            if (req == null) return Result.Failure("Request not found.");
            if (req.Status != "Pending") return Result.Failure("Request is no longer pending.");

            var allPairs = await _pairRepo.GetAllAsync();
            var currentMentees = allPairs.Count(p => p.MentorId == req.MentorId);
            var mentorUser = await _userService.GetUserByIdAsync(req.MentorId);
            var maxMentees = (mentorUser.Data as StudentModel)?.MentorProfile?.MaxMentees ?? 1;

            if (currentMentees >= maxMentees)
            {
                await RejectPairRequestAsync(requestId);
                return Result.Failure("You have reached your maximum number of mentees.");
            }

            int assignedSupervisorId = await _supervisorAssignment.GetForMenteeAsync(req.MenteeId);

            bool created = await _pairRepo.CreateWithTierAsync(
                assignedSupervisorId, req.MentorId, req.MenteeId,
                req.Tier, isProfileIncomplete: false);

            if (!created) return Result.Failure("Failed to create pair in database.");

            await _pairRequestRepo.UpdateStatusAsync(requestId, "Accepted");
            await _pairRequestRepo.CancelPendingForUsersAsync(req.MenteeId, req.MentorId);

            return Result.Ok();
        }

        public async Task<Result> RejectPairRequestAsync(int requestId)
        {
            bool updated = await _pairRequestRepo.UpdateStatusAsync(requestId, "Rejected");
            return updated ? Result.Ok() : Result.Failure("Request not found.");
        }

        public async Task<IEnumerable<PairRequest>> GetPendingRequestsForMentorAsync(int mentorId)
        {
            var dtos = await _pairRequestRepo.GetByMentorAsync(mentorId);
            var result = new List<PairRequest>();

            foreach (var dto in dtos)
            {
                var menteeResult = await _userService.GetUserByIdAsync(dto.MenteeId);
                var menteeModel = menteeResult.Data as StudentModel;
                var subjectName = await GetSubjectNameAsync(menteeModel?.MenteeProfile?.SubjectToLearn);

                result.Add(new PairRequest
                {
                    Id = dto.Id,
                    MenteeId = dto.MenteeId,
                    MentorId = dto.MentorId,
                    Status = Enum.TryParse<PairRequestStatus>(dto.Status, out var s) ? s : PairRequestStatus.Pending,
                    Tier = (MatchTier)dto.Tier,
                    CreatedAt = DateTime.TryParse(dto.CreatedAt, out var d) ? d : DateTime.MinValue,
                    MenteeName = menteeModel?.UserName ?? "Unknown",
                    MenteeProfilePicturePath = menteeModel?.ProfilePicturePath ?? string.Empty,
                    MenteeSubjectName = subjectName
                });
            }

            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 2 – Score Matrix Generation
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result> GenerateScoreMatrixAsync()
        {
            await _matchScoreRepo.ClearAllAsync();

            var mentees = (await GetAvailableMenteesAsync()).ToList();
            var mentors = (await GetAvailableMentorsAsync()).ToList();

            if (!mentees.Any() || !mentors.Any())
                return Result.Failure("No unmatched users found to build score matrix.");

            var scores = new List<MatchScoreDto>();

            foreach (var mentee in mentees)
            {
                foreach (var mentor in mentors)
                {
                    scores.Add(new MatchScoreDto
                    {
                        MenteeId = mentee.Id,
                        MentorId = mentor.Id,
                        ScorePercent = _scorer.Calculate(
                            mentee.MenteeProfile?.SubjectToLearn,
                            mentor.MentorProfile?.SubjectToTeach)
                    });
                }
            }

            await _matchScoreRepo.BulkInsertAsync(scores);
            return Result.Ok();
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 3 – Selection Gallery
        // ════════════════════════════════════════════════════════════════════

        public async Task<IEnumerable<MatchScore>> GetTopRecommendationsAsync(int menteeId, int topN = 3)
        {
            var dtos = await _matchScoreRepo.GetTopForMenteeAsync(menteeId, topN);

            var allPairs = await _pairRepo.GetAllAsync();
            var mentorPairCounts = allPairs
                .GroupBy(p => p.MentorId)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = new List<MatchScore>();

            foreach (var dto in dtos)
            {
                var mentorResult = await _userService.GetUserByIdAsync(dto.MentorId);
                var mentorModel = mentorResult.Data as StudentModel;

                int currentMentees = mentorPairCounts.GetValueOrDefault(dto.MentorId, 0);
                int maxMentees = mentorModel?.MentorProfile?.MaxMentees ?? 1;
                if (currentMentees >= maxMentees) continue;

                var menteeResult = await _userService.GetUserByIdAsync(dto.MenteeId);
                var menteeModel = menteeResult.Data as StudentModel;

                result.Add(new MatchScore
                {
                    Id = dto.Id,
                    MenteeId = dto.MenteeId,
                    MentorId = dto.MentorId,
                    ScorePercent = dto.ScorePercent,
                    MentorName = mentorModel?.UserName ?? "Unknown",
                    MentorProfilePicturePath = mentorModel?.ProfilePicturePath ?? string.Empty,
                    MentorSubjectName = await GetSubjectNameAsync(mentorModel?.MentorProfile?.SubjectToTeach),
                    MenteeSubjectName = await GetSubjectNameAsync(menteeModel?.MenteeProfile?.SubjectToLearn)
                });
            }

            return result;
        }

        public async Task<Result> GalleryPickAsync(int menteeId, int mentorId, int supervisorId)
        {
            var matchedMentees = await _pairRepo.GetMatchedMenteeIdsAsync();
            if (matchedMentees.Contains(menteeId))
                return Result.Failure("You are already matched.");

            var allPairs = await _pairRepo.GetAllAsync();
            var currentMentees = allPairs.Count(p => p.MentorId == mentorId);
            var mentorUser = await _userService.GetUserByIdAsync(mentorId);
            var maxMentees = (mentorUser.Data as StudentModel)?.MentorProfile?.MaxMentees ?? 1;

            if (currentMentees >= maxMentees)
                return Result.Failure("That mentor is no longer available.");

            int assignedSupervisorId = await _supervisorAssignment.GetForMenteeAsync(menteeId);

            bool created = await _pairRepo.CreateWithTierAsync(
                assignedSupervisorId, mentorId, menteeId,
                (int)MatchTier.GalleryChoice, isProfileIncomplete: false);

            if (!created) return Result.Failure("Failed to create pair.");

            await _pairRequestRepo.CancelPendingForUsersAsync(menteeId, mentorId);
            return Result.Ok();
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 4 – Algorithmic Auto-Match
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Greedy auto-match over the pre-built score matrix (Tier 4).
        /// Scores are sorted descending so the highest-compatibility pairs are assigned first.
        /// Each mentee and mentor can appear in at most one pair per run — once committed,
        /// both are excluded from further iterations via in-memory HashSets.
        /// Returns the number of pairs successfully created.
        /// </summary>
        public async Task<Result<int>> RunAutoMatchAsync()
        {
            var mentees = (await GetAvailableMenteesAsync()).Where(m => m.MenteeProfile != null).ToList();
            var mentors = (await GetAvailableMentorsAsync()).Where(m => m.MentorProfile != null).ToList();

            if (!mentees.Any() || !mentors.Any())
                return Result<int>.Ok(0);

            var allScores = (await _matchScoreRepo.GetAllAsync())
                .Where(s => mentees.Any(m => m.Id == s.MenteeId) && mentors.Any(m => m.Id == s.MentorId))
                .OrderByDescending(s => s.ScorePercent)
                .ToList();

            var matchedMentees = new HashSet<int>();
            var matchedMentors = new HashSet<int>();
            int pairsCreated = 0;

            foreach (var score in allScores)
            {
                if (matchedMentees.Contains(score.MenteeId)) continue;
                if (matchedMentors.Contains(score.MentorId)) continue;

                int assignedSupervisorId = await _supervisorAssignment.GetForMenteeAsync(score.MenteeId);

                bool created = await _pairRepo.CreateWithTierAsync(
                    assignedSupervisorId, score.MentorId, score.MenteeId,
                    (int)MatchTier.AutoMatch, isProfileIncomplete: false);

                if (created)
                {
                    matchedMentees.Add(score.MenteeId);
                    matchedMentors.Add(score.MentorId);
                    pairsCreated++;
                }
            }

            return Result<int>.Ok(pairsCreated);
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 5 – Fallback / Safety Net
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Fallback random match (Tier 5) for mentees still unmatched after auto-match.
        /// Available mentors are shuffled randomly, then assigned one-per-mentee from a queue.
        /// Pairs created here are flagged <c>isProfileIncomplete</c> when either participant
        /// lacks a complete profile, so supervisors can review them.
        /// </summary>
        public async Task<Result<int>> RunFallbackMatchAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var matchedMenteeIds = (await _pairRepo.GetMatchedMenteeIdsAsync()).ToHashSet();

            var allPairs = await _pairRepo.GetAllAsync();
            var mentorPairCounts = allPairs
                .GroupBy(p => p.MentorId)
                .ToDictionary(g => g.Key, g => g.Count());

            var incompleteMentees = allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentee && !matchedMenteeIds.Contains(s.Id))
                .ToList();

            var availableMentors = allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentor &&
                       mentorPairCounts.GetValueOrDefault(s.Id, 0) < (s.MentorProfile?.MaxMentees ?? 1))
                .ToList();

            if (!incompleteMentees.Any() || !availableMentors.Any())
                return Result<int>.Ok(0);

            // Shuffle mentors to avoid systematic bias in fallback assignment
            var rng = new Random();
            var remainingMentors = new Queue<StudentModel>(availableMentors.OrderBy(_ => rng.Next()));
            int pairsCreated = 0;

            foreach (var mentee in incompleteMentees)
            {
                if (!remainingMentors.TryDequeue(out var mentor)) break;

                // Flag for supervisor review if either profile is incomplete
                bool isIncomplete = mentee.MenteeProfile == null || mentor.MentorProfile == null;
                int assignedSupervisorId = await _supervisorAssignment.GetForMenteeAsync(mentee.Id);

                bool created = await _pairRepo.CreateWithTierAsync(
                    assignedSupervisorId, mentor.Id, mentee.Id,
                    (int)MatchTier.FallbackRandom, isProfileIncomplete: isIncomplete);

                if (created) pairsCreated++;
            }

            return Result<int>.Ok(pairsCreated);
        }

        // ════════════════════════════════════════════════════════════════════
        // Private helpers
        // ════════════════════════════════════════════════════════════════════

        private async Task<string> GetSubjectNameAsync(int? subjectId)
        {
            if (subjectId == null) return "N/A";
            var subjects = await _subjectRepo.GetAllSubjectsAsync();
            return subjects.FirstOrDefault(s => s.Id == subjectId)?.Name ?? "Unknown";
        }
    }
}