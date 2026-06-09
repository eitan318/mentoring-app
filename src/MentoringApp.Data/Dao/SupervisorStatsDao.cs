using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Data.DTO
{
    /// <summary>Projection row from a JOIN/aggregate query: per-supervisor issue and pair counts for the admin dashboard.</summary>
    public class SupervisorStatsDao
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int PendingIssuesCount { get; set; }
        public int ResolvedIssuesCount { get; set; }
        public int PairsCount { get; set; }
    }
}
