using MentoringApp.Model.User;

namespace MentoringApp.Model
{
    /// <summary>
    /// The status of a pair request sent from a mentee to a mentor.
    /// </summary>
    public enum PairRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }
    /// <summary>
    /// A request from a mentee to be paired with a specific mentor, awaiting the supervisor's
    /// decision. Accepting it produces a <see cref="PairModel"/>. Display-only fields (names,
    /// picture, gender, subject) are filled in by the service layer for the UI.
    /// </summary>
    public class PairRequest
    {
        public int Id { get; set; }
        public int MenteeId { get; set; }
        public int MentorId { get; set; }
        public PairRequestStatus Status { get; set; } = PairRequestStatus.Pending;
        public MatchTier Tier { get; set; } = MatchTier.Direct;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Populated by the service layer for display
        public string MenteeName { get; set; } = string.Empty;
        public string MentorName { get; set; } = string.Empty;
        public string MenteeProfilePicturePath { get; set; } = string.Empty;
        public Gender MenteeGender { get; set; } = Gender.PreferNoAnswer;

        // Subject/interest information (populated from profile)
        public string MenteeSubjectName { get; set; } = string.Empty;
    }
}
