using MentoringApp.Data.Acess.SQLite;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.Data.DI
{
    public static class DataDependencyInjection
    {
        /// <summary>
        /// Registers raw SQLite repositories (no Entity Framework).
        /// </summary>
        public static IServiceCollection AddDataRepositories(this IServiceCollection services, string connectionString)
        {
            return services.AddSqlDataRepositories(connectionString);
        }

        /// <summary>
        /// Raw ADO.NET / Microsoft.Data.Sqlite repositories.
        /// </summary>
        public static IServiceCollection AddSqlDataRepositories(this IServiceCollection services, string connectionString)
        {
            // Derive the file path from "Data Source=<path>" for SQLiteConnectionService
            string dbPath = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                ? connectionString["Data Source=".Length..]
                : connectionString;

            services.AddSingleton<ISQLiteConnectionService>(_ => new SQLiteConnectionService(dbPath));

            services.AddSingleton<IDbRepo>(_ => new SqlDbRepo(connectionString));
            services.AddSingleton<IGradeRepo>(sp =>
                new SqlGradeRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddSingleton<ISubjectRepo>(sp =>
                new SqlSubjectRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddSingleton<IVerificationCodeRepo>(sp =>
                new SqlVerificationCodeRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddSingleton<IUserRepo>(sp =>
                new SqlUserRepo(
                    sp.GetRequiredService<ISQLiteConnectionService>(),
                    connectionString));

            services.AddScoped<IPairRepo>(sp =>
                new SqlPairRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddScoped<IIssueRepo>(sp =>
                new SqlIssueRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddScoped<IIssueCategoryRepo>(sp =>
                new SqlIssueCategoryRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddScoped<IReviewRepo>(sp =>
                new SqlReviewRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddScoped<ISettingsRepo>(sp =>
                new SqlSettingsRepo(sp.GetRequiredService<ISQLiteConnectionService>()));

            services.AddScoped<IPairRequestRepo>(sp =>
                new SqlPairRequestRepo(sp.GetRequiredService<ISQLiteConnectionService>()));
            services.AddScoped<IMatchScoreRepo>(sp =>
                new SqlMatchScoreRepo(sp.GetRequiredService<ISQLiteConnectionService>()));


            return services;
        }

    }
}
