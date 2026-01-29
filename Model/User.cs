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
        public VerificationCode? CurrentVerificationCode { get; set; }

        protected User() { }

        [SetsRequiredMembers]
        protected User(string dummyName)
        {
            UserName = dummyName;
            Email = "dummy@example.com";
            NationalId = "000000000";
        }
    }


    public class Admin : User 
    {
        public Admin() : base() { }

        [SetsRequiredMembers]
        public Admin(string name) : base(name) { }
    }

    public class Supervisor : User 
    { 
        public Supervisor() : base() { }

        [SetsRequiredMembers]
        public Supervisor(string name) : base(name) { }
    }


    public class Student : User
    {
        public required Grade Grade { get; set; }
        public MentorProfile? MentorProfile { get; set; }
        public MenteeProfile? MenteeProfile { get; set; }

        public bool IsMentor => MentorProfile != null;
        public bool IsMentee => MenteeProfile != null;

        public Student() : base() { 
            Grade = new Grade("Grade Name");
        }

        [SetsRequiredMembers]
        public Student(string name) : base(name) { 
            Grade = new Grade("Grade Name");
        }
    }

    public class MentorProfile { 
        public int SubjectToTeach { get; set; }
    }
    public class MenteeProfile { 
        public int SubjectToLearn { get; set; }
    
    }
}