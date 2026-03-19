using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlReviewRepo : IReviewRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlReviewRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ReviewDto>> GetByPairAsync(int pairId)
        {
            var rows = await _db.QueryAsync<ReviewRow>(
                "SELECT Id, PairId, AuthorUserId, Content, Date FROM Reviews WHERE PairId = @PairId",
                new { PairId = pairId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ReviewDto>> GetByAuthorAsync(int authorUserId)
        {
            var rows = await _db.QueryAsync<ReviewRow>(
                "SELECT Id, PairId, AuthorUserId, Content, Date FROM Reviews WHERE AuthorUserId = @AuthorUserId",
                new { AuthorUserId = authorUserId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<bool> CreateAsync(string content, DateTime date, int pairId, int authorUserId)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO Reviews (PairId, AuthorUserId, Content, Date)
                      VALUES (@PairId, @AuthorUserId, @Content, @Date)",
                    new
                    {
                        PairId = pairId,
                        AuthorUserId = authorUserId,
                        Content = content,
                        Date = date.ToString("o")
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ReviewDto MapToDto(ReviewRow row) => new ReviewDto
        {
            Id = row.Id,
            PairId = row.PairId,
            AuthorUserId = row.AuthorUserId,
            Content = row.Content,
            Date = row.Date
        };

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
