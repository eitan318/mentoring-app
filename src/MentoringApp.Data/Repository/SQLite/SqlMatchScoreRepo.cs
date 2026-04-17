using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlMatchScoreRepo : IMatchScoreRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlMatchScoreRepo(ISQLiteConnectionService db) => _db = db;

        public async Task BulkInsertAsync(IEnumerable<MatchScoreDto> scores)
        {
            foreach (var score in scores)
            {
                await _db.ExecuteAsync(
                    "INSERT INTO MatchScores (MenteeId, MentorId, ScorePercent) VALUES (@MenteeId, @MentorId, @ScorePercent)",
                    new { score.MenteeId, score.MentorId, score.ScorePercent });
            }
        }

        public async Task<IEnumerable<MatchScoreDto>> GetTopForMenteeAsync(int menteeId, int limit = 3)
        {
            var rows = await _db.QueryAsync<ScoreRow>(
                "SELECT Id, MenteeId, MentorId, ScorePercent FROM MatchScores WHERE MenteeId = @MenteeId ORDER BY ScorePercent DESC LIMIT @Limit",
                new { MenteeId = menteeId, Limit = limit });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<MatchScoreDto>> GetAllAsync()
        {
            var rows = await _db.QueryAsync<ScoreRow>("SELECT Id, MenteeId, MentorId, ScorePercent FROM MatchScores ORDER BY ScorePercent DESC");
            return rows.Select(MapToDto).ToList();
        }

        public async Task ClearAllAsync()
        {
            await _db.ExecuteAsync("DELETE FROM MatchScores", null);
        }

        private static MatchScoreDto MapToDto(ScoreRow row) => new MatchScoreDto
        {
            Id = row.Id,
            MenteeId = row.MenteeId,
            MentorId = row.MentorId,
            ScorePercent = row.ScorePercent
        };

        private class ScoreRow
        {
            public int Id { get; set; }
            public int MenteeId { get; set; }
            public int MentorId { get; set; }
            public double ScorePercent { get; set; }
        }
    }
}
