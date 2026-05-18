namespace MentoringApp.Model
{
    /// <summary>
    /// Represents a participating class slot (Grade + Class Number) defined by the admin.
    /// </summary>
    public class SchoolClassModel : BaseModel
    {
        public required GradeModel Grade { get; set; }
        public required int ClassNum { get; set; }

        public string DisplayName => $"{Grade?.Name} – Class {ClassNum}";
    }
}
