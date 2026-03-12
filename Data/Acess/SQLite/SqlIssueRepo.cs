using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Model;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlIssueRepo : IIssueRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlIssueRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public IEnumerable<Issue> GetAll()
        {
            var rows = _db.Query<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues");
            return rows.Select(r => MapToDomain(r)).ToList();
        }

        public Issue? GetById(int id)
        {
            var row = _db.QuerySingle<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues WHERE Id = @Id",
                new { Id = id });
            return row == null ? null : MapToDomain(row);
        }

        public IEnumerable<Issue> GetByReporter(int userId)
        {
            var rows = _db.Query<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues WHERE ReportedByUserId = @UserId",
                new { UserId = userId });
            return rows.Select(r => MapToDomain(r)).ToList();
        }

        public IEnumerable<Issue> GetBySupervisor(int supervisorId)
        {
            // Get supervised student IDs from Pairs
            var studentRows = _db.Query<StudentIdRow>(
                @"SELECT MentorId AS UserId FROM Pairs WHERE SupervisorId = @SupervisorId
                  UNION
                  SELECT MenteeId AS UserId FROM Pairs WHERE SupervisorId = @SupervisorId",
                new { SupervisorId = supervisorId });

            var studentIds = studentRows.Select(r => r.UserId).Distinct().ToList();
            if (!studentIds.Any())
                return Enumerable.Empty<Issue>();

            // Fetch all issues and filter by those student IDs in memory
            var allIssues = _db.Query<IssueRow>(
                "SELECT Id, Description, CategoryId, ReportedByUserId, IsResolved, CreationDate FROM Issues");

            return allIssues
                .Where(i => studentIds.Contains(i.ReportedByUserId))
                .Select(r => MapToDomain(r))
                .ToList();
        }

        public IEnumerable<IssueCategory> GetCategories()
        {
            var rows = _db.Query<CategoryRow>("SELECT Id, Name FROM IssueCategories");
            return rows.Select(r => new IssueCategory { Id = r.Id, Name = r.Name }).ToList();
        }

        public bool Create(Issue issue, int reportedByUserId)
        {
            try
            {
                _db.Execute(
                    @"INSERT INTO Issues (Description, CategoryId, ReportedByUserId, IsResolved, CreationDate)
                      VALUES (@Description, @CategoryId, @ReportedByUserId, 0, @CreationDate)",
                    new
                    {
                        Description = issue.Description,
                        CategoryId = issue.Category.Id,
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

        public bool Resolve(int issueId)
        {
            try
            {
                int affected = _db.Execute(
                    "UPDATE Issues SET IsResolved = 1 WHERE Id = @Id",
                    new { Id = issueId });
                return affected > 0;
            }
            catch
            {
                return false;
            }
        }

        private IssueCategory LoadCategory(int categoryId)
        {
            var row = _db.QuerySingle<CategoryRow>(
                "SELECT Id, Name FROM IssueCategories WHERE Id = @Id",
                new { Id = categoryId });
            return row != null
                ? new IssueCategory { Id = row.Id, Name = row.Name }
                : new IssueCategory { Id = categoryId, Name = "Unknown" };
        }

        private Issue MapToDomain(IssueRow row) => new Issue
        {
            Id = row.Id,
            Description = row.Description,
            Category = LoadCategory(row.CategoryId),
            IsResolved = row.IsResolved != 0,
            CreationDate = DateTime.Parse(row.CreationDate)
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

        private class CategoryRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class StudentIdRow
        {
            public int UserId { get; set; }
        }
    }
}
