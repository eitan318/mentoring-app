using MentoringApp.Model;
using MentoringApp.Model.User;

namespace MentoringApp.ViewModel.ViewModel.Supervisor
{
    /// <summary>
    /// Wraps a <see cref="Pair"/> together with its meeting-hours progress so the
    /// supervisor list can sort by completion and show inline progress bars.
    /// </summary>
    public class PairProgressItem
    {
        public Pair Pair { get; }

        // Forwarded for existing XAML bindings that address these directly
        public StudentModel Mentor       => Pair.Mentor;
        public StudentModel Mentee       => Pair.Mentee;
        public MatchTier    MatchTier    => Pair.MatchTier;
        public int          Id           => Pair.Id;
        public bool         IsProfileIncomplete => Pair.IsProfileIncomplete;

        public double TotalMeetingHours    { get; }
        public double RequiredMeetingHours { get; }

        /// <summary>Percentage 0-100 for a ProgressBar.</summary>
        public double HoursProgress        { get; }

        /// <summary>Compact display string, e.g. "3.5h / 10h".</summary>
        public string ProgressText => $"{TotalMeetingHours:0.#}h / {RequiredMeetingHours:0.#}h";

        public PairProgressItem(Pair pair, double totalHours, double requiredHours)
        {
            Pair = pair;
            TotalMeetingHours    = totalHours;
            RequiredMeetingHours = requiredHours;
            HoursProgress        = requiredHours > 0
                ? Math.Min(100, (totalHours / requiredHours) * 100)
                : 0;
        }
    }
}
