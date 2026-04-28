using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public class SupervisorSlot : ObservableObject
{
    public UserModel Supervisor { get; }
    public ObservableCollection<SchoolClassModel> AssignedClasses { get; } = [];
    public string DisplayName => Supervisor.UserName;
    public SupervisorSlot(UserModel supervisor) { Supervisor = supervisor; }
}

public partial class SystemSettingsViewModel : ObservableObject, INavigatable
{
    private readonly ReferenceApiClient _referenceClient;
    private readonly SettingsApiClient _settingsClient;
    private readonly UserApiClient _userClient;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    public ObservableCollection<SchoolClassModel> AllClasses { get; set; } = new();
    public ObservableCollection<GradeModel> AvailableGrades { get; set; } = new();
    public ObservableCollection<SupervisorSlot> SupervisorSlots { get; } = [];
    public ObservableCollection<SchoolClassModel> UnassignedClasses { get; } = [];

    [ObservableProperty] private GradeModel? _selectedGrade;
    [ObservableProperty] private string _classNumInput = "";
    [ObservableProperty] private SchoolClassModel? _selectedClass;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinueToDashboard))]
    private bool _isFirstTimeSetup;

    [ObservableProperty]
    private bool _isSchoolConfigTabSelected = true;

    public bool CanContinueToDashboard => IsFirstTimeSetup && AllClasses.Count > 0;

    public SystemSettingsViewModel(
        ReferenceApiClient referenceClient,
        SettingsApiClient settingsClient,
        UserApiClient userClient,
        INavigationService navigationService,
        IToastService toastService,
        ILocalizationService loc)
    {
        _referenceClient = referenceClient;
        _settingsClient = settingsClient;
        _userClient = userClient;
        _navigationService = navigationService;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync()
    {
        var settings = await _settingsClient.GetAllAsync();
        IsFirstTimeSetup = !settings.IsSchoolConfigured;

        var grades = await _referenceClient.GetGradesAsync();
        AvailableGrades.Clear();
        foreach (var g in grades) AvailableGrades.Add(g);

        await RefreshClassesAsync();
    }

    private async Task RefreshClassesAsync()
    {
        var classes = await _referenceClient.GetSchoolClassesAsync();
        AllClasses.Clear();
        foreach (var c in classes) AllClasses.Add(c);
        OnPropertyChanged(nameof(CanContinueToDashboard));
        await RefreshSupervisorAssignmentsAsync();
    }

    private async Task RefreshSupervisorAssignmentsAsync()
    {
        SupervisorSlots.Clear();
        UnassignedClasses.Clear();

        var supervisors = (await _userClient.GetAllAsync())
            .Where(u => u.IsSupervisor)
            .ToList();

        var classAssignments = await Task.WhenAll(
            supervisors.Select(s => _referenceClient.GetSchoolClassesBySupervisorAsync(s.Id)));

        var assignedIds = new HashSet<int>();
        for (int i = 0; i < supervisors.Count; i++)
        {
            var slot = new SupervisorSlot(supervisors[i]);
            foreach (var c in classAssignments[i])
            {
                slot.AssignedClasses.Add(c);
                assignedIds.Add(c.Id);
            }
            SupervisorSlots.Add(slot);
        }

        foreach (var c in AllClasses.Where(c => !assignedIds.Contains(c.Id)))
            UnassignedClasses.Add(c);
    }

    public async Task MoveClassAsync(SchoolClassModel cls, SupervisorSlot? fromSlot, SupervisorSlot toSlot)
    {
        if (fromSlot == toSlot) return;

        try
        {
            if (fromSlot == null)
                UnassignedClasses.Remove(cls);
            else
                fromSlot.AssignedClasses.Remove(cls);

            toSlot.AssignedClasses.Add(cls);

            await _userClient.UpdateSupervisorClassesAsync(
                toSlot.Supervisor.Id,
                new UpdateSupervisorClassesRequest(toSlot.AssignedClasses.Select(c => c.Id)));

            if (fromSlot != null)
                await _userClient.UpdateSupervisorClassesAsync(
                    fromSlot.Supervisor.Id,
                    new UpdateSupervisorClassesRequest(fromSlot.AssignedClasses.Select(c => c.Id)));
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
            await RefreshSupervisorAssignmentsAsync();
        }
    }

    public async Task MoveClassToPoolAsync(SchoolClassModel cls, SupervisorSlot fromSlot)
    {
        try
        {
            fromSlot.AssignedClasses.Remove(cls);
            UnassignedClasses.Add(cls);

            await _userClient.UpdateSupervisorClassesAsync(
                fromSlot.Supervisor.Id,
                new UpdateSupervisorClassesRequest(fromSlot.AssignedClasses.Select(c => c.Id)));
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
            await RefreshSupervisorAssignmentsAsync();
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

        try
        {
            await _referenceClient.AddSchoolClassAsync(new AddSchoolClassRequest(SelectedGrade.Id, num));
            ClassNumInput = "";
            await RefreshClassesAsync();

            var settings = await _settingsClient.GetAllAsync();
            if (!settings.IsSchoolConfigured)
                await _settingsClient.SetIsSchoolConfiguredAsync(true);
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task GoToDashboard() => await _navigationService.GoBackAsync();

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
            _loc.Format("SysSettings_Confirm_RemoveClass_Body", $"Grade {SelectedClass.Grade.Id} Class {SelectedClass.ClassNum}"));
        if (!confirmed) return;

        try
        {
            await _referenceClient.DeleteSchoolClassAsync(SelectedClass.Id);
            SelectedClass = null;
            await RefreshClassesAsync();
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private void SelectTab(string parameter)
    {
        if (parameter == "0")
            IsSchoolConfigTabSelected = true;
        else if (parameter == "1")
            IsSchoolConfigTabSelected = false;
    }
}
