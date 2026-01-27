namespace MentoringApp.Model
{
    public enum UserRole
    {
        None = 0,
        Admin,
        Supervisor,
        Student,
    }
    public abstract class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string NationalId { get; set; }

        public VerificationCode CurrentVerificationCode { get; set; }
    }

    /* 
     * VerificationCode is part of the User aggregate.
     * It represents temporary user state and is persisted via the User repository.
    */
    public class VerificationCode
    {
        public string Code { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class Admin : User { /* Admin specific logic */ }

    public class Supervisor : User { 
        public int PairsSupervised { get; set;}
        public List<Issue> PendingIssues { get; set; }
        public List<Issue> ResulvedIssues { get; set; }
    }

    public class Issue
    {
        public string Description { get; set; }
        public int Cateory { get; set; }
    }

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
