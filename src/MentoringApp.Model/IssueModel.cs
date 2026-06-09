using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    /// <summary>
    /// A problem reported by a student to their supervisor. A supervisor can resolve it,
    /// or forward it up to the admin (then <see cref="IsForwardedToAdmin"/> is true).
    /// </summary>
    public class IssueModel : BaseModel
    {
        public required string Description { get; set; }
        public required IssueCategoryModel Category { get; set; }
        public int ReportedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public bool IsResolved { get; set; }
        /// <summary>Id of the supervisor who escalated this issue to the admin; null if not forwarded.</summary>
        public int? ForwardedBySupervisorId { get; set; }
        public bool IsForwardedToAdmin => ForwardedBySupervisorId.HasValue;

        public IssueModel() { }

        [SetsRequiredMembers]
        public IssueModel(string desc, IssueCategoryModel category, bool isResulved, int reportedByUserId)
        {
            Description = desc;
            Category = category;
            IsResolved = isResulved;
            ReportedByUserId = reportedByUserId;
            Id = -1;
        }
    }



}
