using MentoringApp.Data.Interfaces;
using MentoringApp.Model.User;

namespace MentoringApp.Service
{
    /// <summary>
    /// Sends transactional email notifications for phase transitions and issue events.
    /// Every public method is wrapped in a top-level try/catch so it can never throw —
    /// callers that use fire-and-forget are safe, and callers that await get a bool result
    /// they can use to show a UI warning.
    /// </summary>
    public class NotificationService
    {
        private readonly EmailService _emailService;
        private readonly UserService _userService;
        private readonly IPairRepo _pairRepo;

        public NotificationService(EmailService emailService, UserService userService, IPairRepo pairRepo)
        {
            _emailService = emailService;
            _userService = userService;
            _pairRepo = pairRepo;
        }

        // ── Phase notifications ───────────────────────────────────────────────

        /// <summary>
        /// Sent to every registered user when the admin opens the info-filing phase.
        /// Returns false if any email could not be sent.
        /// </summary>
        public async Task<bool> SendPhase1StartedAsync()
        {
            try
            {
                var all = await _userService.GetAllUsersAsync();
                var results = await Task.WhenAll(all.Select(u => _emailService.SendEmailAsync(
                    u.Email,
                    "Mentoring App — Please Fill Your Profile",
                    BuildPhase1Body(u.UserName))));
                return results.All(r => r);
            }
            catch { return false; }
        }

        /// <summary>
        /// Sent when the admin starts the matching/selection phase.
        /// Students are asked to log in for pairing; supervisors are asked to monitor.
        /// Returns false if any email could not be sent.
        /// </summary>
        public async Task<bool> SendPhase2StartedAsync()
        {
            try
            {
                var all = (await _userService.GetAllUsersAsync()).ToList();

                var studentTasks = all.OfType<StudentModel>().Select(s => _emailService.SendEmailAsync(
                    s.Email,
                    "Mentoring App — Matching Phase Has Begun",
                    BuildPhase2StudentBody(s.UserName)));

                var supervisorTasks = all.OfType<SupervisorModel>().Select(sv => _emailService.SendEmailAsync(
                    sv.Email,
                    "Mentoring App — Please Supervise Student Matching",
                    BuildPhase2SupervisorBody(sv.UserName)));

                var results = await Task.WhenAll(studentTasks.Concat(supervisorTasks));
                return results.All(r => r);
            }
            catch { return false; }
        }

        // ── Issue notifications ───────────────────────────────────────────────

        /// <summary>
        /// Notifies the supervisor responsible for the reporting student's pair
        /// when a new issue is created. Silently skips if the student is not yet paired.
        /// </summary>
        public async Task NotifyIssueCreatedAsync(int reporterUserId, string issueDescription)
        {
            try
            {
                var pair = await _pairRepo.GetByMentorIdAsync(reporterUserId)
                        ?? await _pairRepo.GetByMenteeIdAsync(reporterUserId);

                if (pair == null) return;

                var supervisorResult = await _userService.GetUserByIdAsync(pair.SupervisorId);
                if (!supervisorResult.Success || supervisorResult.Data == null) return;

                var supervisor = supervisorResult.Data;
                await _emailService.SendEmailAsync(
                    supervisor.Email,
                    "Mentoring App — New Issue Reported",
                    BuildIssueCreatedBody(supervisor.UserName, reporterUserId, issueDescription));
            }
            catch { }
        }

        /// <summary>
        /// Notifies all admin users when a supervisor forwards an issue to admin.
        /// </summary>
        public async Task NotifyIssueForwardedToAdminAsync(int issueId, int forwardedBySupervisorId, string issueDescription)
        {
            try
            {
                var supervisorResult = await _userService.GetUserByIdAsync(forwardedBySupervisorId);
                string supervisorName = supervisorResult.Success && supervisorResult.Data != null
                    ? supervisorResult.Data.UserName
                    : $"Supervisor #{forwardedBySupervisorId}";

                var all = await _userService.GetAllUsersAsync();
                await Task.WhenAll(all.OfType<AdminModel>().Select(a => _emailService.SendEmailAsync(
                    a.Email,
                    "Mentoring App — Issue Forwarded to Admin",
                    BuildIssueForwardedBody(a.UserName, supervisorName, issueId, issueDescription))));
            }
            catch { }
        }

        // ── Email bodies ──────────────────────────────────────────────────────

        private static string BuildPhase1Body(string userName) => $"""
            <p>Hello <strong>{userName}</strong>,</p>
            <p>The administrator has opened the <strong>profile info-filing phase</strong> of the mentoring program.</p>
            <p>Please log in to the Mentoring App and complete your profile information so that the matching process can begin.</p>
            <p>Thank you,<br/>Mentoring App</p>
            """;

        private static string BuildPhase2StudentBody(string userName) => $"""
            <p>Hello <strong>{userName}</strong>,</p>
            <p>The <strong>matching/selection phase</strong> has begun!</p>
            <p>Please log in to the Mentoring App to browse available mentors and submit your preferences.</p>
            <p>Thank you,<br/>Mentoring App</p>
            """;

        private static string BuildPhase2SupervisorBody(string userName) => $"""
            <p>Hello <strong>{userName}</strong>,</p>
            <p>The <strong>matching/selection phase</strong> has started for your students.</p>
            <p>Please log in to the Mentoring App and ensure that all students assigned to you complete their pairing selections in time.</p>
            <p>Thank you,<br/>Mentoring App</p>
            """;

        private static string BuildIssueCreatedBody(string supervisorName, int reporterUserId, string description) => $"""
            <p>Hello <strong>{supervisorName}</strong>,</p>
            <p>A new issue has been reported by one of your assigned students (User ID: {reporterUserId}).</p>
            <p><strong>Issue:</strong> {System.Net.WebUtility.HtmlEncode(description)}</p>
            <p>Please log in to the Mentoring App to review and address this issue.</p>
            <p>Thank you,<br/>Mentoring App</p>
            """;

        private static string BuildIssueForwardedBody(string adminName, string supervisorName, int issueId, string description) => $"""
            <p>Hello <strong>{adminName}</strong>,</p>
            <p>Supervisor <strong>{supervisorName}</strong> has forwarded an issue to you for review.</p>
            <p><strong>Issue #{issueId}:</strong> {System.Net.WebUtility.HtmlEncode(description)}</p>
            <p>Please log in to the Mentoring App to review the forwarded issues in your admin dashboard.</p>
            <p>Thank you,<br/>Mentoring App</p>
            """;
    }
}
