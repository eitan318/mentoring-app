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

        public Result<IEnumerable<Review>> GetReviewsByPair(int pairId)
        {
            var dtos = _reviewRepo.GetByPair(pairId);
            var reviews = dtos.Select(MapDtoToReview);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public Result<IEnumerable<Review>> GetReviewsByAuthor(int authorUserId)
        {
            var dtos = _reviewRepo.GetByAuthor(authorUserId);
            var reviews = dtos.Select(MapDtoToReview);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public Result CreateReview(string content, int pairId, int authorUserId)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure("Review content cannot be empty.");

            bool created = _reviewRepo.Create(content, DateTime.UtcNow, pairId, authorUserId);
            return created ? Result.Ok() : Result.Failure("Failed to save review.");
        }

        private static Review MapDtoToReview(ReviewDto dto) =>
            new Review(dto.Content, DateTime.Parse(dto.Date))
            {
                Id = dto.Id
            };
    }
}
