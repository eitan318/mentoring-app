using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Data.Dao
{
    /// <summary>Flat row mirror of the IssueCategories lookup table.</summary>
    public class IssueCategoryDao
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
