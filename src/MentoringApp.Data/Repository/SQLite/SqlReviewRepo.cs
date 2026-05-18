using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlReviewRepo : IReviewRepo
    {
        private readonly ISQLiteConnectionService _db;
        private const string SelectCols = "Id, PairId, AuthorUserId, Content, Date, AmountOfHours";

        public SqlReviewRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ReviewDao>> GetByPairAsync(int pairId)
        {
            var rows = await _db.QueryAsync<ReviewRow>(
                $"SELECT {SelectCols} FROM Reviews WHERE PairId = @PairId",
                new { PairId = pairId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ReviewDao>> GetByAuthorAsync(int authorUserId)
        {
            var rows = await _db.QueryAsync<ReviewRow>(
                $"SELECT {SelectCols} FROM Reviews WHERE AuthorUserId = @AuthorUserId",
                new { AuthorUserId = authorUserId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<bool> CreateAsync(string content, DateTime date, int pairId, int authorUserId, double amountOfHours)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO Reviews (PairId, AuthorUserId, Content, Date, AmountOfHours)
                      VALUES (@PairId, @AuthorUserId, @Content, @Date, @AmountOfHours)",
                    new
                    {
                        PairId = pairId,
                        AuthorUserId = authorUserId,
                        Content = content,
                        Date = date.ToString("o"),
                        AmountOfHours = amountOfHours
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ReviewDao MapToDto(ReviewRow row) => new ReviewDao
        {
            Id = row.Id,
            PairId = row.PairId,
            AuthorUserId = row.AuthorUserId,
            Content = row.Content,
            Date = row.Date,
            AmountOfHours = row.AmountOfHours
        };

        private class ReviewRow
        {
            public int Id { get; set; }
            public int PairId { get; set; }
            public int AuthorUserId { get; set; }
            public string Content { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public double AmountOfHours { get; set; }
        }
    }
}
