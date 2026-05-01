using MentoringApp.Model;
using MentoringApp.ViewModel.IService;

namespace MentoringApp.ViewModel.ViewModel.Supervisor;

public class PairProgressItem
{
    private readonly ILocalizationService _loc;

    public PairModel Pair { get; }
    public MatchTier MatchTier => Pair.MatchTier;
    public int Id => Pair.Id;
    public bool IsProfileIncomplete => Pair.IsProfileIncomplete;

    public double TotalMeetingHours { get; }
    public double RequiredMeetingHours { get; }
    public double HoursProgress { get; }
    public string ProgressText => $"{TotalMeetingHours:0.#} / {RequiredMeetingHours:0.#} {_loc.Get("Common_HoursUnit")}";

    public PairProgressItem(PairModel pair, double totalHours, double requiredHours, ILocalizationService loc)
    {
        Pair = pair;
        TotalMeetingHours = totalHours;
        RequiredMeetingHours = requiredHours;
        HoursProgress = requiredHours > 0 ? Math.Min(100, (totalHours / requiredHours) * 100) : 0;
        _loc = loc;
    }
}
