namespace MentoringApp.Data.DTO
{

    public class IssueDao
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int ReportedByUserId { get; set; }
        public int IsResolved { get; set; }
        public string CreationDate { get; set; } = string.Empty;
        public int? ForwardedBySupervisorId { get; set; }
    }
}
