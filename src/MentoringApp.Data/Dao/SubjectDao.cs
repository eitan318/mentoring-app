namespace MentoringApp.Data.DTO
{
    /// <summary>Flat row mirror of the Subjects lookup table.</summary>
    public class SubjectDao
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
