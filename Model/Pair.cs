using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class Pair
    {
        public required Student Mentee { get; set; }
        public required Student Mentor { get; set; }
        public required int Id { get; set; }
    }
}
