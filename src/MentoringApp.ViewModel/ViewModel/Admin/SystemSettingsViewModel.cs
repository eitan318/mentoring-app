using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin
{
    public partial class SystemSettingsViewModel : ObservableObject, INavigatable
    {
        private readonly SchoolClassService _schoolClassService;
        private readonly GradeService _gradeService;
        private readonly SettingsService _settingsService;
        private readonly INavigationService _navigationService;
        private readonly IToastService _toastService;
        private readonly ILocalizationService _loc;

        public ObservableCollection<SchoolClass> AllClasses { get; set; } = new();
        public ObservableCollection<Grade> AvailableGrades { get; set; } = new();

        [ObservableProperty]
        private Grade? _selectedGrade;

        [ObservableProperty]
        private string _classNumInput = "";

        [ObservableProperty]
        private SchoolClass? _selectedClass;

        /// <summary>True when the admin has not yet completed the initial school configuration.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanContinueToDashboard))]
        private bool _isFirstTimeSetup;

        public bool CanContinueToDashboard => IsFirstTimeSetup && AllClasses.Count > 0;

        public SystemSettingsViewModel(SchoolClassService schoolClassService, GradeService gradeService, SettingsService settingsService, INavigationService navigationService, IToastService toastService, ILocalizationService loc)
        {
            _schoolClassService = schoolClassService;
            _gradeService = gradeService;
            _settingsService = settingsService;
            _navigationService = navigationService;
            _toastService = toastService;
            _loc = loc;
        }

        public async Task OnNavigatedToAsync()
        {
            IsFirstTimeSetup = !await _settingsService.GetIsSchoolConfiguredAsync();

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
                _toastService.Warning(_loc.Get("SysSettings_Validation_SelectGrade"));
                return;
            }
            if (!int.TryParse(ClassNumInput, out int num) || num <= 0)
            {
                _toastService.Warning(_loc.Get("SysSettings_Validation_InvalidClassNum"));
                return;
            }

            var result = await _schoolClassService.AddClassAsync(SelectedGrade.Id, num);
            if (result.Success)
            {
                ClassNumInput = "";
                await RefreshClassesAsync();
                if (!await _settingsService.GetIsSchoolConfiguredAsync())
                    await _settingsService.SetIsSchoolConfiguredAsync(true);
                OnPropertyChanged(nameof(CanContinueToDashboard));
            }
            else
            {
                _toastService.Error(result.ErrorMessage ?? "");
            }
        }

        [RelayCommand]
        private async Task GoToDashboard()
        {
            await _navigationService.GoBackAsync();
        }

        [RelayCommand]
        private async Task DeleteClass()
        {
            if (SelectedClass == null)
            {
                _toastService.Warning(_loc.Get("SysSettings_Validation_SelectClass"));
                return;
            }

            bool confirmed = await _toastService.ConfirmAsync(
                _loc.Get("SysSettings_Confirm_RemoveClass_Title"),
                _loc.Format("SysSettings_Confirm_RemoveClass_Body", SelectedClass.DisplayName));
            if (!confirmed) return;

            var result = await _schoolClassService.DeleteClassAsync(SelectedClass.Id);
            if (result.Success)
            {
                SelectedClass = null;
                await RefreshClassesAsync();
            }
            else
            {
                _toastService.Error(result.ErrorMessage ?? "");
            }
        }
    }
}
