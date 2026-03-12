namespace MentoringApp.Data.Access.SQLEF.DataObject
{
    internal class ReviewData
    {
        public int Id { get; set; }
        public int PairId { get; set; }
        public int AuthorUserId { get; set; }
        public required string Content { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
