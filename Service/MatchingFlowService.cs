using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;

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

        public MatchingFlowService(
            IPairRepo pairRepo,
            IPairRequestRepo pairRequestRepo,
            IMatchScoreRepo matchScoreRepo,
            IUserRepo userRepo,
            IGradeRepo gradeRepo,
            ISubjectRepo subjectRepo,
            UserService userService)
        {
            _pairRepo = pairRepo;
            _pairRequestRepo = pairRequestRepo;
            _matchScoreRepo = matchScoreRepo;
            _userRepo = userRepo;
            _gradeRepo = gradeRepo;
            _subjectRepo = subjectRepo;
            _userService = userService;
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 1 – Direct Request Window
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Returns active (unmatched) mentors visible to a mentee during Tier 1.</summary>
        public async Task<IEnumerable<StudentModel>> GetAvailableMentorsAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var matchedMentorIds = (await _pairRepo.GetMatchedMentorIdsAsync()).ToHashSet();

            return allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentor && !matchedMentorIds.Contains(s.Id));
        }

        /// <summary>Returns active (unmatched) mentees – visible to admin and for algorithmic tiers.</summary>
        public async Task<IEnumerable<StudentModel>> GetAvailableMenteesAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var matchedMenteeIds = (await _pairRepo.GetMatchedMenteeIdsAsync()).ToHashSet();

            return allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentee && !matchedMenteeIds.Contains(s.Id));
        }

        /// <summary>
        /// A mentee sends a Tier 1 request to a mentor.
        /// Returns failure if the mentee is already matched, the mentor is already matched,
        /// or a pending request already exists.
        /// </summary>
        public async Task<Result> SendPairRequestAsync(int menteeId, int mentorId)
        {
            var matchedMentees = await _pairRepo.GetMatchedMenteeIdsAsync();
            if (matchedMentees.Contains(menteeId))
                return Result.Failure("You are already matched with a mentor.");

            var matchedMentors = await _pairRepo.GetMatchedMentorIdsAsync();
            if (matchedMentors.Contains(mentorId))
                return Result.Failure("That mentor is no longer available.");

            bool exists = await _pairRequestRepo.ExistsAsync(menteeId, mentorId);
            if (exists)
                return Result.Failure("A pending request to that mentor already exists.");

            bool created = await _pairRequestRepo.CreateAsync(menteeId, mentorId, (int)MatchTier.Direct);
            return created ? Result.Ok() : Result.Failure("Failed to create request.");
        }

        /// <summary>
        /// A mentor accepts a pending pair request.
        /// The supervisor ID is taken from an available supervisor (first one found for simplicity;
        /// real logic may assign a specific supervisor).
        /// </summary>
        public async Task<Result> AcceptPairRequestAsync(int requestId, int supervisorId)
        {
            var requests = await _pairRequestRepo.GetByMentorAsync(-1); // workaround – fetch by id below
            // We look up the request via mentor-side list; use a broad fetch
            var allUsers = await _userService.GetAllUsersAsync();

            // Find the request in all pending requests for this supervisor's mentors
            // Since we don't have GetByIdAsync on requests, find via mentor pool
            var mentors = allUsers.OfType<StudentModel>().Where(s => s.IsMentor).Select(s => s.Id);
            PairRequestDto? req = null;
            foreach (var mId in mentors)
            {
                var mReqs = await _pairRequestRepo.GetByMentorAsync(mId);
                req = mReqs.FirstOrDefault(r => r.Id == requestId);
                if (req != null) break;
            }

            if (req == null) return Result.Failure("Request not found.");
            if (req.Status != "Pending") return Result.Failure("Request is no longer pending.");

            // Create the pair using the tier from the request
            bool created = await _pairRepo.CreateWithTierAsync(
                supervisorId, req.MentorId, req.MenteeId,
                req.Tier, isProfileIncomplete: false);

            if (!created) return Result.Failure("Failed to create pair in database.");

            // Mark request as accepted and cancel other pending requests for both users
            await _pairRequestRepo.UpdateStatusAsync(requestId, "Accepted");
            await _pairRequestRepo.CancelPendingForUsersAsync(req.MenteeId, req.MentorId);

            return Result.Ok();
        }

        /// <summary>A mentor rejects a pair request.</summary>
        public async Task<Result> RejectPairRequestAsync(int requestId)
        {
            bool updated = await _pairRequestRepo.UpdateStatusAsync(requestId, "Rejected");
            return updated ? Result.Ok() : Result.Failure("Request not found.");
        }

        /// <summary>Returns all pending requests for a mentor (Tier 1 and Tier 3).</summary>
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

        /// <summary>
        /// Runs after Tier 1 deadline. Calculates compatibility scores for all
        /// remaining unmatched mentors and mentees, stores results in MatchScores table.
        /// Score = 100 if subjects match exactly, 0 if they differ (simple single-subject version).
        /// </summary>
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
                int? menteeSubject = mentee.MenteeProfile?.SubjectToLearn;

                foreach (var mentor in mentors)
                {
                    int? mentorSubject = mentor.MentorProfile?.SubjectToTeach;

                    double score = CalculateCompatibility(menteeSubject, mentorSubject);
                    scores.Add(new MatchScoreDto
                    {
                        MenteeId = mentee.Id,
                        MentorId = mentor.Id,
                        ScorePercent = score
                    });
                }
            }

            await _matchScoreRepo.BulkInsertAsync(scores);
            return Result.Ok();
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 3 – Selection Gallery
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Returns the top 3 mentor recommendations for a mentee based on the score matrix.</summary>
        public async Task<IEnumerable<MatchScore>> GetTopRecommendationsAsync(int menteeId, int topN = 3)
        {
            var dtos = await _matchScoreRepo.GetTopForMenteeAsync(menteeId, topN);
            // Also filter out mentors that became matched since Tier 2 ran
            var matchedMentors = (await _pairRepo.GetMatchedMentorIdsAsync()).ToHashSet();

            var result = new List<MatchScore>();
            foreach (var dto in dtos.Where(d => !matchedMentors.Contains(d.MentorId)))
            {
                var mentorResult = await _userService.GetUserByIdAsync(dto.MentorId);
                var mentorModel = mentorResult.Data as StudentModel;
                var subjectName = await GetSubjectNameAsync(mentorModel?.MentorProfile?.SubjectToTeach);

                var menteeResult = await _userService.GetUserByIdAsync(dto.MenteeId);
                var menteeModel = menteeResult.Data as StudentModel;
                var menteeSubjectName = await GetSubjectNameAsync(menteeModel?.MenteeProfile?.SubjectToLearn);

                result.Add(new MatchScore
                {
                    Id = dto.Id,
                    MenteeId = dto.MenteeId,
                    MentorId = dto.MentorId,
                    ScorePercent = dto.ScorePercent,
                    MentorName = mentorModel?.UserName ?? "Unknown",
                    MentorProfilePicturePath = mentorModel?.ProfilePicturePath ?? string.Empty,
                    MentorSubjectName = subjectName,
                    MenteeSubjectName = menteeSubjectName
                });
            }

            return result;
        }

        /// <summary>
        /// A mentee picks a mentor from the Tier 3 gallery –
        /// creates an instant pair (no confirmation required in this implementation).
        /// </summary>
        public async Task<Result> GalleryPickAsync(int menteeId, int mentorId, int supervisorId)
        {
            var matchedMentees = await _pairRepo.GetMatchedMenteeIdsAsync();
            if (matchedMentees.Contains(menteeId))
                return Result.Failure("You are already matched.");

            var matchedMentors = await _pairRepo.GetMatchedMentorIdsAsync();
            if (matchedMentors.Contains(mentorId))
                return Result.Failure("That mentor is no longer available.");

            bool created = await _pairRepo.CreateWithTierAsync(
                supervisorId, mentorId, menteeId,
                (int)MatchTier.GalleryChoice, isProfileIncomplete: false);

            if (!created) return Result.Failure("Failed to create pair.");

            await _pairRequestRepo.CancelPendingForUsersAsync(menteeId, mentorId);
            return Result.Ok();
        }

        // ════════════════════════════════════════════════════════════════════
        // TIER 4 – Algorithmic Auto-Match
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Pairs remaining unmatched users using the Tier 2 score matrix.
        /// Uses a greedy best-score-first approach (not full Gale-Shapley for simplicity).
        /// Returns a count of pairs created.
        /// </summary>
        public async Task<Result<int>> RunAutoMatchAsync(int supervisorId)
        {
            var mentees = (await GetAvailableMenteesAsync()).Where(m => m.MenteeProfile != null).ToList();
            var mentors = (await GetAvailableMentorsAsync()).Where(m => m.MentorProfile != null).ToList();

            if (!mentees.Any() || !mentors.Any())
                return Result<int>.Ok(0);

            // Fetch all scores and sort descending
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

                bool created = await _pairRepo.CreateWithTierAsync(
                    supervisorId, score.MentorId, score.MenteeId,
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
        /// Randomly pairs users who have incomplete profiles (no subject data).
        /// Mentors with incomplete profiles may also be matched here.
        /// </summary>
        public async Task<Result<int>> RunFallbackMatchAsync(int supervisorId)
        {
            // Users with COMPLETE profiles are handled by Tier 4; here we grab the incomplete ones
            var allUsers = await _userService.GetAllUsersAsync();
            var matchedMenteeIds = (await _pairRepo.GetMatchedMenteeIdsAsync()).ToHashSet();
            var matchedMentorIds = (await _pairRepo.GetMatchedMentorIdsAsync()).ToHashSet();

            var incompleteMentees = allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentee && !matchedMenteeIds.Contains(s.Id))
                .ToList();

            var availableMentors = allUsers
                .OfType<StudentModel>()
                .Where(s => s.IsMentor && !matchedMentorIds.Contains(s.Id))
                .ToList();

            if (!incompleteMentees.Any() || !availableMentors.Any())
                return Result<int>.Ok(0);

            var rng = new Random();
            var remainingMentors = new Queue<StudentModel>(availableMentors.OrderBy(_ => rng.Next()));
            int pairsCreated = 0;

            foreach (var mentee in incompleteMentees)
            {
                if (!remainingMentors.TryDequeue(out var mentor)) break;

                bool isIncomplete = mentee.MenteeProfile == null || mentor.MentorProfile == null;

                bool created = await _pairRepo.CreateWithTierAsync(
                    supervisorId, mentor.Id, mentee.Id,
                    (int)MatchTier.FallbackRandom, isProfileIncomplete: isIncomplete);

                if (created) pairsCreated++;
            }

            return Result<int>.Ok(pairsCreated);
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Compatibility score: 100 if subject IDs match, otherwise 0.
        /// Can be extended to partial/weighted matching in the future.
        /// </summary>
        private static double CalculateCompatibility(int? menteeSubject, int? mentorSubject)
        {
            if (menteeSubject == null || mentorSubject == null) return 0;
            return menteeSubject == mentorSubject ? 100.0 : 0.0;
        }

        private async Task<string> GetSubjectNameAsync(int? subjectId)
        {
            if (subjectId == null) return "N/A";
            var subjects = await _subjectRepo.GetAllSubjectsAsync();
            return subjects.FirstOrDefault(s => s.Id == subjectId)?.Name ?? "Unknown";
        }
    }
}
