using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Supervisor
{
    public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
    {
        protected readonly INavigationService _navigationService;

        [ObservableProperty] private Model.Supervisor? _selectedSupervisor;

        public ObservableCollection<Pair> PairsSupervised { get; } = [];
        public ObservableCollection<Issue> AllIssues { get; } = [];

        public SupervisorDashboardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task SelectIssue(Issue issue)
        {
            await _navigationService.NavigateToAsync<IssueViewModel, int>(issue.Id);
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            PairsSupervised.Clear();
            AllIssues.Clear();

            PairsSupervised.Add(new Pair { Mentor = new Model.Student("Name1")});
            PairsSupervised.Add(new Pair { Mentee = new Model.Student("Name2")});

            AllIssues.Add(new Issue("Inconsistent meeting schedulee", new IssueCategory("Sigma"), true));
            AllIssues.Add(new Issue("Communication barrier reported by mentee", new IssueCategory("Sigma"), true));
            AllIssues.Add(new Issue("Communication barrier reported by mentee", new IssueCategory("Sigma"), false));
            AllIssues.Add(new Issue("Communication barrier reported by mentee", new IssueCategory("Sigma"), false));
        }

        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            await LoadSupervisorDataAsync(supervisorId);
            SelectedSupervisor = new Model.Supervisor("Primo");
        }
    }
}
