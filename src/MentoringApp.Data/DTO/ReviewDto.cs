namespace MentoringApp.Data.DTO
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int PairId { get; set; }
        public int AuthorUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public double AmountOfHours { get; set; }
    }
}
