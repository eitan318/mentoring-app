using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    /// <summary>
    /// A session log / feedback entry written by a pair member, recording what was discussed
    /// and how many hours the meeting lasted (counts toward the meeting-hours requirement).
    /// </summary>
    public class Review : BaseModel
    {
        [SetsRequiredMembers]
        public Review(string content, DateTime date, double amountOfHours = 0) {
            Content = content;
            Date = date;
            AmountOfHours = amountOfHours;
        }
        public required string Content { get; set; }
        public required DateTime Date { get; set; }
        public double AmountOfHours { get; set; }
    }
}
