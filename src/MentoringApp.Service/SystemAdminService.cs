using MentoringApp.Data.Interfaces;

namespace MentoringApp.Service;

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
