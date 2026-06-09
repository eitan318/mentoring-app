namespace MentoringApp.Model
{
    /// <summary>Lookup entity for a subject (e.g. "Math", "English") a mentor teaches or a mentee wants to learn.</summary>
    public class SubjectModel : BaseModel
    {
        public required string Name { get; set; }
    }
}
