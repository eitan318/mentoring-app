using MentoringApp.Model;

namespace MentoringApp.ViewModel.ViewModel.Supervisor;

/// <summary>
/// Wraps a <see cref="PairModel"/> with meeting-hours progress so the supervisor list
/// can sort by completion and show inline progress bars.
/// </summary>
public class PairProgressItem
{
    public PairModel Pair { get; }
    public MatchTier MatchTier => Pair.MatchTier;
    public int Id => Pair.Id;
    public bool IsProfileIncomplete => Pair.IsProfileIncomplete;

    public double TotalMeetingHours { get; }
    public double RequiredMeetingHours { get; }
    public double HoursProgress { get; }
    public string ProgressText => $"{TotalMeetingHours:0.#}h / {RequiredMeetingHours:0.#}h";

    public PairProgressItem(PairModel pair, double totalHours, double requiredHours)
    {
        Pair = pair;
        TotalMeetingHours = totalHours;
        RequiredMeetingHours = requiredHours;
        HoursProgress = requiredHours > 0 ? Math.Min(100, (totalHours / requiredHours) * 100) : 0;
    }
}
