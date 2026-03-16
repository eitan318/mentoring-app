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
            var pairs = await _pairRepo.GetAllAsync();
            return Result<IEnumerable<Pair>>.Ok(pairs);
        }

        public async Task<Result<Pair>> GetPairById(int id)
        {
            var pair = _pairRepo.GetById(id);
            return pair != null
                ? Result<Pair>.Ok(pair)
                : Result<Pair>.Failure("Pair not found.");
        }

        public Result<Pair> GetPairByMentor(int mentorId)
        {
            var pair = _pairRepo.GetByMentorIdAsync(mentorId).Result;
            return pair != null
                ? Result<Pair>.Ok(pair)
                : Result<Pair>.Failure("No pair found for this mentor.");
        }

        public Result<Pair> GetPairByMentee(int menteeId)
        {
            var pair = _pairRepo.GetByMenteeIdAsync(menteeId).Result;
            return pair != null
                ? Result<Pair>.Ok(pair)
                : Result<Pair>.Failure("No pair found for this mentee.");
        }

        public Result<IEnumerable<Pair>> GetPairsBySupervisor(int supervisorId)
        {
            var pairs = _pairRepo.GetBySupervisorId(supervisorId);
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

            var pair = new Pair
            {
                Id = -1,
                Mentor = (Student)mentor.Data,
                Mentee = (Student)mentee.Data
            };

            bool created = await _pairRepo.CreateAsync(pair, supervisorId, mentorId, menteeId);
            return created ? Result.Ok() : Result.Failure("Failed to create pair.");
        }

        public Result SeparatePair(int pairId)
        {
            bool deleted = _pairRepo.Delete(pairId);
            return deleted ? Result.Ok() : Result.Failure("Pair not found or could not be deleted.");
        }
    }
}
