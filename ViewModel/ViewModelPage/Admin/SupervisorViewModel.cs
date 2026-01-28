using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class SupervisorViewModel : ObservableObject, INavigatable<int>
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty] private Supervisor _selectedSupervisor;

        public ObservableCollection<Pair> PairsSupervised { get; } = new();
        public ObservableCollection<Issue> PendingIssues { get; } = new();
        public ObservableCollection<Issue> ResolvedIssues { get; } = new();

        public SupervisorViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            PairsSupervised.Clear();
            PendingIssues.Clear();
            ResolvedIssues.Clear();

            await Task.Delay(500);

            PairsSupervised.Add(new Pair { Mentor = new Student("Name1")});
            PairsSupervised.Add(new Pair { Mentee = new Student("Name2")});

            PendingIssues.Add(new Issue 
            { 
                Description = "Inconsistent meeting schedule", 
                Category = 1 
            });
            PendingIssues.Add(new Issue 
            { 
                Description = "Communication barrier reported by mentee", 
                Category = 2 
            });

            // 5. Add Dummy Resolved Issues
            ResolvedIssues.Add(new Issue 
            { 
                Description = "Initial goal setting completed", 
                Category = 1 
            });
        }

        [RelayCommand] private async Task Back() => await _navigationService.GoBackAsync();

        public Task OnNavigatedToAsync(int supervisorId)
        {
            LoadSupervisorDataAsync(supervisorId);
            SelectedSupervisor = new Supervisor("Primo");
            return Task.CompletedTask;
        }
    }
}
