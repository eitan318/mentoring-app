using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    /// <summary>
    /// Manages session reviews written by mentors and mentees.
    /// Validates that only pair members may create reviews and that hours fall within 0–24.
    /// </summary>
    public class ReviewService
    {
        private readonly IReviewRepo _reviewRepo;
        private readonly IPairRepo _pairRepo;

        public ReviewService(IReviewRepo reviewRepo, IPairRepo pairRepo)
        {
            _reviewRepo = reviewRepo;
            _pairRepo = pairRepo;
        }

        public async Task<Result<IEnumerable<Review>>> GetReviewsByPairAsync(int pairId)
        {
            var dtos = await _reviewRepo.GetByPairAsync(pairId);
            var reviews = dtos.Select(MapDtoToReview);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public async Task<Result<IEnumerable<Review>>> GetReviewsByAuthorAsync(int authorUserId)
        {
            var dtos = await _reviewRepo.GetByAuthorAsync(authorUserId);
            var reviews = dtos.Select(MapDtoToReview);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public async Task<Result> CreateReviewAsync(string content, int pairId, int authorUserId, double amountOfHours)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure("Review content cannot be empty.");
            if (amountOfHours <= 0 || amountOfHours > 24)
                return Result.Failure("Please enter a valid number of meeting hours (between 0 and 24).");

            var pair = await _pairRepo.GetByIdAsync(pairId);
            if (pair == null)
                return Result.Failure("Pair not found.");

            if (pair.MentorId != authorUserId && pair.MenteeId != authorUserId)
                return Result.Failure("Only the mentor or mentee of this pair can create a review.");

            bool created = await _reviewRepo.CreateAsync(content, DateTime.UtcNow, pairId, authorUserId, amountOfHours);
            return created ? Result.Ok() : Result.Failure("Failed to save review.");
        }

        private static Review MapDtoToReview(ReviewDao dto) =>
            new Review(dto.Content, DateTime.Parse(dto.Date), dto.AmountOfHours)
            {
                Id = dto.Id
            };
    }
}
