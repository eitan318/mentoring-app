namespace MentoringApp.Data.DTO
{
    public class VerificationCodeDao
    {
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
    }
}
