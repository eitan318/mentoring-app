using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Data.Dao
{
    /// <summary>Flat row mirror of the SchoolClasses table (a grade + class-number slot).</summary>
    public class SchoolClassDao
    {
        public int Id { get; set; }
        public int GradeId { get; set; }
        public int ClassNum { get; set; }
    }
}
