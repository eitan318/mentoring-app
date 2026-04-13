using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{

    public class SupervisorModel : UserModel
    {
        /// <summary>All school class slots assigned to this supervisor.</summary>
        public List<SchoolClass> AssignedClasses { get; set; } = new();

        /// <summary>Legacy compat: first assigned class's grade (or null).</summary>
        public Grade? Grade
        {
            get => AssignedClasses.FirstOrDefault()?.Grade;
            set { /* kept for backward compat — do not use to set */ }
        }

        /// <summary>Legacy compat: first assigned class's ClassNum (or 0).</summary>
        public int ClassNum
        {
            get => AssignedClasses.FirstOrDefault()?.ClassNum ?? 0;
            set { /* kept for backward compat — do not use to set */ }
        }

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

        public int Problematicness() => PendingCount;
    }
}
