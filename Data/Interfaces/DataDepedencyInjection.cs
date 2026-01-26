using MentoringApp.Data.SQLEF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.Data.Interfaces
{
    public static class DataDependencyInjection
    {
        public static IServiceCollection AddDataRepositories(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<MentoringDbContext>(options => options.UseSqlite(connectionString));

            services.AddSingleton<IVerificationCodeRepo, EFVerificationCodeRepo>(); 
            services.AddSingleton<IUserRepo, EFUserRepo>(); 
            services.AddSingleton<IDbRepo, EFDbRepo>(); 
            return services;
        }
    }
}
