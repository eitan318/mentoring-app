using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlIssueRepo : IIssueRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlIssueRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public async Task<IEnumerable<IssueDto>> GetAllAsync()
        {
            var rows = await _db.QueryAsync<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues");
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IssueDto?> GetByIdAsync(int id)
        {
            var row = await _db.QuerySingleAsync<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues WHERE Id = @Id",
                new { Id = id });
            return row == null ? null : MapToDto(row);
        }

        public async Task<IEnumerable<IssueDto>> GetByReporterAsync(int userId)
        {
            var rows = await _db.QueryAsync<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues WHERE ReportedByUserId = @UserId",
                new { UserId = userId });
            return rows.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<IssueDto>> GetBySupervisorAsync(int supervisorId)
        {
            var studentRows = await _db.QueryAsync<StudentIdRow>(
                @"SELECT MentorId AS UserId FROM Pairs WHERE SupervisorId = @SupervisorId
                  UNION
                  SELECT MenteeId AS UserId FROM Pairs WHERE SupervisorId = @SupervisorId",
                new { SupervisorId = supervisorId });

            var studentIds = studentRows.Select(r => r.UserId).Distinct().ToList();
            if (!studentIds.Any())
                return Enumerable.Empty<IssueDto>();

            var allIssues = await _db.QueryAsync<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues");

            return allIssues
                .Where(i => studentIds.Contains(i.ReportedByUserId))
                .Select(MapToDto)
                .ToList();
        }



        public async Task<bool> CreateAsync(string description, int categoryId, int reportedByUserId)
        {
            try
            {
                await _db.ExecuteAsync(
                    @"INSERT INTO Issues (Description, CategoryId, ReportedByUserId, IsResolved, CreationDate)
                      VALUES (@Description, @CategoryId, @ReportedByUserId, 0, @CreationDate)",
                    new
                    {
                        Description = description,
                        CategoryId = categoryId,
                        ReportedByUserId = reportedByUserId,
                        CreationDate = DateTime.UtcNow.ToString("o")
                    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResolveAsync(int issueId)
        {
            try
            {
                int affected = await _db.ExecuteAsync(
                    "UPDATE Issues SET IsResolved = 1 WHERE Id = @Id",
                    new { Id = issueId });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        private static IssueDto MapToDto(IssueRow row) => new IssueDto
        {
            Id = row.Id,
            Description = row.Description,
            CategoryId = row.CategoryId,
            ReportedByUserId = row.ReportedByUserId,
            IsResolved = row.IsResolved,
            CreationDate = row.CreationDate
        };

        private class IssueRow
        {
            public int Id { get; set; }
            public string Description { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public int ReportedByUserId { get; set; }
            public int IsResolved { get; set; }
            public string CreationDate { get; set; } = string.Empty;
        }



        private class StudentIdRow
        {
            public int UserId { get; set; }
        }
    }
}
