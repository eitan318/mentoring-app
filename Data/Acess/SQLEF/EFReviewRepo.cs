using MentoringApp.Data.Access.SQLEF.DataObject;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.Access.SQLEF
{
    internal class EFReviewRepo : IReviewRepo
    {
        private readonly MentoringDbContext _context;

        public EFReviewRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<Review> GetByPair(int pairId)
        {
            return _context.Reviews
                .AsNoTracking()
                .Where(r => r.PairId == pairId)
                .Select(r => MapToDomain(r))
                .ToList();
        }

        public IEnumerable<Review> GetByAuthor(int authorUserId)
        {
            return _context.Reviews
                .AsNoTracking()
                .Where(r => r.AuthorUserId == authorUserId)
                .Select(r => MapToDomain(r))
                .ToList();
        }

        public bool Create(Review review, int pairId, int authorUserId)
        {
            try
            {
                _context.Reviews.Add(new ReviewData
                {
                    PairId = pairId,
                    AuthorUserId = authorUserId,
                    Content = review.Content,
                    Date = review.Date
                });
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Review MapToDomain(ReviewData data)
        {
            return new Review(data.Content, data.Date);
        }
    }
}
