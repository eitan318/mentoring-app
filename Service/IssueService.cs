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

        public async Task<Result<IEnumerable<Issue>>> GetAllIssuesAsync()
        {
            var dtos = await _issueRepo.GetAllAsync();
            var issues = await MapDtosToIssuesAsync(dtos);
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public async Task<Result<Issue>> GetIssueByIdAsync(int id)
        {
            var dto = await _issueRepo.GetByIdAsync(id);
            if (dto == null) return Result<Issue>.Failure("Issue not found.");
            var issue = await MapDtoToIssueAsync(dto);
            return issue != null ? Result<Issue>.Ok(issue) : Result<Issue>.Failure("Failed to build issue.");
        }

        public async Task<Result<IEnumerable<Issue>>> GetIssuesByUserAsync(int userId)
        {
            var dtos = await _issueRepo.GetByReporterAsync(userId);
            var issues = await MapDtosToIssuesAsync(dtos);
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public async Task<Result<IEnumerable<IssueCategory>>> GetCategoriesAsync()
        {
            var categoryDtos = await _issueRepo.GetCategoriesAsync();
            var categories = categoryDtos.Select(c => new IssueCategory { Id = c.Id, Name = c.Name });
            return Result<IEnumerable<IssueCategory>>.Ok(categories);
        }

        public async Task<Result> CreateIssueAsync(string description, int categoryId, int reportedByUserId)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure("Description cannot be empty.");

            bool created = await _issueRepo.CreateAsync(description, categoryId, reportedByUserId);
            return created ? Result.Ok() : Result.Failure("Failed to create issue.");
        }

        public async Task<Result> ResolveIssueAsync(int issueId)
        {
            bool resolved = await _issueRepo.ResolveAsync(issueId);
            return resolved ? Result.Ok() : Result.Failure("Issue not found or could not be resolved.");
        }

        public async Task<Result<IEnumerable<Issue>>> GetIssuesBySupervisorAsync(int supervisorId)
        {
            try
            {
                var dtos = await _issueRepo.GetBySupervisorAsync(supervisorId);
                if (dtos == null)
                    return Result<IEnumerable<Issue>>.Ok(new List<Issue>());

                var issues = await MapDtosToIssuesAsync(dtos);
                return Result<IEnumerable<Issue>>.Ok(issues);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Issue>>.Failure($"Error retrieving supervised issues: {ex.Message}");
            }
        }

        private async Task<IEnumerable<Issue>> MapDtosToIssuesAsync(IEnumerable<IssueDto> dtos)
        {
            var tasks = dtos.Select(dto => MapDtoToIssueAsync(dto));
            var results = await Task.WhenAll(tasks);
            return results.Where(i => i != null).Cast<Issue>();
        }

        private async Task<Issue?> MapDtoToIssueAsync(IssueDto dto)
        {
            var categoryDto = await _issueRepo.GetCategoryByIdAsync(dto.CategoryId);
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
