using MentoringApp.Data.Interfaces;
using MentoringApp.Data.SQLEF.DataObject;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.SQLEF
{
    internal class EFIssueRepo : IIssueRepo
    {
        private readonly MentoringDbContext _context;

        public EFIssueRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IEnumerable<Issue> GetAll()
        {
            return _context.Issues
                .AsNoTracking()
                .Select(i => MapToDomain(i))
                .ToList();
        }

        public Issue? GetById(int id)
        {
            var data = _context.Issues.AsNoTracking().FirstOrDefault(i => i.Id == id);
            return data == null ? null : MapToDomain(data);
        }

        public IEnumerable<Issue> GetBySupervisor(int supervisorId)
        {
            // 1. Get the IDs of all students (Mentors/Mentees) supervised by this person
            var supervisedStudentIds = _context.Pairs
                .Where(p => p.SupervisorId == supervisorId)
                .SelectMany(p => new[] { p.MentorId, p.MenteeId })
                .Distinct()
                .ToList();

            // 2. Fetch issues reported by those specific students
            return _context.Issues
                .Include(i => i.CategoryId)
                .AsEnumerable() // Move to memory for custom mapping
                .Select(i => MapToDomain(i))
                .Where(i => i != null)
                .Cast<Issue>()
                .ToList();
        }
        public IEnumerable<Issue> GetByReporter(int userId)
        {
            return _context.Issues
                .AsNoTracking()
                .Where(i => i.ReportedByUserId == userId)
                .Select(i => MapToDomain(i))
                .ToList();
        }

        public IEnumerable<IssueCategory> GetCategories()
        {
            return _context.IssueCategories
                .AsNoTracking()
                .Select(c => new IssueCategory { Id = c.Id, Name = c.Name })
                .ToList();
        }

        public bool Create(Issue issue, int reportedByUserId)
        {
            try
            {
                _context.Issues.Add(new IssueData
                {
                    Description = issue.Description,
                    CategoryId = issue.Category.Id,
                    ReportedByUserId = reportedByUserId,
                    IsResolved = false,
                    CreationDate = DateTime.UtcNow
                });
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Resolve(int issueId)
        {
            var data = _context.Issues.FirstOrDefault(i => i.Id == issueId);
            if (data == null) return false;

            try
            {
                data.IsResolved = true;
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Issue MapToDomain(IssueData data)
        {
            var category = _context.IssueCategories.AsNoTracking()
                .FirstOrDefault(c => c.Id == data.CategoryId);

            return new Issue
            {
                Id = data.Id,
                Description = data.Description,
                Category = category != null
                    ? new IssueCategory { Id = category.Id, Name = category.Name }
                    : new IssueCategory { Id = data.CategoryId, Name = "Unknown" },
                IsResolved = data.IsResolved,
                CreationDate = data.CreationDate
            };
        }
    }
}
