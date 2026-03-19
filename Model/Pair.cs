using MentoringApp.Model.User;
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
        public required StudentModel Mentee { get; set; }
        public required StudentModel Mentor { get; set; }
        public required int Id { get; set; }
    }
}
