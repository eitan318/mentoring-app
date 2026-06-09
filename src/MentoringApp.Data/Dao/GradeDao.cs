using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Data.Dao
{
    /// <summary>Flat row mirror of the Grades lookup table.</summary>
    public class GradeDao
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Num { get; set; }
    }


}
