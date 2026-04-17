using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlPairRequestRepo : IPairRequestRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlPairRequestRepo(ISQLiteConnectionService db) => _db = db;

        public async Task<bool> CreateAsync(int menteeId, int mentorId, int tier)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO PairRequests (MenteeId, MentorId, Status, Tier, CreatedAt)
                      VALUES (@MenteeId, @MentorId, 'Pending', @Tier, @CreatedAt)",
                    new { MenteeId = menteeId, MentorId = mentorId, Tier = tier, CreatedAt = DateTime.UtcNow.ToString("o") });
                return true;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<PairRequestDto>> GetByMentorAsync(int mentorId)
        {
            var rows = await _db.QueryAsync<RequestRow>(
                "SELECT Id, MenteeId, MentorId, Status, Tier, CreatedAt FROM PairRequests WHERE MentorId = @MentorId AND Status = 'Pending'",
                new { MentorId = mentorId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<PairRequestDto>> GetByMenteeAsync(int menteeId)
        {
            var rows = await _db.QueryAsync<RequestRow>(
                "SELECT Id, MenteeId, MentorId, Status, Tier, CreatedAt FROM PairRequests WHERE MenteeId = @MenteeId",
                new { MenteeId = menteeId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<bool> UpdateStatusAsync(int requestId, string status)
        {
            int rows = await _db.ExecuteAsync(
                "UPDATE PairRequests SET Status = @Status WHERE Id = @Id",
                new { Status = status, Id = requestId });
            return rows > 0;
        }

        public async Task CancelPendingForUsersAsync(int menteeId, int mentorId)
        {
            await _db.ExecuteAsync(
                @"UPDATE PairRequests SET Status = 'Rejected'
                  WHERE Status = 'Pending' AND (MenteeId = @MenteeId OR MentorId = @MentorId)",
                new { MenteeId = menteeId, MentorId = mentorId });
        }

        public async Task<bool> ExistsAsync(int menteeId, int mentorId)
        {
            var row = await _db.QuerySingleAsync<CountRow>(
                "SELECT COUNT(1) AS Count FROM PairRequests WHERE MenteeId = @MenteeId AND MentorId = @MentorId AND Status = 'Pending'",
                new { MenteeId = menteeId, MentorId = mentorId });
            return row != null && row.Count > 0;
        }

        private static PairRequestDto MapToDto(RequestRow row) => new PairRequestDto
        {
            Id = row.Id,
            MenteeId = row.MenteeId,
            MentorId = row.MentorId,
            Status = row.Status,
            Tier = row.Tier,
            CreatedAt = row.CreatedAt
        };

        private class RequestRow
        {
            public int Id { get; set; }
            public int MenteeId { get; set; }
            public int MentorId { get; set; }
            public string Status { get; set; } = string.Empty;
            public int Tier { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
        }

        private class CountRow { public int Count { get; set; } }
    }
}
