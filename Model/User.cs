
namespace MentoringApp.Model
{

    public enum UserRole
    {
        InvalidUserRole = 0,
        Admin = 1,
        Supervisor = 2,
        Mentor = 3,
        Mentee = 4,
    }
    public abstract class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string NationalId { get; set; }
    }

    public class Admin : User { /* Admin specific logic */ }

    public class Supervisor : User { /* Supervisor specific logic */ }

    public class Student : User
    {
        public int Grade { get; set; }

        public MentorProfile? MentorProfile { get; set; }
        public MenteeProfile? MenteeProfile { get; set; }

        // Helper logic for the UI/Service
        public bool IsMentor => MentorProfile != null;
        public bool IsMentee => MenteeProfile != null;
    }

    public class MentorProfile
    {
        public int SubjectToTeach { get; set; }
    }

    public class MenteeProfile
    {
        public int SubjectToLearn { get; set; }
    }
}
