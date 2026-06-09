namespace MentoringApp.Data.DTO
{
    /// <summary>Flat row mirror of the VerificationCodes table (one login code per user).</summary>
    public class VerificationCodeDao
    {
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
    }
}
