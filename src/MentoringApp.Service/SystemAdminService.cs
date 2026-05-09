using MentoringApp.Data.Interfaces;

namespace MentoringApp.Service;

public class SystemAdminService
{
    private readonly IDbRepo _dbRepo;
    private readonly DummyDataSeeder _seeder;
    private readonly IYearAdvanceRepo _yearAdvanceRepo;
    private readonly IUserRepo _userRepo;
    private readonly IPairRepo _pairRepo;
    private readonly SettingsService _settingsService;

    public SystemAdminService(
        IDbRepo dbRepo,
        DummyDataSeeder seeder,
        IYearAdvanceRepo yearAdvanceRepo,
        IUserRepo userRepo,
        IPairRepo pairRepo,
        SettingsService settingsService)
    {
        _dbRepo = dbRepo;
        _seeder = seeder;
        _yearAdvanceRepo = yearAdvanceRepo;
        _userRepo = userRepo;
        _pairRepo = pairRepo;
        _settingsService = settingsService;
    }

    public async Task RecreateDatabaseAsync()
    {
        _dbRepo.Recreate();
        await Task.CompletedTask;
    }

    public async Task SeedDatabaseAsync()
    {
        await _seeder.SeedAsync();
    }

    /// <summary>
    /// Advances the system to the next academic year:
    /// <list type="number">
    ///   <item>Removes all students in the highest grade (they graduate),
    ///         along with their related data — role rows, verification codes,
    ///         pairs, reviews, and reported issues — via
    ///         <see cref="IUserRepo.DeleteUserAsync"/>.</item>
    ///   <item>Moves every remaining student up one grade.</item>
    ///   <item>Deletes all pairs, reviews, and pair requests.</item>
    ///   <item>Resets all admin settings flags to their initial values so the
    ///         admin wizard starts from step 1 again.</item>
    /// </list>
    /// </summary>
    public async Task AdvanceYearAsync()
    {
        // 1. Find and fully delete graduating students. DeleteUserAsync
        // cascades through role tables, verification codes, the user's
        // reviews/issues, plus pairs they're in.
        var graduatingIds = await _yearAdvanceRepo.GetGraduatingStudentIdsAsync();
        foreach (var userId in graduatingIds)
            await _userRepo.DeleteUserAsync(userId);

        // 2. Promote every remaining student to the next grade
        await _yearAdvanceRepo.AdvanceStudentGradesAsync();

        // 3. Wipe all pairs, reviews, and pair requests
        await _pairRepo.DeleteAllAsync();

        // 4. Reset settings – admin returns to step 1 of the wizard
        await _settingsService.SetIsSchoolConfiguredAsync(false);
        await _settingsService.SetIsSupervisorsAssignedAsync(false);
        await _settingsService.SetIsUsersImportedAsync(false);
        await _settingsService.SetIsPhase1CompleteAsync(false);
        await _settingsService.SetIsProcessCompleteAsync(false);
        await _settingsService.ClearPhase1DeadlineAsync();
        await _settingsService.ClearPhase2DeadlineAsync();
    }
}
