namespace MentoringApp.Data.DTO
{
    public class PairDto
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int MenteeId { get; set; }
        public int SupervisorId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
