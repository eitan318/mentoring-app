namespace MentoringApp.Data.SQLEF.DataObject
{
    internal class UserData
    {
        public int Id { get; set; }        
        public string Email { get; set; }
        public string UserName { get; set; }
        public string NationalId { get; set; }
    }

    internal class UserMenteeData
    {
        public int UserId { get; set; }
        public int SubjectToLearn { get; set; }
    }

    internal class UserMentorData
    {
        public int UserId { get; set; }
        public int SubjectToTeach { get; set; } 
    }

    internal class UserStudentData
    {
        public int UserId { get; set; } // Id in user table
        public int Grade { get; set; }
    }

    internal class UserSupervisorData
    {
        public int UserId { get; set; }
    }

    internal class UserAdminData
    {
        public int UserId { get; set; }
    }

    internal class VerificationCodeData
    {      
        public int UserId { get; set; }
        public string Code { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
