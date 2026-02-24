using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{

    public class VerificationCode
    {
        public required string Code { get; set; }
        public DateTime CreationDate { get; set; }

        public VerificationCode() { }

        [SetsRequiredMembers]
        public VerificationCode(string code)
        {
            Code = code;
            CreationDate = DateTime.Now;
        }
    }
}
