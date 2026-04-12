using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModel.Admin
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
        private readonly IToastService _toastService;
        private readonly ILocalizationService _loc;
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
        [NotifyPropertyChangedFor(nameof(IsSupervisorSelected))]
        private UserModel? _selectedUser;

        public bool IsSupervisorSelected => SelectedUser is SupervisorModel;

        private bool HasSelectedUser => SelectedUser != null;

        // ── Filter Bar — Row 1 ──────────────────────────────────────────────
        [ObservableProperty] private string _searchText = string.Empty;

        // Options: "All", "Supervisor", "Student"
        [ObservableProperty] private string _selectedRole = "All";

        public static IReadOnlyList<string> RoleOptions { get; } = ["All", "Supervisor", "Student"];

        // True when the student-specific panel should be visible
        public bool IsStudentFilterVisible => SelectedRole == "Student";

        // ── Edit Supervisor — Multi-class assignment ────────────────────────
        [ObservableProperty] private string _supervisorClassSaveStatus = "";
        [ObservableProperty] private bool _supervisorClassSaveIsError;

        public ObservableCollection<Grade> AllGrades { get; set; } = [];

        /// <summary>
        /// Checkable class list for the selected supervisor.
        /// Only contains classes not assigned to any OTHER supervisor,
        /// plus any classes already held by this supervisor.
        /// </summary>
        public ObservableCollection<SelectableSchoolClass> SelectableClasses { get; } = [];

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task SaveSupervisorClass()
        {
            if (SelectedUser is not SupervisorModel supervisor) return;
            var ids = SelectableClasses.Where(c => c.IsSelected).Select(c => c.Class.Id).ToList();
            var res = await _schoolClassService.SetSupervisorClassesAsync(supervisor.Id, ids);
            if (res.Success)
            {
                var refreshed = await _userService.GetUserByIdAsync(supervisor.Id);
                if (refreshed.Success && refreshed.Data is SupervisorModel updated)
                {
                    var idx = AllUsers.IndexOf(SelectedUser);
                    if (idx >= 0) AllUsers[idx] = updated;
                    SelectedUser = updated;
                }
                OnPropertyChanged(nameof(FilteredUsers));
                SupervisorClassSaveIsError = false;
                SupervisorClassSaveStatus = "✓ Saved";
                _ = Task.Delay(3000).ContinueWith(_ => SupervisorClassSaveStatus = "",
                    System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                SupervisorClassSaveIsError = true;
                SupervisorClassSaveStatus = $"✗ {res.ErrorMessage}";
            }
        }

        partial void OnSelectedUserChanged(UserModel? value)
        {
            SelectableClasses.Clear();
            SupervisorClassSaveStatus = "";
            SupervisorClassSaveIsError = false;
            if (value is not SupervisorModel supervisor) return;

            // Class IDs already assigned to OTHER supervisors (off-limits)
            var takenByOthers = AllUsers
                .OfType<SupervisorModel>()
                .Where(s => s.Id != supervisor.Id)
                .SelectMany(s => s.AssignedClasses.Select(c => c.Id))
                .ToHashSet();

            var assignedToThis = supervisor.AssignedClasses.Select(c => c.Id).ToHashSet();

            foreach (var sc in _allSchoolClasses.Where(sc => !takenByOthers.Contains(sc.Id)))
                SelectableClasses.Add(new SelectableSchoolClass(sc, assignedToThis.Contains(sc.Id)));
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
        /// <summary>
        /// Computed property that applies up to four filter layers on demand:
        /// (1) free-text search on UserName/Email,
        /// (2) role type (All / Supervisor / Student),
        /// (3) student profile flags (IsMentor, IsMentee — AND when both checked),
        /// (4) grade number range.
        /// Each filter property setter calls <see cref="OnPropertyChanged"/> for this property
        /// to trigger re-evaluation; no separate ObservableCollection copy is maintained.
        /// </summary>
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
                    // Both checked = must satisfy both roles simultaneously (mentor AND mentee)
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
            IToastService toastService,
            ILocalizationService loc,
            UserService userService,
            PairService pairService,
            ExcelImportService excelImportService,
            GradeService gradeService,
            SchoolClassService schoolClassService)
        {
            _fileService        = fileService;
            _windowService      = windowService;
            _navigationService  = navigationService;
            _toastService       = toastService;
            _loc                = loc;
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
            bool confirmed = await _toastService.ConfirmAsync(
                _loc.Get("Confirm_DeleteUser_Title"),
                _loc.Get("Confirm_DeleteUser_Body"));
            if (!confirmed) return;

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
                _toastService.Success(_loc.Format("ManageUsers_ImportStudents_Success", result.Data));
                await OnNavigatedToAsync();
            }
            else
                _toastService.Error(_loc.Format("ManageUsers_ImportStudents_Error", result.ErrorMessage ?? ""));
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
                _toastService.Success(_loc.Format("ManageUsers_ImportSupervisors_Success", result.Data));
                await OnNavigatedToAsync();
            }
            else
                _toastService.Error(_loc.Format("ManageUsers_ImportSupervisors_Error", result.ErrorMessage ?? ""));
        }

        // ── Template download ───────────────────────────────────────────────
        [RelayCommand]
        private void DownloadStudentTemplate()
        {
            string? savePath = _fileService.SaveFile("Excel files (*.xlsx)|*.xlsx", "students_import_template.xlsx");
            if (string.IsNullOrEmpty(savePath)) return;
            var result = _excelImportService.GenerateTemplate(isSupervisor: false, savePath);
            if (result.Success)
                _toastService.Success(_loc.Get("ManageUsers_TemplateSaved_Success"));
            else
                _toastService.Error(_loc.Format("ManageUsers_TemplateSaved_Error", result.ErrorMessage ?? ""));
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
