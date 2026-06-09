namespace MentoringApp.Data.DTO
{
    /// <summary>Flat row mirror of the MatchScores table (precomputed mentee↔mentor compatibility %).</summary>
    public class MatchScoreDao
    {
        public int Id { get; set; }
        public int MenteeId { get; set; }
        public int MentorId { get; set; }
        public double ScorePercent { get; set; }
    }
}
