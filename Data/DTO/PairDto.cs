namespace MentoringApp.Data.DTO
{
    public class PairDto
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int MenteeId { get; set; }
        public int SupervisorId { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        /// <summary>Stored as an integer matching the MatchTier enum.</summary>
        public int MatchTier { get; set; } = 0;
        public bool IsProfileIncomplete { get; set; } = false;
    }
}
