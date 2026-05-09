using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;

namespace MentoringApp.Service
{
    /// <summary>
    /// CRUD and query operations for mentor–mentee pairs.
    /// Hydrates <see cref="PairModel"/> models from <see cref="PairDao"/>s by loading both
    /// mentor and mentee as <see cref="StudentModel"/> instances via <see cref="UserService"/>.
    /// Returns <c>null</c> from <see cref="MapDtoToPairAsync"/> if either user fails to load,
    /// so callers should check <see cref="Result{T}.Success"/> before using the data.
    /// </summary>
    public class PairService
    {
        private readonly IPairRepo _pairRepo;
        private readonly UserService _userService;
        private readonly SupervisorAssignmentService _supervisorAssignment;

        public PairService(IPairRepo pairRepo, UserService userService, SupervisorAssignmentService supervisorAssignment)
        {
            _pairRepo = pairRepo;
            _userService = userService;
            _supervisorAssignment = supervisorAssignment;
        }

        public async Task<Result<IEnumerable<PairModel>>> GetAllPairsAsync()
        {
            var dtos = await _pairRepo.GetAllAsync();
            var pairs = new List<PairModel>();
            foreach (var dto in dtos)
            {
                var pair = await MapDtoToPairAsync(dto);
                if (pair != null) pairs.Add(pair);
            }
            return Result<IEnumerable<PairModel>>.Ok(pairs);
        }

        public async Task<Result<PairModel>> GetPairByIdAsync(int id)
        {
            var dto = await _pairRepo.GetByIdAsync(id);
            if (dto == null) return Result<PairModel>.Failure("Pair not found.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<PairModel>.Ok(pair) : Result<PairModel>.Failure("Failed to build pair.");
        }

        public async Task<Result<PairModel>> GetPairByMentorIdAsync(int mentorId)
        {
            var dto = await _pairRepo.GetByMentorIdAsync(mentorId);
            if (dto == null) return Result<PairModel>.Failure("No pair found for this mentor.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<PairModel>.Ok(pair) : Result<PairModel>.Failure("Failed to build pair.");
        }

        public async Task<Result<PairModel>> GetPairByMenteeIdAsync(int menteeId)
        {
            var dto = await _pairRepo.GetByMenteeIdAsync(menteeId);
            if (dto == null) return Result<PairModel>.Failure("No pair found for this mentee.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<PairModel>.Ok(pair) : Result<PairModel>.Failure("Failed to build pair.");
        }

        public async Task<Result<IEnumerable<PairModel>>> GetPairsBySupervisorAsync(int supervisorId)
        {
            var dtos = await _pairRepo.GetBySupervisorIdAsync(supervisorId);
            var pairs = new List<PairModel>();
            foreach (var dto in dtos)
            {
                var pair = await MapDtoToPairAsync(dto);
                if (pair != null) pairs.Add(pair);
            }
            return Result<IEnumerable<PairModel>>.Ok(pairs);
        }

        public async Task<Result> CreatePairAsync(int mentorId, int menteeId)
        {
            if (mentorId == menteeId)
                return Result.Failure("Mentor and mentee cannot be the same person.");

            var mentor = await _userService.GetUserByIdAsync(mentorId);
            if (mentor.Data is not StudentModel { IsMentor: true })
                return Result.Failure("Selected mentor is not a valid mentor.");

            var mentee = await _userService.GetUserByIdAsync(menteeId);
            if (mentee.Data is not StudentModel { IsMentee: true })
                return Result.Failure("Selected mentee is not a valid mentee.");

            var mentorPair = await _pairRepo.GetByMentorIdAsync(mentorId);
            if (mentorPair != null)
                return Result.Failure("The mentor is already in a pair.");

            var menteePair = await _pairRepo.GetByMenteeIdAsync(menteeId);
            if (menteePair != null)
                return Result.Failure("The mentee is already in a pair.");

            int supervisorId = await _supervisorAssignment.GetForMenteeAsync(menteeId);

            bool created = await _pairRepo.CreateAsync(supervisorId, mentorId, menteeId);
            return created ? Result.Ok() : Result.Failure("Failed to create pair.");
        }

        public async Task<Result> SeparatePairAsync(int pairId)
        {
            bool deleted = await _pairRepo.DeleteAsync(pairId);
            return deleted ? Result.Ok() : Result.Failure("Pair not found or could not be deleted.");
        }

        private async Task<PairModel?> MapDtoToPairAsync(PairDao dto)
        {
            var mentorResult = await _userService.GetUserByIdAsync(dto.MentorId);
            var menteeResult = await _userService.GetUserByIdAsync(dto.MenteeId);

            if (mentorResult.Data is not StudentModel mentor) return null;
            if (menteeResult.Data is not StudentModel mentee) return null;

            var supervisorResult = await _userService.GetUserByIdAsync(dto.SupervisorId);
            var supervisor = supervisorResult.Data as SupervisorModel;

            return new PairModel
            {
                Id = dto.Id,
                Mentor = mentor,
                Mentee = mentee,
                Supervisor = supervisor,
                MatchTier = (MatchTier)dto.MatchTier,
                IsProfileIncomplete = dto.IsProfileIncomplete
            };
        }
    }
}
