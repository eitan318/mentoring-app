using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model
{
    public class AvailabilitySlot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsBooked { get; set; }
        // Helper for the UI to show "10:00 AM - 10:30 AM"
        public string TimeLabel => $"{StartTime:t} - {EndTime:t}";
    }
}
