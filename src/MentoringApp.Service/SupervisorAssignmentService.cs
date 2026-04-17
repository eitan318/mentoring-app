using MentoringApp.Model.User;
using MentoringApp.Service;

namespace MentoringApp.Service
{
    /// <summary>
    /// Resolves which supervisor is responsible for a given mentee.
    /// Used by the matching pipeline and any other service that creates pairs.
    /// </summary>
    public class SupervisorAssignmentService
    {
        private readonly UserService _userService;

        public SupervisorAssignmentService(UserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Returns the supervisor ID for a mentee based on their class and grade.
        /// Falls back to the first available supervisor, then to ID 1 as a last resort.
        /// </summary>
        public async Task<int> GetForMenteeAsync(int menteeId)
        {
            var uRes = await _userService.GetUserByIdAsync(menteeId);
            if (uRes.Data is not StudentModel mentee)
                return 1;

            var allUsers = await _userService.GetAllUsersAsync();
            var supervisors = allUsers.OfType<SupervisorModel>().ToList();

            var assigned = supervisors.FirstOrDefault(s =>
                s.AssignedClasses.Any(c =>
                    c.Grade?.Id == mentee.Grade?.Id &&
                    c.ClassNum == mentee.ClassNum));

            if (assigned != null) return assigned.Id;

            var fallback = supervisors.FirstOrDefault();
            return fallback?.Id ?? 1;
        }
    }
}