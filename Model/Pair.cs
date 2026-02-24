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

        [SetsRequiredMembers]
        public Pair()
        {
            Mentee = new Student("Dummy1");
            Mentor = new Student("Dummy2");
        }
        public required Student Mentee { get; set; }
        public required Student Mentor { get; set; }
    }
}
