namespace MentoringApp.Data.SQLEF.DataObject
{
    internal class UserData
    {
        public int Id { get; set; }        
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string NationalId { get; set; }
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
        public int UserId { get; set; }
        public int GradeId { get; set; }
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
        public required string Code { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
