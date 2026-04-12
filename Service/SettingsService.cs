using MentoringApp.Data.Interfaces;

namespace MentoringApp.Service
{
    public class SettingsService
    {
        private readonly ISettingsRepo _settingsRepo;

        public const string MeetingHoursBarrierKey = "MeetingHoursBarrier";
        public const string GlobalLanguageKey = "GlobalLoginLanguage";
        public const string Phase1DeadlineKey = "Phase1Deadline";
        public const string Phase2DeadlineKey = "Phase2Deadline";
        public const string IsPhase1CompleteKey = "IsPhase1Complete";
        public const string IsProcessCompleteKey = "IsProcessComplete";
        public const string IsSchoolConfiguredKey = "IsSchoolConfigured";

        public SettingsService(ISettingsRepo settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        // ── Meeting Hours ─────────────────────────────────────────────────────

        public Task<double> GetMeetingHoursBarrierAsync()
            => _settingsRepo.GetDoubleAsync(MeetingHoursBarrierKey, 10);

        public Task SetMeetingHoursBarrierAsync(double hours)
            => _settingsRepo.SetDoubleAsync(MeetingHoursBarrierKey, hours);

        // ── Language ──────────────────────────────────────────────────────────

        public Task<string> GetGlobalLanguageAsync()
            => _settingsRepo.GetStringAsync(GlobalLanguageKey, "en");

        public Task SetGlobalLanguageAsync(string lang)
            => _settingsRepo.SetStringAsync(GlobalLanguageKey, lang);

        // ── Phase 1 Deadline ──────────────────────────────────────────────────

        public async Task<DateTime?> GetPhase1DeadlineAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(Phase1DeadlineKey, "");
            return DateTime.TryParse(raw, out var d) ? d : null;
        }

        public Task SetPhase1DeadlineAsync(DateTime date)
            => _settingsRepo.SetStringAsync(Phase1DeadlineKey, date.ToString("o"));

        public Task ClearPhase1DeadlineAsync()
            => _settingsRepo.SetStringAsync(Phase1DeadlineKey, "");

        // ── Phase 2 Deadline ──────────────────────────────────────────────────

        public async Task<DateTime?> GetPhase2DeadlineAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(Phase2DeadlineKey, "");
            return DateTime.TryParse(raw, out var d) ? d : null;
        }

        public Task SetPhase2DeadlineAsync(DateTime date)
            => _settingsRepo.SetStringAsync(Phase2DeadlineKey, date.ToString("o"));

        public Task ClearPhase2DeadlineAsync()
            => _settingsRepo.SetStringAsync(Phase2DeadlineKey, "");

        // ── Phase State ───────────────────────────────────────────────────────

        public async Task<bool> GetIsPhase1CompleteAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(IsPhase1CompleteKey, "false");
            return bool.TryParse(raw, out var b) && b;
        }

        public Task SetIsPhase1CompleteAsync(bool value)
            => _settingsRepo.SetStringAsync(IsPhase1CompleteKey, value.ToString());

        public async Task<bool> GetIsProcessCompleteAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(IsProcessCompleteKey, "false");
            return bool.TryParse(raw, out var b) && b;
        }

        public Task SetIsProcessCompleteAsync(bool value)
            => _settingsRepo.SetStringAsync(IsProcessCompleteKey, value.ToString());

        // ── School Configuration ──────────────────────────────────────────────

        public async Task<bool> GetIsSchoolConfiguredAsync()
        {
            string raw = await _settingsRepo.GetStringAsync(IsSchoolConfiguredKey, "false");
            return bool.TryParse(raw, out var b) && b;
        }

        public Task SetIsSchoolConfiguredAsync(bool value)
            => _settingsRepo.SetStringAsync(IsSchoolConfiguredKey, value.ToString());
    }
}