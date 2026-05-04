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

        private const string SelectCols = "Id, MentorId, MenteeId, SupervisorId, CreatedAt, MatchTier, IsProfileIncomplete";

        public async Task<IEnumerable<PairDao>> GetAllAsync()
        {
            var rows = await _db.QueryAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs");
            return rows.Select(MapToDto).ToList();
        }

        public async Task<PairDao?> GetByIdAsync(int id)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs WHERE Id = @Id",
                new { Id = id });
            return row == null ? null : MapToDto(row);
        }

        public async Task<PairDao?> GetByMentorIdAsync(int mentorId)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs WHERE MentorId = @MentorId",
                new { MentorId = mentorId });
            return row == null ? null : MapToDto(row);
        }

        public async Task<PairDao?> GetByMenteeIdAsync(int menteeId)
        {
            var row = await _db.QuerySingleAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs WHERE MenteeId = @MenteeId",
                new { MenteeId = menteeId });
            return row == null ? null : MapToDto(row);
        }

        public async Task<IEnumerable<PairDao>> GetBySupervisorIdAsync(int supervisorId)
        {
            var rows = await _db.QueryAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs WHERE SupervisorId = @SupervisorId",
                new { SupervisorId = supervisorId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<bool> CreateAsync(int supervisorId, int mentorId, int menteeId)
            => await CreateWithTierAsync(supervisorId, mentorId, menteeId, matchTier: 0, isProfileIncomplete: false);

        public async Task<bool> CreateWithTierAsync(int supervisorId, int mentorId, int menteeId, int matchTier, bool isProfileIncomplete)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO Pairs (MentorId, MenteeId, SupervisorId, CreatedAt, MatchTier, IsProfileIncomplete)
                      VALUES (@MentorId, @MenteeId, @SupervisorId, @CreatedAt, @MatchTier, @IsProfileIncomplete)",
                    new
                    {
                        MentorId = mentorId,
                        MenteeId = menteeId,
                        SupervisorId = supervisorId,
                        CreatedAt = DateTime.UtcNow.ToString("o"),
                        MatchTier = matchTier,
                        IsProfileIncomplete = isProfileIncomplete ? 1 : 0
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

        public async Task<IEnumerable<int>> GetMatchedMentorIdsAsync()
        {
            var rows = await _db.QueryAsync<IdRow>("SELECT MentorId AS Id FROM Pairs");
            return rows.Select(r => r.Id).ToList();
        }

        public async Task<IEnumerable<int>> GetMatchedMenteeIdsAsync()
        {
            var rows = await _db.QueryAsync<IdRow>("SELECT MenteeId AS Id FROM Pairs");
            return rows.Select(r => r.Id).ToList();
        }

        public async Task<IEnumerable<PairDao>> GetProfileIncompleteAsync()
        {
            var rows = await _db.QueryAsync<PairRow>(
                $"SELECT {SelectCols} FROM Pairs WHERE IsProfileIncomplete = 1");
            return rows.Select(MapToDto).ToList();
        }

        // ── Mapping helpers ───────────────────────────────────────────────────

        private static PairDao MapToDto(PairRow row) => new PairDao
        {
            Id = row.Id,
            MentorId = row.MentorId,
            MenteeId = row.MenteeId,
            SupervisorId = row.SupervisorId,
            CreatedAt = row.CreatedAt,
            MatchTier = row.MatchTier,
            IsProfileIncomplete = row.IsProfileIncomplete == 1
        };

        private class PairRow
        {
            public int Id { get; set; }
            public int MentorId { get; set; }
            public int MenteeId { get; set; }
            public int SupervisorId { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
            public int MatchTier { get; set; }
            public int IsProfileIncomplete { get; set; }
        }

        private class IdRow { public int Id { get; set; } }
    }
}
