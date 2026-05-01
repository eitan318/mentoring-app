using MentoringApp.Model.User.StudentProfiles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{
    public class StudentModel : UserModel
    {
        public GradeModel? Grade { get; set; }
        public int ClassNum { get; set; }
        public MentorProfile? MentorProfile { get; set; }
        public MenteeProfile? MenteeProfile { get; set; }

        /// <summary>Set by mentees: preferred gender of their mentor.</summary>
        public GenderPreference PreferredMentorGender { get; set; } = GenderPreference.NoPreference;

        /// <summary>Set by mentors: preferred gender of their mentee(s).</summary>
        public GenderPreference PreferredMenteeGender { get; set; } = GenderPreference.NoPreference;

        public bool IsMentor => MentorProfile != null;
        public bool IsMentee => MenteeProfile != null;

        public StudentModel() : base() { }

        public StudentModel(int id, string email, string userName, string nationalId, GradeModel? grade)
            : base(id, email, userName, nationalId)
        {
            Grade = grade;
        }

        public bool CanHaveMentorProfile()
        {
            // Business Rule: Students below grade 9 can't be mentors
            return Grade?.Num >= 9;
        }


    }
}
