using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.Helpers;

namespace MentoringApp.ViewModel.ViewModel.Supervisor;

/// <summary>
/// Wraps a <see cref="PairResponse"/> with resolved user names and meeting-hours progress
/// so the supervisor list can sort by completion and show inline progress bars.
/// </summary>
public class PairProgressItem
{
    public PairResponse Pair { get; }
    public string MentorName { get; }
    public string MenteeName { get; }

    public int MentorId => Pair.MentorId;
    public int MenteeId => Pair.MenteeId;
    public MatchTier MatchTier => (MatchTier)Pair.MatchTier;
    public int Id => Pair.Id;
    public bool IsProfileIncomplete => Pair.IsProfileIncomplete;

    public double TotalMeetingHours { get; }
    public double RequiredMeetingHours { get; }
    public double HoursProgress { get; }
    public string ProgressText => $"{TotalMeetingHours:0.#}h / {RequiredMeetingHours:0.#}h";

    public PairProgressItem(PairResponse pair, string mentorName, string menteeName, double totalHours, double requiredHours)
    {
        Pair = pair;
        MentorName = mentorName;
        MenteeName = menteeName;
        TotalMeetingHours = totalHours;
        RequiredMeetingHours = requiredHours;
        HoursProgress = requiredHours > 0 ? Math.Min(100, (totalHours / requiredHours) * 100) : 0;
    }
}
