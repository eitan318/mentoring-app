using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.DI;

public static class ViewDependencyInjection
{
    public static IServiceCollection AddView(this IServiceCollection services)
    {
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<IToastService>(ToastService.Instance);
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}
