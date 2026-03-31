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
    }
}
