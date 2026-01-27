using MentoringApp.Service.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.Service
{
    public static class ServiceDependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Note: Changed to AddScoped because AuthService uses the Database (Repository)
            services.AddScoped<AuthService>();
            
            // Validator
            services.AddSingleton<UserValidator>();

            // Pull settings from the "EmailSettings" section of appsettings.json
            var emailSection = configuration.GetSection("EmailSettings");

            services.AddSingleton<EmailService>(sp =>
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