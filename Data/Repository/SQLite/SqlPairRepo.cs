using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlPairRepo : IPairRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlPairRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PairDto>> GetAllAsync()
        {
            var rows = await _db.QueryAsync<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs");
            return rows.Select(MapToDto).ToList();
        }

        public async Task<PairDto?> GetByIdAsync(int id)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE Id = @Id",
                new { Id = id });
            return row == null ? null : MapToDto(row);
        }

        public async Task<PairDto?> GetByMentorIdAsync(int mentorId)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE MentorId = @MentorId",
                new { MentorId = mentorId });
            return row == null ? null : MapToDto(row);
        }

        public async Task<PairDto?> GetByMenteeIdAsync(int menteeId)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE MenteeId = @MenteeId",
                new { MenteeId = menteeId });
            return row == null ? null : MapToDto(row);
        }

        public async Task<IEnumerable<PairDto>> GetBySupervisorIdAsync(int supervisorId)
        {
            var rows = await _db.QueryAsync<PairRow>(
                "SELECT Id, MentorId, MenteeId, SupervisorId, CreatedAt FROM Pairs WHERE SupervisorId = @SupervisorId",
                new { SupervisorId = supervisorId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<bool> CreateAsync(int supervisorId, int mentorId, int menteeId)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO Pairs (MentorId, MenteeId, SupervisorId, CreatedAt)
                      VALUES (@MentorId, @MenteeId, @SupervisorId, @CreatedAt)",
                    new
                    {
                        MentorId = mentorId,
                        MenteeId = menteeId,
                        SupervisorId = supervisorId,
                        CreatedAt = DateTime.UtcNow.ToString("o")
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int pairId)
        {
            try
            {
                int affected = await _db.ExecuteAsync(
                    "DELETE FROM Pairs WHERE Id = @Id",
                    new { Id = pairId });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        private static PairDto MapToDto(PairRow row) => new PairDto
        {
            Id = row.Id,
            MentorId = row.MentorId,
            MenteeId = row.MenteeId,
            SupervisorId = row.SupervisorId,
            CreatedAt = row.CreatedAt
        };

        private class PairRow
        {
            public int Id { get; set; }
            public int MentorId { get; set; }
            public int MenteeId { get; set; }
            public int SupervisorId { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
        }
    }
}
