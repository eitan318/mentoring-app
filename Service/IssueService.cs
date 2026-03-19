using DocumentFormat.OpenXml.Bibliography;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Service.Mapping;

namespace MentoringApp.Service
{
    public class IssueService
    {
        private readonly IIssueRepo _issueRepo;
        private readonly IIssueCategoryRepo _issueCategoryRepo;

        public IssueService(IIssueRepo issueRepo, IIssueCategoryRepo issueCategoryRepo)
        {
            _issueRepo = issueRepo;
            _issueCategoryRepo = issueCategoryRepo;
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
            var categories = await _issueCategoryRepo.GetAllAsync();
            var models = IssueMapper.ToModels(dtos, IssueCategoryMapper.ToModels(categories));

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
            return created ? Result.Ok() : Result.Failure("Failed to create issue.");
        }

        public async Task<Result> ResolveIssueAsync(int issueId)
        {
            bool resolved = await _issueRepo.ResolveAsync(issueId);
            return resolved ? Result.Ok() : Result.Failure("Issue not found or could not be resolved.");
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
        private async Task<IEnumerable<IssueModel>> MapDtosToIssuesAsync(IEnumerable<IssueDto> dtos)
        {
            var categoryDtos = await _issueCategoryRepo.GetAllAsync();
            var categorys = IssueCategoryMapper.ToModels(categoryDtos);
            return IssueMapper.ToModels(dtos, categorys);
        }

        private async Task<IssueModel?> MapDtoToIssueAsync(IssueDto dto)
        {
            var categoryDto = await _issueCategoryRepo.GetByIdAsync(dto.CategoryId);
            var category = IssueCategoryMapper.ToModel(categoryDto);
            return IssueMapper.ToModel(dto, category);
        }


    }
}
