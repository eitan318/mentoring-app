using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MentoringApp.Service
{
    /// <summary>
    /// Pure compatibility scoring logic, isolated for easy extension.
    /// No dependencies — all methods are stateless.
    /// </summary>
    public class CompatibilityScorer
    {
        /// <summary>
        /// Returns a 0–100 compatibility score between a mentee and mentor.
        /// Currently: 100 if subjects match exactly, 0 otherwise.
        /// Extend here when adding weighted/partial matching.
        /// </summary>
        public double Calculate(int? menteeSubjectId, int? mentorSubjectId)
        {
            if (menteeSubjectId == null || mentorSubjectId == null) return 0;
            return menteeSubjectId == mentorSubjectId ? 100.0 : 0.0;
        }
    }
}