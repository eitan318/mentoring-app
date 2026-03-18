using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

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
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string NationalId { get; set; }
        public string? ProfilePicturePath { get; set; }
        public VerificationCode? CurrentVerificationCode { get; set; }

        // Empty constructor for serialization/tools
        protected User() { }

        [SetsRequiredMembers]
        protected User(int id, string email, string userName, string nationalId)
        {
            Id = id;
            Email = email;
            UserName = userName;
            NationalId = nationalId;
        }
    }

    public class Admin : User
    {
        public Admin() : base() { }

        [SetsRequiredMembers]
        public Admin(int id, string email, string userName, string nationalId)
            : base(id, email, userName, nationalId) { }
    }

    public class Supervisor : User
    {
        public Supervisor() : base() { }

        [SetsRequiredMembers]
        public Supervisor(int id, string email, string userName, string nationalId)
            : base(id, email, userName, nationalId) { }
    }

    public class Student : User
    {
        public required Grade Grade { get; set; }
        public MentorProfile? MentorProfile { get; set; }
        public MenteeProfile? MenteeProfile { get; set; }

        public bool IsMentor => MentorProfile != null;
        public bool IsMentee => MenteeProfile != null;

        public Student() : base() { }

        [SetsRequiredMembers]
        public Student(int id, string email, string userName, string nationalId, Grade grade)
            : base(id, email, userName, nationalId)
        {
            Grade = grade;
        }
    }

    public class MentorProfile { 
        public int SubjectToTeach { get; set; }
    }
    public class MenteeProfile { 
        public int SubjectToLearn { get; set; }
    
    }
}