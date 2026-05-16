using MentoringApp.Service.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.Service.DI
{
    public static class ServiceDependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Note: Changed to AddScoped because AuthService uses the Database (Repository)
            services.AddScoped<AuthService>();
            services.AddScoped<PairService>();
            services.AddScoped<IssueService>();
            services.AddScoped<ReviewService>();
            services.AddScoped<UserService>();
            services.AddScoped<SubjectService>();
            services.AddScoped<GradeService>();
            services.AddScoped<ExcelImportService>();
            services.AddScoped<SettingsService>();
            services.AddScoped<SchoolClassService>();
            services.AddScoped<MatchingFlowService>();
            services.AddScoped<CompatibilityScorer>();
            services.AddScoped<SupervisorAssignmentService>();
            services.AddScoped<SystemAdminService>();
            services.AddScoped<DummyDataSeeder>();

            services.AddScoped<NotificationService>();

            services.AddSingleton<SessionService>();

            // Validator
            services.AddSingleton<UserValidator>();

            // Pull settings from the "AppSettings" section of appsettings.json
            var appSection = configuration.GetSection("AppSettings");
            services.AddSingleton(new AppSettings
            {
                RecreateDbOnStartup  = bool.TryParse(appSection["RecreateDbOnStartup"],  out var rec)  && rec,
                SkipVerificationCode = bool.TryParse(appSection["SkipVerificationCode"], out var skip) && skip,
                AdminEmail           = appSection["AdminEmail"] ?? "admin@school.edu"
            });

            // Pull settings from the "EmailSettings" section of appsettings.json
            var emailSection = configuration.GetSection("EmailSettings");

            services.AddSingleton(sp =>
                new EmailService(
                    smtpHost: emailSection["SmtpHost"],
                    smtpPort: int.Parse(emailSection["SmtpPort"] ?? "587"),
                    fromEmail: emailSection["FromEmail"],
                    fromPassword: emailSection["FromPassword"]
                ));

            return services;
        }
    }
}