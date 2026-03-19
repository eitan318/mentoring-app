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

        public IEnumerable<IssueModel> PendingIssues =>
            Issues?.Where(i => !i.IsResolved) ?? Enumerable.Empty<IssueModel>();

        public IEnumerable<IssueModel> ResolvedIssues =>
            Issues?.Where(i => i.IsResolved) ?? Enumerable.Empty<IssueModel>();

        public int Problematicness() => PendingCount;
    }
}
