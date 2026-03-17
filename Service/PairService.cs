using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class PairService
    {
        private readonly IPairRepo _pairRepo;
        private readonly UserService _userService;

        public PairService(IPairRepo pairRepo, UserService userService)
        {
            _pairRepo = pairRepo;
            _userService = userService;
        }

        public async Task<Result<IEnumerable<Pair>>> GetAllPairsAsync()
        {
            var dtos = await _pairRepo.GetAllAsync();
            var pairs = new List<Pair>();
            foreach (var dto in dtos)
            {
                var pair = await MapDtoToPairAsync(dto);
                if (pair != null) pairs.Add(pair);
            }
            return Result<IEnumerable<Pair>>.Ok(pairs);
        }

        public async Task<Result<Pair>> GetPairById(int id)
        {
            var dto = await _pairRepo.GetByIdAsync(id);
            if (dto == null) return Result<Pair>.Failure("Pair not found.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<Pair>.Ok(pair) : Result<Pair>.Failure("Failed to build pair.");
        }

        public async Task<Result<Pair>> GetPairByMentorAsync(int mentorId)
        {
            var dto = await _pairRepo.GetByMentorIdAsync(mentorId);
            if (dto == null) return Result<Pair>.Failure("No pair found for this mentor.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<Pair>.Ok(pair) : Result<Pair>.Failure("Failed to build pair.");
        }

        public async Task<Result<Pair>> GetPairByMenteeAsync(int menteeId)
        {
            var dto = await _pairRepo.GetByMenteeIdAsync(menteeId);
            if (dto == null) return Result<Pair>.Failure("No pair found for this mentee.");
            var pair = await MapDtoToPairAsync(dto);
            return pair != null ? Result<Pair>.Ok(pair) : Result<Pair>.Failure("Failed to build pair.");
        }

        public async Task<Result<IEnumerable<Pair>>> GetPairsBySupervisorAsync(int supervisorId)
        {
            var dtos = await _pairRepo.GetBySupervisorIdAsync(supervisorId);
            var pairs = new List<Pair>();
            foreach (var dto in dtos)
            {
                var pair = await MapDtoToPairAsync(dto);
                if (pair != null) pairs.Add(pair);
            }
            return Result<IEnumerable<Pair>>.Ok(pairs);
        }

        public async Task<Result> CreatePairAsync(int supervisorId, int mentorId, int menteeId)
        {
            // Verify users exist and have correct roles
            var supervisor = await _userService.GetUserByIdAsync(supervisorId);
            if (supervisor.Data is not Supervisor)
                return Result.Failure("Selected supervisor is not valid.");

            var mentor = await _userService.GetUserByIdAsync(mentorId);
            if (mentor.Data is not Student { IsMentor: true })
                return Result.Failure("Selected mentor is not a valid mentor.");

            var mentee = await _userService.GetUserByIdAsync(menteeId);
            if (mentee.Data is not Student { IsMentee: true })
                return Result.Failure("Selected mentee is not a valid mentee.");

            bool created = await _pairRepo.CreateAsync(supervisorId, mentorId, menteeId);
            return created ? Result.Ok() : Result.Failure("Failed to create pair.");
        }

        public Result SeparatePair(int pairId)
        {
            bool deleted = _pairRepo.Delete(pairId);
            return deleted ? Result.Ok() : Result.Failure("Pair not found or could not be deleted.");
        }

        private async Task<Pair?> MapDtoToPairAsync(PairDto dto)
        {
            var mentorResult = await _userService.GetUserByIdAsync(dto.MentorId);
            var menteeResult = await _userService.GetUserByIdAsync(dto.MenteeId);

            if (mentorResult.Data is not Student mentor) return null;
            if (menteeResult.Data is not Student mentee) return null;

            return new Pair
            {
                Id = dto.Id,
                Mentor = mentor,
                Mentee = mentee
            };
        }
    }
}
