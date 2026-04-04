using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.Service
{
    public class SettingsService
    {
        private readonly ISettingsRepo _settingsRepo;
        public const string MeetingHoursBarrierKey = "MeetingHoursBarrier";
        public const string Tier1DeadlineKey = "Tier1Deadline";
        public const string Tier3DeadlineKey = "Tier3Deadline";

        public SettingsService(ISettingsRepo settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        public Task<double> GetMeetingHoursBarrierAsync()
            => _settingsRepo.GetDoubleAsync(MeetingHoursBarrierKey, 10);

        public Task SetMeetingHoursBarrierAsync(double hours)
            => _settingsRepo.SetDoubleAsync(MeetingHoursBarrierKey, hours);

        private const string GlobalLanguageKey = "GlobalLoginLanguage";

        public Task<string> GetGlobalLanguageAsync()
            => _settingsRepo.GetStringAsync(GlobalLanguageKey, "en");

        public Task SetGlobalLanguageAsync(string lang)
            => _settingsRepo.SetStringAsync(GlobalLanguageKey, lang);

        // ── Tier Deadlines ────────────────────────────────────────────────────

        public async Task<DateTime?> GetTier1DeadlineAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(Tier1DeadlineKey, "");
            return DateTime.TryParse(raw, out var d) ? d : null;
        }

        public Task SetTier1DeadlineAsync(DateTime date)
            => _settingsRepo.SetStringAsync(Tier1DeadlineKey, date.ToString("o"));

        public async Task<DateTime?> GetTier3DeadlineAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(Tier3DeadlineKey, "");
            return DateTime.TryParse(raw, out var d) ? d : null;
        }

        public Task SetTier3DeadlineAsync(DateTime date)
            => _settingsRepo.SetStringAsync(Tier3DeadlineKey, date.ToString("o"));
    }
}
