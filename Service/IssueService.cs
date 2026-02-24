using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using System.Collections.Generic;

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
            var issues = _issueRepo.GetAll();
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public Result<Issue> GetIssueById(int id)
        {
            var issue = _issueRepo.GetById(id);
            return issue != null
                ? Result<Issue>.Ok(issue)
                : Result<Issue>.Failure("Issue not found.");
        }

        public Result<IEnumerable<Issue>> GetIssuesByUser(int userId)
        {
            var issues = _issueRepo.GetByReporter(userId);
            return Result<IEnumerable<Issue>>.Ok(issues);
        }

        public Result<IEnumerable<IssueCategory>> GetCategories()
        {
            var categories = _issueRepo.GetCategories();
            return Result<IEnumerable<IssueCategory>>.Ok(categories);
        }

        public Result CreateIssue(string description, IssueCategory category, int reportedByUserId)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure("Description cannot be empty.");

            var issue = new Issue(description, category, false);
            bool created = _issueRepo.Create(issue, reportedByUserId);
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
                var issues = _issueRepo.GetBySupervisor(supervisorId);

                if (issues == null)
                    return Result<IEnumerable<Issue>>.Ok(new List<Issue>());

                return Result<IEnumerable<Issue>>.Ok(issues);
            }
            catch (Exception ex)
            {
                // Log exception here if you have a logger
                return Result< IEnumerable <Issue>>.Failure($"Error retrieving supervised issues: {ex.Message}");
            }
        }
    }
}
