using Microsoft.Extensions.DependencyInjection;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;


namespace MentoringApp.DI
{
    public static class ViewDependencyInjection
    {
        public static IServiceCollection AddView(this IServiceCollection services)
        {
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ILanguageService, LanguageService>();
            services.AddTransient<DummyDataSeeder>();
            services.AddSingleton<MainWindow>();

            return services;
        }
    }
}
