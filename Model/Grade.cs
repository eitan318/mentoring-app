

using System.Diagnostics.CodeAnalysis;

namespace MentoringApp.Model
{
    public class Grade
    {
        [SetsRequiredMembers]
        public Grade(string name, int id = -1, int num = 0)
        {
            Name = name;
            Id = id;
            Num = num;
        }
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required int Num { get; set; }
    }
}
