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
            var reviews = _reviewRepo.GetByPair(pairId);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public Result<IEnumerable<Review>> GetReviewsByAuthor(int authorUserId)
        {
            var reviews = _reviewRepo.GetByAuthor(authorUserId);
            return Result<IEnumerable<Review>>.Ok(reviews);
        }

        public Result CreateReview(string content, int pairId, int authorUserId)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure("Review content cannot be empty.");

            var review = new Review(content, DateTime.UtcNow);
            bool created = _reviewRepo.Create(review, pairId, authorUserId);
            return created ? Result.Ok() : Result.Failure("Failed to save review.");
        }
    }
}
