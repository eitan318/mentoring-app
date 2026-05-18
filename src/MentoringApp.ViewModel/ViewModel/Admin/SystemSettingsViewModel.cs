using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
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
    private readonly AdminProgressStore _progress;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    public SchoolConfigViewModel SchoolConfig { get; }

    public ObservableCollection<SupervisorSlot> SupervisorSlots { get; } = [];
    public ObservableCollection<SchoolClassModel> UnassignedClasses { get; } = [];

    public bool AllClassesAssigned => UnassignedClasses.Count == 0;

    [ObservableProperty]
    private bool _isSchoolConfigTabSelected = true;

    public SystemSettingsViewModel(
        ReferenceApiClient referenceClient,
        SettingsApiClient settingsClient,
        UserApiClient userClient,
        AdminProgressStore progress,
        SchoolConfigViewModel schoolConfig,
        INavigationService navigationService,
        IToastService toastService,
        ILocalizationService loc)
    {
        _referenceClient = referenceClient;
        _settingsClient = settingsClient;
        _userClient = userClient;
        _progress = progress;
        SchoolConfig = schoolConfig;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync()
    {
        await SchoolConfig.LoadAsync();
        await RefreshSupervisorAssignmentsAsync();
    }

    public async Task RefreshSupervisorAssignmentsAsync()
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

        foreach (var c in SchoolConfig.AllClasses.Where(c => !assignedIds.Contains(c.Id)))
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
    private void SelectTab(string parameter)
    {
        if (parameter == "0")
            IsSchoolConfigTabSelected = true;
        else if (parameter == "1")
            IsSchoolConfigTabSelected = false;
    }

    [RelayCommand]
    private async Task AdvanceYear()
    {
        bool confirmed = await _toastService.ConfirmAsync(
            _loc.Get("SysSettings_AdvanceYear_Confirm_Title"),
            _loc.Get("SysSettings_AdvanceYear_Confirm_Body"));
        if (!confirmed) return;

        try
        {
            await _settingsClient.AdvanceYearAsync();
            await _progress.RefreshAsync();
            _toastService.Success(_loc.Get("SysSettings_AdvanceYear_Success"));
            await OnNavigatedToAsync();
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
        }
    }

}
