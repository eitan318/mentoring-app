using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.Windows;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class SystemSettingsViewModel : ObservableObject, INavigatable
    {
        private readonly SchoolClassService _schoolClassService;
        private readonly GradeService _gradeService;

        public ObservableCollection<SchoolClass> AllClasses { get; set; } = new();
        public ObservableCollection<Grade> AvailableGrades { get; set; } = new();

        [ObservableProperty]
        private Grade? _selectedGrade;

        [ObservableProperty]
        private string _classNumInput = "";

        [ObservableProperty]
        private SchoolClass? _selectedClass;

        public SystemSettingsViewModel(SchoolClassService schoolClassService, GradeService gradeService)
        {
            _schoolClassService = schoolClassService;
            _gradeService = gradeService;
        }

        public async Task OnNavigatedToAsync()
        {
            var grades = await _gradeService.GetAllGradesAsync();
            if (grades.Success && grades.Data != null)
            {
                AvailableGrades.Clear();
                foreach (var g in grades.Data) AvailableGrades.Add(g);
            }
            await RefreshClassesAsync();
        }

        private async Task RefreshClassesAsync()
        {
            var result = await _schoolClassService.GetAllAsync();
            if (result.Success && result.Data != null)
            {
                AllClasses.Clear();
                foreach (var c in result.Data) AllClasses.Add(c);
            }
        }

        [RelayCommand]
        private async Task AddClass()
        {
            if (SelectedGrade == null)
            {
                MessageBox.Show("Please select a grade first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(ClassNumInput, out int num) || num <= 0)
            {
                MessageBox.Show("Please enter a valid positive class number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _schoolClassService.AddClassAsync(SelectedGrade.Id, num);
            if (result.Success)
            {
                ClassNumInput = "";
                await RefreshClassesAsync();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task DeleteClass()
        {
            if (SelectedClass == null)
            {
                MessageBox.Show("Please select a class from the list to remove.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show(
                $"Remove {SelectedClass.DisplayName} from the system? Any supervisors assigned to this class will lose the assignment.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var result = await _schoolClassService.DeleteClassAsync(SelectedClass.Id);
            if (result.Success)
            {
                SelectedClass = null;
                await RefreshClassesAsync();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
