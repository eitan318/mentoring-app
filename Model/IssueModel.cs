using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class IssueModel
    {
        public required int Id { get; set; }
        public required string Description { get; set; }
        public required IssueCategoryModel Category { get; set; }
        public int ReportedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public bool IsResolved { get; set; }
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
