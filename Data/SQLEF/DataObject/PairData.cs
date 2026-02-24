namespace MentoringApp.Data.SQLEF.DataObject
{
    internal class PairData
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int MenteeId { get; set; }
        public int SupervisorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
