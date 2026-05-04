namespace MentoringApp.Model
{
    /// <summary>
    /// Represents a participating class slot (Grade + Class Number) defined by the admin.
    /// </summary>
    public class SchoolClassModel
    {
        public int Id { get; set; }
        public required GradeModel Grade { get; set; }
        public required int ClassNum { get; set; }

        public string DisplayName => $"{Grade?.Name} – Class {ClassNum}";
    }
}
