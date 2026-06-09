using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User.StudentProfiles
{
    /// <summary>Mentor-specific data attached to a <see cref="StudentModel"/>: the subject taught and how many mentees can be taken on.</summary>
    public class MentorProfile
    {
        public int SubjectToTeach { get; set; }
        public int MaxMentees { get; set; } = 1;
    }
}
