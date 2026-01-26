using MentoringApp.Data.SQL.EF;
using MentoringApp.Data.SQL.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.Data.Interfaces
{
    public static class DataDependencyInjection
    {
        public static IServiceCollection AddDataRepositories(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<MentoringDbContext>(options => options.UseSqlite(connectionString));
            services.AddSingleton<IUserRepository, EFUserRepository>(); 
            services.AddSingleton<IRecreateDb, EFRecreateDb>(); 
            return services;
        }
    }
}
