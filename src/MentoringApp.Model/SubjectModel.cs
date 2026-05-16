namespace MentoringApp.Model
{
    /// <summary>Lookup record for an academic subject (e.g. "Math", "Physics").</summary>
    public class SubjectModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
    }
}
