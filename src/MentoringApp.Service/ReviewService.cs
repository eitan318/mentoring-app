using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class ReviewService
    {
        private readonly IReviewRepo _reviewRepo;

        public ReviewService(IReviewRepo reviewRepo)
        {
            _reviewRepo = reviewRepo;
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
            if (amountOfHours <= 0)
                return Result.Failure("Please enter a valid number of meeting hours.");

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
