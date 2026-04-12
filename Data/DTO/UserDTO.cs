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

    public class SchoolClassDto
    {
        public int Id { get; set; }
        public int GradeId { get; set; }
        public int ClassNum { get; set; }
    }
    public class UserDto
    {
        // Core Identity
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public string Language { get; set; } = "en";
        public string? PhoneNumber { get; set; }
        public int Gender { get; set; } = 3; // Gender.PreferNoAnswer

        // Discriminator/Role info
        public UserRoleType Role { get; set; }

        // Role-Specific Data (Now including Grade details)
        public int? GradeId { get; set; }
        public int? ClassNum { get; set; }
        public int? PreferredMentorGender { get; set; } // GenderPreference — mentee's preference for mentor gender
        public int? PreferredMenteeGender { get; set; } // GenderPreference — mentor's preference for mentee gender


        public int? MentorSubjectId { get; set; }
        public int? MaxMentees { get; set; }

        public int? MenteeSubjectId { get; set; }

        // Auth Data
        public string? VerificationCode { get; set; }
        public DateTime? VerificationCodeCreated { get; set; }
    }

    public enum UserRoleType { Student, Admin, Supervisor }

}
