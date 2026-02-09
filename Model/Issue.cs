using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class Issue
    {
        public required int Id { get; set; }
        public required string Description { get; set; }
        public required IssueCategory Category { get; set; } 
        public DateTime CreationDate { get; set; }
        public bool IsResolved { get; set; }

        public Issue() { }

        [SetsRequiredMembers]
        public Issue(string desc, IssueCategory category, bool isResulved)
        {
            Description = desc;
            Category = category;
            IsResolved = isResulved;
            Id = -1;
        }
    }
    public class IssueCategory
    {
        public required int Id { get; set; }
        public required string Name { get; set; }

        [SetsRequiredMembers]
        public IssueCategory(string name, int id = -1) 
        { 
            Name = name; 
            Id = id; 
        }

        public IssueCategory() { }
    }

}
