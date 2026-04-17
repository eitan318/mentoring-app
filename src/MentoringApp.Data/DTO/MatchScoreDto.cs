namespace MentoringApp.Data.DTO
{
    public class MatchScoreDto
    {
        public int Id { get; set; }
        public int MenteeId { get; set; }
        public int MentorId { get; set; }
        public double ScorePercent { get; set; }
    }
}
