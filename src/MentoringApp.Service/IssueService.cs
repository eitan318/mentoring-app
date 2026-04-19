using DocumentFormat.OpenXml.Bibliography;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Service.Mapping;

namespace MentoringApp.Service
{
    /// <summary>
    /// Issue CRUD and query operations.
    /// Two mapping paths exist: <c>IssueMapper</c>/<c>IssueCategoryMapper</c> for batch conversion,
    /// and per-item async mapping for single-issue lookups.
    /// All write operations return <see cref="Result"/> so callers can surface errors without exceptions.
    /// </summary>
    public class IssueService
    {
        private readonly IIssueRepo _issueRepo;
        private readonly IIssueCategoryRepo _issueCategoryRepo;
        private readonly NotificationService _notificationService;

        public IssueService(IIssueRepo issueRepo, IIssueCategoryRepo issueCategoryRepo, NotificationService notificationService)
        {
            _issueRepo = issueRepo;
            _issueCategoryRepo = issueCategoryRepo;
            _notificationService = notificationService;
        }

        public async Task<Result<IEnumerable<IssueModel>>> GetAllIssuesAsync()
        {
            var dtos = await _issueRepo.GetAllAsync();
            var issues = await MapDtosToIssuesAsync(dtos);
            return Result<IEnumerable<IssueModel>>.Ok(issues);
        }

        public async Task<Result<IssueModel>> GetIssueByIdAsync(int id)
        {
            var dto = await _issueRepo.GetByIdAsync(id);
            if (dto == null) return Result<IssueModel>.Failure("Issue not found.");
            var issue = await MapDtoToIssueAsync(dto);
            return issue != null ? Result<IssueModel>.Ok(issue) : Result<IssueModel>.Failure("Failed to build issue.");
        }

        public async Task<Result<IEnumerable<IssueModel>>> GetIssuesByUserAsync(int userId)
        {
            var dtos = await _issueRepo.GetByReporterAsync(userId);
            var categoryDtos = await _issueCategoryRepo.GetAllAsync();
            var categories = IssueCategoryMapper.ToModels(categoryDtos).ToList();
            var models = IssueMapper.ToModels(dtos, categories).ToList(); // materialize here

            return Result<IEnumerable<IssueModel>>.Ok(models);
        }

        public async Task<Result<IEnumerable<IssueCategoryModel>>> GetCategoriesAsync()
        {
            var categoryDtos = await _issueCategoryRepo.GetAllAsync();
            var categories = categoryDtos.Select(c => new IssueCategoryModel { Id = c.Id, Name = c.Name });
            return Result<IEnumerable<IssueCategoryModel>>.Ok(categories);
        }

        public async Task<Result> CreateIssueAsync(string description, int categoryId, int reportedByUserId)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure("Description cannot be empty.");

            bool created = await _issueRepo.CreateAsync(description, categoryId, reportedByUserId);
            if (!created) return Result.Failure("Failed to create issue.");

            _ = _notificationService.NotifyIssueCreatedAsync(reportedByUserId, description);
            return Result.Ok();
        }

        public async Task<Result> ResolveIssueAsync(int issueId)
        {
            bool resolved = await _issueRepo.ResolveAsync(issueId);
            return resolved ? Result.Ok() : Result.Failure("Issue not found or could not be resolved.");
        }

        public async Task<Result> ForwardIssueAsync(int issueId, int supervisorId)
        {
            bool forwarded = await _issueRepo.ForwardAsync(issueId, supervisorId);
            if (!forwarded) return Result.Failure("Issue not found or could not be forwarded.");

            var issueResult = await GetIssueByIdAsync(issueId);
            if (issueResult.Success && issueResult.Data != null)
                _ = _notificationService.NotifyIssueForwardedToAdminAsync(issueId, supervisorId, issueResult.Data.Description);

            return Result.Ok();
        }

        public async Task<Result<IEnumerable<IssueModel>>> GetForwardedIssuesAsync()
        {
            var dtos = await _issueRepo.GetForwardedAsync();
            var issues = await MapDtosToIssuesAsync(dtos);
            return Result<IEnumerable<IssueModel>>.Ok(issues);
        }

        public async Task<Result<IEnumerable<IssueModel>>> GetIssuesBySupervisorAsync(int supervisorId)
        {
            try
            {
                var dtos = await _issueRepo.GetBySupervisorAsync(supervisorId);
                if (dtos == null)
                    return Result<IEnumerable<IssueModel>>.Ok(new List<IssueModel>());

                var issues = await MapDtosToIssuesAsync(dtos);
                return Result<IEnumerable<IssueModel>>.Ok(issues);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<IssueModel>>.Failure($"Error retrieving supervised issues: {ex.Message}");
            }
        }
        private async Task<IEnumerable<IssueModel>> MapDtosToIssuesAsync(IEnumerable<IssueDao> dtos)
        {
            var categoryDtos = await _issueCategoryRepo.GetAllAsync();
            var categories = IssueCategoryMapper.ToModels(categoryDtos).ToList();
            return IssueMapper.ToModels(dtos, categories).ToList(); // materialize — prevents lazy eval on UI thread
        }

        private async Task<IssueModel?> MapDtoToIssueAsync(IssueDao dto)
        {
            var categoryDto = await _issueCategoryRepo.GetByIdAsync(dto.CategoryId);
            var category = IssueCategoryMapper.ToModel(categoryDto);
            return IssueMapper.ToModel(dto, category);
        }


    }
}
