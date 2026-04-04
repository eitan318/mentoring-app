using MentoringApp.Model.User;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class Pair
    {
        public required StudentModel Mentee { get; set; }
        public required StudentModel Mentor { get; set; }
        public required int Id { get; set; }

        /// <summary>Which tier of the matching process created this pair.</summary>
        public MatchTier MatchTier { get; set; } = MatchTier.AdminManual;

        /// <summary>
        /// True when the pair was created via Tier 5 fallback because one or both
        /// users had incomplete profile data. Displayed as a warning on the Supervisor dashboard.
        /// </summary>
        public bool IsProfileIncomplete { get; set; } = false;
    }
}
