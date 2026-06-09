using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User.StudentProfiles
{

    /// <summary>Mentee-specific data attached to a <see cref="StudentModel"/>: the subject the student wants help with.</summary>
    public class MenteeProfile
    {
        public int SubjectToLearn { get; set; }

    }
}
