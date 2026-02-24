namespace MentoringApp.Data.SQLEF.DataObject
{
    internal class IssueData
    {
        public int Id { get; set; }
        public required string Description { get; set; }
        public int CategoryId { get; set; }
        public int ReportedByUserId { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    }

    internal class IssueCategoryData
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
