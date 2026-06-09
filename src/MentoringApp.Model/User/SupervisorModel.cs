using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{

    /// <summary>
    /// A supervisor (teacher) user. Oversees the students in their <see cref="AssignedClasses"/>,
    /// handles their issues, and approves pair requests. Issue counts are computed live from
    /// <see cref="Issues"/> when loaded, or fall back to manually-set values (used by the admin
    /// dashboard, which loads counts without the full issue list).
    /// </summary>
    public class SupervisorModel : UserModel
    {
        /// <summary>All school class slots assigned to this supervisor.</summary>
        public List<SchoolClassModel> AssignedClasses { get; set; } = new();
        public SupervisorModel() : base() { }

        [SetsRequiredMembers]
        public SupervisorModel(int id, string email, string userName, string nationalId)
            : base(id, email, userName, nationalId) { }

        public IEnumerable<IssueModel>? Issues { get; set; }

        private int? _manualPendingCount;
        private int? _manualResolvedCount;

        public int PendingCount
        {
            get => Issues?.Count(i => !i.IsResolved) ?? _manualPendingCount ?? 0;
            set => _manualPendingCount = value;
        }

        public int ResolvedCount
        {
            get => Issues?.Count(i => i.IsResolved) ?? _manualResolvedCount ?? 0;
            set => _manualResolvedCount = value;
        }

        public int SupervisedPairsCount { get; set; }

        // ── Student fill-progress (set by the admin dashboard at load time) ──
        public int FilledStudentsCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public double FillProgressPercent => TotalStudentsCount > 0
            ? (double)FilledStudentsCount / TotalStudentsCount * 100 : 0;
        public string FillProgressLabel => $"{FilledStudentsCount}/{TotalStudentsCount}";

        public IEnumerable<IssueModel> PendingIssues =>
            Issues?.Where(i => !i.IsResolved) ?? Enumerable.Empty<IssueModel>();

        public IEnumerable<IssueModel> ResolvedIssues =>
            Issues?.Where(i => i.IsResolved) ?? Enumerable.Empty<IssueModel>();

        /// <summary>Workload heuristic (= number of pending issues) used to pick the least-busy supervisor when auto-forwarding issues.</summary>
        public int Problematicness() => PendingCount;
    }
}
