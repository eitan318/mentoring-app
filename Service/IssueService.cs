using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;

namespace MentoringApp.Service
{
    public class IssueService
    {
        private readonly IIssueRepo _issueRepo;

        public IssueService(IIssueRepo issueRepo)
        {
            _issueRepo = issueRepo;
        }

        public Result<IEnumerable<Issue>> GetAllIssues()
        {
            var dtos = _issueRepo.GetAll();
            var issues = dtos.Select(dto => MapDtoToIssue(dto)).Where(i => i != null).Cast<Issue>();
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public Result<Issue> GetIssueById(int id)
        {
            var dto = _issueRepo.GetById(id);
            if (dto == null) return Result<Issue>.Failure("Issue not found.");
            var issue = MapDtoToIssue(dto);
            return issue != null ? Result<Issue>.Ok(issue) : Result<Issue>.Failure("Failed to build issue.");
        }

        public Result<IEnumerable<Issue>> GetIssuesByUser(int userId)
        {
            var dtos = _issueRepo.GetByReporter(userId);
            var issues = dtos.Select(dto => MapDtoToIssue(dto)).Where(i => i != null).Cast<Issue>();
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public Result<IEnumerable<IssueCategory>> GetCategories()
        {
            var categoryDtos = _issueRepo.GetCategories();
            var categories = categoryDtos.Select(c => new IssueCategory { Id = c.Id, Name = c.Name });
            return Result<IEnumerable<IssueCategory>>.Ok(categories);
        }

        public Result CreateIssue(string description, int categoryId, int reportedByUserId)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure("Description cannot be empty.");

            bool created = _issueRepo.Create(description, categoryId, reportedByUserId);
            return created ? Result.Ok() : Result.Failure("Failed to create issue.");
        }

        public Result ResolveIssue(int issueId)
        {
            bool resolved = _issueRepo.Resolve(issueId);
            return resolved ? Result.Ok() : Result.Failure("Issue not found or could not be resolved.");
        }

        public Result<IEnumerable<Issue>> GetIssuesBySupervisor(int supervisorId)
        {
            try
            {
                var dtos = _issueRepo.GetBySupervisor(supervisorId);
                if (dtos == null)
                    return Result<IEnumerable<Issue>>.Ok(new List<Issue>());

                var issues = dtos.Select(dto => MapDtoToIssue(dto)).Where(i => i != null).Cast<Issue>();
                return Result<IEnumerable<Issue>>.Ok(issues);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Issue>>.Failure($"Error retrieving supervised issues: {ex.Message}");
            }
        }

        private Issue? MapDtoToIssue(IssueDto dto)
        {
            var categoryDto = _issueRepo.GetCategoryById(dto.CategoryId);
            var category = categoryDto != null
                ? new IssueCategory { Id = categoryDto.Id, Name = categoryDto.Name }
                : new IssueCategory { Id = dto.CategoryId, Name = "Unknown" };

            return new Issue(dto.Description, category, dto.IsResolved != 0)
            {
                Id = dto.Id,
                CreationDate = DateTime.Parse(dto.CreationDate)
            };
        }
    }
}
