using System.Diagnostics.CodeAnalysis;

namespace MentoringApp.Model
{
    /// <summary>Lookup record for a school grade (e.g. Name="Grade 9", Num=9).</summary>
    public class GradeModel
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required int Num { get; set; }
    }
}