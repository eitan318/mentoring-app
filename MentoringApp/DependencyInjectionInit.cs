using MentoringApp.Data.Interfaces;
using MentoringApp.Service;
using Microsoft.Extensions.DependencyInjection;
namespace MentoringApp.ViewModel.ViewModelHelper
{
    public static class DependencyInjectionInit
    {
        public static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddViewModels();
            services.AddDataRepositories("Data Source=mentoring.db");
            services.AddServices();

            return services.BuildServiceProvider();
        }
    }
}
