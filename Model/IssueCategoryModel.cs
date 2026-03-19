using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class IssueCategoryModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }

        [SetsRequiredMembers]
        public IssueCategoryModel(string name, int id = -1)
        {
            Name = name;
            Id = id;
        }

        public IssueCategoryModel() { }
    }
}
