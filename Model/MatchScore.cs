namespace MentoringApp.Model
{
    /// <summary>
    /// Holds a single cell of the Tier 2 compatibility score matrix.
    /// ScorePercent is 0–100 based on overlapping subject tags.
    /// </summary>
    public class MatchScore
    {
        public int Id { get; set; }
        public int MenteeId { get; set; }
        public int MentorId { get; set; }

        /// <summary>0-100 compatibility score.</summary>
        public double ScorePercent { get; set; }

        // Populated by service layer for UI display
        public string MentorName { get; set; } = string.Empty;
        public string MentorProfilePicturePath { get; set; } = string.Empty;
        public string MentorSubjectName { get; set; } = string.Empty;
        public string MenteeSubjectName { get; set; } = string.Empty;
    }
}
