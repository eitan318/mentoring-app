using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class Review
    {


        [SetsRequiredMembers]
        public Review(string content, DateTime date) {
            Content = content;
            Date = date;
        }
        public required string Content { get; set; }
        public required DateTime Date { get; set; }
    }
}
