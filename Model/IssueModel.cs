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
        public DateTime CreationDate { get; set; }
        public bool IsResolved { get; set; }

        public IssueModel() { }

        [SetsRequiredMembers]
        public IssueModel(string desc, IssueCategoryModel category, bool isResulved)
        {
            Description = desc;
            Category = category;
            IsResolved = isResulved;
            Id = -1;
        }
    }



}
