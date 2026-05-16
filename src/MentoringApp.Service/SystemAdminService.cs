using MentoringApp.Data.Interfaces;

namespace MentoringApp.Service;

/// <summary>
/// Developer/admin operations: drops and recreates the SQLite database and triggers the dummy-data seed.
/// Not intended for use in production workflows.
/// </summary>
public class SystemAdminService
{
    private readonly IDbRepo _dbRepo;
    private readonly DummyDataSeeder _seeder;

    public SystemAdminService(IDbRepo dbRepo, DummyDataSeeder seeder)
    {
        _dbRepo = dbRepo;
        _seeder = seeder;
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
}
