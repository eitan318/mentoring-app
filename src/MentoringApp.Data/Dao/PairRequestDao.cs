namespace MentoringApp.Data.DTO
{
    public class PairRequestDao
    {
        public int Id { get; set; }
        public int MenteeId { get; set; }
        public int MentorId { get; set; }
        /// <summary>"Pending", "Accepted", or "Rejected"</summary>
        public string Status { get; set; } = "Pending";
        /// <summary>Match tier as integer (1, 3…)</summary>
        public int Tier { get; set; } = 1;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
