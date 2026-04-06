using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using MentoringApp.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    /// <summary>A checkable wrapper around a SchoolClass for the supervisor assignment UI.</summary>
    public class SelectableSchoolClass : ObservableObject
    {
        private bool _isSelected;
        public SchoolClass Class { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public SelectableSchoolClass(SchoolClass c, bool selected = false) { Class = c; IsSelected = selected; }
    }
    public partial class ManageUsersViewModel : ObservableObject, INavigatable
    {
        private readonly IFileService _fileService;
        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;
        private readonly UserService _userService;
        private readonly PairService _pairService;
        private readonly ExcelImportService _excelImportService;
        private readonly GradeService _gradeService;
        private readonly SchoolClassService _schoolClassService;

        // ── Navigation commands ─────────────────────────────────────────────
        [RelayCommand] private async Task RegisterStudent()
            => await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(false);

        [RelayCommand] private async Task RegisterSupervisor()
            => await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(true);

        // ── User list ───────────────────────────────────────────────────────
        public ObservableCollection<UserModel> AllUsers { get; set; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(ViewUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveSupervisorClassCommand))]
        private UserModel? _selectedUser;

        private bool HasSelectedUser => SelectedUser != null;

        // ── Filter Bar — Row 1 ──────────────────────────────────────────────
        [ObservableProperty] private string _searchText = string.Empty;

        // Options: "All", "Supervisor", "Student"
        [ObservableProperty] private string _selectedRole = "All";

        public static IReadOnlyList<string> RoleOptions { get; } = ["All", "Supervisor", "Student"];

        // True when the student-specific panel should be visible
        public bool IsStudentFilterVisible => SelectedRole == "Student";

        // ── Edit Supervisor — Checkable class list ──────────────────────────
        /// <summary>All system-defined school classes with selection state for the current supervisor.</summary>
        public ObservableCollection<SelectableSchoolClass> SelectableClasses { get; set; } = [];
        public ObservableCollection<Grade> AllGrades { get; set; } = [];

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task SaveSupervisorClass()
        {
            if (SelectedUser is not SupervisorModel supervisor) return;
            var selected = SelectableClasses.Where(c => c.IsSelected).Select(c => c.Class.Id).ToList();
            var res = await _schoolClassService.SetSupervisorClassesAsync(supervisor.Id, selected);
            if (res.Success)
            {
                // Refresh in-memory model
                var refreshed = await _userService.GetUserByIdAsync(supervisor.Id);
                if (refreshed.Success && refreshed.Data is SupervisorModel updated)
                {
                    var idx = AllUsers.IndexOf(SelectedUser);
                    if (idx >= 0) AllUsers[idx] = updated;
                    SelectedUser = updated;
                }
                OnPropertyChanged(nameof(FilteredUsers));
                MessageBox.Show("Supervisor classes saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Failed: {res.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        partial void OnSelectedUserChanged(UserModel? value)
        {
            SelectableClasses.Clear();
            if (value is SupervisorModel supervisor)
            {
                var assignedIds = supervisor.AssignedClasses.Select(c => c.Id).ToHashSet();
                // Populate from the full list loaded in OnNavigatedToAsync
                foreach (var sc in _allSchoolClasses)
                {
                    SelectableClasses.Add(new SelectableSchoolClass(sc, assignedIds.Contains(sc.Id)));
                }
            }
        }

        private List<SchoolClass> _allSchoolClasses = new();

        // ── Filter Bar — Row 2 (Student-specific) ───────────────────────────
        [ObservableProperty] private bool _filterIsMentor;
        [ObservableProperty] private bool _filterIsMentee;
        [ObservableProperty] private bool _filterUnpairedOnly;

        // Grade bounds (nullable int, null means "no bound")
        [ObservableProperty] private string? _filterMinGrade;
        [ObservableProperty] private string? _filterMaxGrade;

        public ObservableCollection<string> AvailableGrades { get; set; } = [];

        // ── Pairing state ───────────────────────────────────────────────────
        private HashSet<int> _pairedUserIds = [];

        // ── Filtered list ───────────────────────────────────────────────────
        public IEnumerable<UserModel> FilteredUsers
        {
            get
            {
                var q = AllUsers.AsEnumerable();

                // 1. Text search
                if (!string.IsNullOrWhiteSpace(SearchText))
                    q = q.Where(u =>
                        u.UserName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase));

                // 2. Role filter
                q = SelectedRole switch
                {
                    "Supervisor" => q.OfType<UserModel>().Where(u => u is SupervisorModel),
                    "Student"    => q.OfType<UserModel>().Where(u => u is StudentModel),
                    _            => q
                };

                // 3. Student-specific filters (only when role == Student)
                if (SelectedRole == "Student")
                {
                    // Profile: if either is checked enforce it (both checked = must be both)
                    if (FilterIsMentor || FilterIsMentee)
                    {
                        q = q.Where(u =>
                        {
                            if (u is not StudentModel s) return false;
                            if (FilterIsMentor && FilterIsMentee) return s.IsMentor && s.IsMentee;
                            if (FilterIsMentor) return s.IsMentor;
                            return s.IsMentee;
                        });
                    }

                    // Unpaired only
                    if (FilterUnpairedOnly)
                        q = q.Where(u => !_pairedUserIds.Contains(u.Id));

                    // Grade range
                    if (!string.IsNullOrEmpty(FilterMinGrade) && int.TryParse(FilterMinGrade, out int min))
                        q = q.Where(u => u is StudentModel s && s.Grade.Num >= min);

                    if (!string.IsNullOrEmpty(FilterMaxGrade) && int.TryParse(FilterMaxGrade, out int max))
                        q = q.Where(u => u is StudentModel s && s.Grade.Num <= max);
                }

                return q;
            }
        }

        // Invalidate the list whenever any filter property changes
        partial void OnSearchTextChanged(string v)       => OnPropertyChanged(nameof(FilteredUsers));
        partial void OnSelectedRoleChanged(string v)     { OnPropertyChanged(nameof(IsStudentFilterVisible)); OnPropertyChanged(nameof(FilteredUsers)); }
        partial void OnFilterIsMentorChanged(bool v)     => OnPropertyChanged(nameof(FilteredUsers));
        partial void OnFilterIsMenteeChanged(bool v)     => OnPropertyChanged(nameof(FilteredUsers));
        partial void OnFilterUnpairedOnlyChanged(bool v) => OnPropertyChanged(nameof(FilteredUsers));
        partial void OnFilterMinGradeChanged(string? v)  => OnPropertyChanged(nameof(FilteredUsers));
        partial void OnFilterMaxGradeChanged(string? v)  => OnPropertyChanged(nameof(FilteredUsers));

        // ── Import popup state ──────────────────────────────────────────────
        [ObservableProperty] private bool _isStudentInfoVisible;
        [ObservableProperty] private bool _isSupervisorInfoVisible;

        [RelayCommand] private void ToggleStudentInfo()    => IsStudentInfoVisible    = !IsStudentInfoVisible;
        [RelayCommand] private void ToggleSupervisorInfo() => IsSupervisorInfoVisible = !IsSupervisorInfoVisible;
        [RelayCommand] private void CloseStudentInfo()     => IsStudentInfoVisible    = false;
        [RelayCommand] private void CloseSupervisorInfo()  => IsSupervisorInfoVisible = false;

        // ── Constructor ─────────────────────────────────────────────────────
        public ManageUsersViewModel(
            IFileService fileService,
            IWindowService windowService,
            INavigationService navigationService,
            UserService userService,
            PairService pairService,
            ExcelImportService excelImportService,
            GradeService gradeService,
            SchoolClassService schoolClassService)
        {
            _fileService        = fileService;
            _windowService      = windowService;
            _navigationService  = navigationService;
            _userService        = userService;
            _pairService        = pairService;
            _excelImportService = excelImportService;
            _gradeService       = gradeService;
            _schoolClassService = schoolClassService;
        }

        public async Task OnNavigatedToAsync()
        {
            // Load school classes first
            var classRes = await _schoolClassService.GetAllAsync();
            _allSchoolClasses = classRes.Success && classRes.Data != null ? classRes.Data.ToList() : new();

            // Load users
            var users = await _userService.GetAllUsersAsync();
            AllUsers = new(users);

            // Populate paired-user IDs
            var pairsResult = await _pairService.GetAllPairsAsync();
            if (pairsResult.Success && pairsResult.Data != null)
            {
                _pairedUserIds = pairsResult.Data
                    .SelectMany(p => new[] { p.Mentor.Id, p.Mentee.Id })
                    .ToHashSet();
            }

            // Build grade list from students (sorted by Num)
            var grades = AllUsers.OfType<StudentModel>()
                                 .Select(s => s.Grade)
                                 .DistinctBy(g => g.Num)
                                 .OrderBy(g => g.Num)
                                 .Select(g => g.Num.ToString())
                                 .ToList();

            var gradeRes = await _gradeService.GetAllGradesAsync();
            if (gradeRes.Success && gradeRes.Data != null)
            {
                AllGrades.Clear();
                foreach (var g in gradeRes.Data) AllGrades.Add(g);
            }

            AvailableGrades.Clear();
            foreach (var g in grades)
                AvailableGrades.Add(g);

            FilterMinGrade = null;
            FilterMaxGrade = null;

            OnPropertyChanged(nameof(FilteredUsers));
        }

        // ── Delete / View ───────────────────────────────────────────────────
        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task DeleteUser()
        {
            if (SelectedUser is null) return;
            await _userService.DeleteUserAsync(SelectedUser.Id);
            AllUsers.Remove(SelectedUser);
            SelectedUser = null;
            OnPropertyChanged(nameof(FilteredUsers));
        }

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task ViewUser()
        {
            if (SelectedUser != null)
                await _navigationService.NavigateToAsync<OtherProfileViewModel, int>(SelectedUser.Id);
        }

        // ── Excel import ────────────────────────────────────────────────────
        [RelayCommand]
        private async Task ImportStudents()
        {
            IsStudentInfoVisible = false;
            string? file = _fileService.OpenFile("Excel files (*.xlsx)|*.xlsx");
            if (string.IsNullOrEmpty(file)) return;

            var result = await _excelImportService.ImportStudentsFromExcelAsync(file);
            if (result.Success)
            {
                MessageBox.Show($"Successfully imported {result.Data} student(s).",
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                await OnNavigatedToAsync();
            }
            else
                MessageBox.Show($"Import failed: {result.ErrorMessage}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        [RelayCommand]
        private async Task ImportSupervisors()
        {
            IsSupervisorInfoVisible = false;
            string? file = _fileService.OpenFile("Excel files (*.xlsx)|*.xlsx");
            if (string.IsNullOrEmpty(file)) return;

            var result = await _excelImportService.ImportSupervisorsFromExcelAsync(file);
            if (result.Success)
            {
                MessageBox.Show($"Successfully imported {result.Data} supervisor(s).",
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                await OnNavigatedToAsync();
            }
            else
                MessageBox.Show($"Import failed: {result.ErrorMessage}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ── Template download ───────────────────────────────────────────────
        [RelayCommand]
        private void DownloadStudentTemplate()
        {
            string? savePath = _fileService.SaveFile("Excel files (*.xlsx)|*.xlsx", "students_import_template.xlsx");
            if (string.IsNullOrEmpty(savePath)) return;
            var result = _excelImportService.GenerateTemplate(isSupervisor: false, savePath);
            if (result.Success)
                MessageBox.Show("Template saved successfully.", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"Could not save template: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        [RelayCommand]
        private void DownloadSupervisorTemplate()
        {
            string? savePath = _fileService.SaveFile("Excel files (*.xlsx)|*.xlsx", "supervisors_import_template.xlsx");
            if (string.IsNullOrEmpty(savePath)) return;
            var result = _excelImportService.GenerateTemplate(isSupervisor: true, savePath);
            if (result.Success)
                MessageBox.Show("Template saved successfully.", "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"Could not save template: {result.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
