
using System.Diagnostics.CodeAnalysis;

namespace MentoringApp.Model
{
    /// <summary>
    /// One-time 6-digit code sent to the user's email during the two-step login flow.
    /// Expires 10 minutes after <see cref="CreationDate"/>.
    /// </summary>
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
