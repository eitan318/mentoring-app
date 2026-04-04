using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.Service
{
    public class SettingsService
    {
        private readonly ISettingsRepo _settingsRepo;
        public const string MeetingHoursBarrierKey = "MeetingHoursBarrier";

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
    }
}
