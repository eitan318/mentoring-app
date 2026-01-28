using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Auth;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Supervisor
{
    public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
    {
        protected readonly INavigationService _navigationService;

        [ObservableProperty] private Model.Supervisor? _selectedSupervisor;

        public ObservableCollection<Pair> PairsSupervised { get; } = [];
        public ObservableCollection<Issue> PendingIssues { get; } = [];
        public ObservableCollection<Issue> ResolvedIssues { get; } = [];

        public SupervisorDashboardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            PairsSupervised.Clear();
            PendingIssues.Clear();
            ResolvedIssues.Clear();

            await Task.Delay(500);

            PairsSupervised.Add(new Pair { Mentor = new Model.Student("Name1")});
            PairsSupervised.Add(new Pair { Mentee = new Model.Student("Name2")});

            PendingIssues.Add(new Issue 
            { 
                Description = "Inconsistent meeting schedule", 
                Category = new IssueCategory("Sigma")
            });
            PendingIssues.Add(new Issue 
            { 
                Description = "Communication barrier reported by mentee", 
                Category = new IssueCategory("Sigma")
            });

            ResolvedIssues.Add(new Issue 
            { 
                Description = "Initial goal setting completed", 
                Category = new IssueCategory("Sigma") 
            });
        }

        [RelayCommand] private async Task Back() => await _navigationService.GoBackAsync();


        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            await LoadSupervisorDataAsync(supervisorId);
            SelectedSupervisor = new Model.Supervisor("Primo");
        }
    }
}
