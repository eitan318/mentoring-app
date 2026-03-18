using MentoringApp.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Data.DTO
{

    public class GradeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Num { get; set; }
    }
    public class UserDto
    {
        // Core Identity
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }

        // Discriminator/Role info
        public UserRoleType Role { get; set; }

        // Role-Specific Data (Now including Grade details)
        public int? GradeId { get; set; }

        public int? MentorSubjectId { get; set; }
        public int? MenteeSubjectId { get; set; }

        // Auth Data
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeCreated { get; set; }
    }

    public enum UserRoleType { Student, Admin, Supervisor }

}
