using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    /// <summary>Lookup entity for an issue category (e.g. "Behaviour", "Attendance") that a reported issue is classified under.</summary>
    public class IssueCategoryModel : BaseModel
    {
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
