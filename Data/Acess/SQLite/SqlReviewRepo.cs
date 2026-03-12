using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlReviewRepo : IReviewRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlReviewRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public IEnumerable<Review> GetByPair(int pairId)
        {
            var rows = _db.Query<ReviewRow>(
                "SELECT Id, PairId, AuthorUserId, Content, Date FROM Reviews WHERE PairId = @PairId",
                new { PairId = pairId });

            return rows.Select(MapToDomain).ToList();
        }

        public IEnumerable<Review> GetByAuthor(int authorUserId)
        {
            var rows = _db.Query<ReviewRow>(
                "SELECT Id, PairId, AuthorUserId, Content, Date FROM Reviews WHERE AuthorUserId = @AuthorUserId",
                new { AuthorUserId = authorUserId });

            return rows.Select(MapToDomain).ToList();
        }

        public bool Create(Review review, int pairId, int authorUserId)
        {
            try
            {
                _db.Execute(
                    @"INSERT INTO Reviews (PairId, AuthorUserId, Content, Date)
                      VALUES (@PairId, @AuthorUserId, @Content, @Date)",
                    new
                    {
                        PairId = pairId,
                        AuthorUserId = authorUserId,
                        Content = review.Content,
                        Date = review.Date.ToString("o")
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Review MapToDomain(ReviewRow row)
        {
            return new Review(row.Content, DateTime.Parse(row.Date));
        }

        private class ReviewRow
        {
            public int Id { get; set; }
            public int PairId { get; set; }
            public int AuthorUserId { get; set; }
            public string Content { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
        }
    }
}
