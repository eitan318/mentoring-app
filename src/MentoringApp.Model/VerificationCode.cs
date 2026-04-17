
using System.Diagnostics.CodeAnalysis;

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
