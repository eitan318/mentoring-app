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
        public required string Description { get; set; }
        public required IssueCategory Category { get; set; } 
        public DateTime CreationDate { get; set; }

        public Issue() { }

        [SetsRequiredMembers]
        public Issue(string desc, IssueCategory category)
        {
            Description = desc;
            Category = category;
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
