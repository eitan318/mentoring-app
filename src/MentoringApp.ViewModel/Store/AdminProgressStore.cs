using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ApiClient.Clients;

namespace MentoringApp.ViewModel.Store;

/// <summary>
/// Single source of truth for the admin onboarding progression. Drives both the
/// dashboard sidebar visibility and the overview stepper.
/// </summary>
public partial class AdminProgressStore : ObservableObject
{
    private readonly SettingsApiClient _settingsClient;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSchoolConfigStepActive))]
    [NotifyPropertyChangedFor(nameof(IsImportStepActive))]
    [NotifyPropertyChangedFor(nameof(ShowManageUsers))]
    [NotifyPropertyChangedFor(nameof(ShowSystemSettings))]
    private bool _isSchoolConfigured;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSupervisorAssignmentStepActive))]
    [NotifyPropertyChangedFor(nameof(IsRegistrationStepActive))]
    private bool _isSupervisorsAssigned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImportStepActive))]
    [NotifyPropertyChangedFor(nameof(IsSupervisorAssignmentStepActive))]
    [NotifyPropertyChangedFor(nameof(IsRegistrationStepActive))]
    private bool _isUsersImported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistrationStepActive))]
    [NotifyPropertyChangedFor(nameof(IsMentorSelectionStepActive))]
    [NotifyPropertyChangedFor(nameof(ShowManagePairs))]
    private bool _isSelectionPhaseActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistrationStepActive))]
    [NotifyPropertyChangedFor(nameof(IsMentorSelectionStepActive))]
    [NotifyPropertyChangedFor(nameof(ShowManagePairs))]
    private bool _isProcessComplete;

    public bool IsSchoolConfigStepActive       => !IsSchoolConfigured;
    public bool IsImportStepActive => !IsUsersImported && IsSchoolConfigured;
    public bool IsSupervisorAssignmentStepActive => IsUsersImported && !IsSupervisorsAssigned;
    public bool IsRegistrationStepActive       => IsSupervisorsAssigned && !IsSelectionPhaseActive && !IsProcessComplete;
    public bool IsMentorSelectionStepActive    => IsSelectionPhaseActive && !IsProcessComplete;

    public bool ShowManageUsers    => IsSchoolConfigured;
    public bool ShowManagePairs    => IsSelectionPhaseActive || IsProcessComplete;
    public bool ShowSystemSettings => IsSchoolConfigured;

    public AdminProgressStore(SettingsApiClient settingsClient)
    {
        _settingsClient = settingsClient;
    }

    public async Task RefreshAsync()
    {
        var settings = await _settingsClient.GetAllAsync();
        IsSchoolConfigured     = settings.IsSchoolConfigured;
        IsSupervisorsAssigned  = settings.IsSupervisorsAssigned;
        IsUsersImported        = settings.IsUsersImported;
        IsSelectionPhaseActive = settings.IsPhase1Complete;
        IsProcessComplete      = settings.IsProcessComplete;
    }

    public async Task MarkSchoolConfiguredAsync()
    {
        if (IsSchoolConfigured) return;
        await _settingsClient.SetIsSchoolConfiguredAsync(true);
        IsSchoolConfigured = true;
    }

    public async Task MarkUsersImportedAsync()
    {
        if (IsUsersImported) return;
        await _settingsClient.SetIsUsersImportedAsync(true);
        IsUsersImported = true;
    }

    public async Task MarkSupervisorsAssignedAsync()
    {
        if (IsSupervisorsAssigned) return;
        await _settingsClient.SetIsSupervisorsAssignedAsync(true);
        IsSupervisorsAssigned = true;
    }

    public async Task MarkSelectionPhaseActiveAsync()
    {
        if (IsSelectionPhaseActive) return;
        await _settingsClient.SetIsPhase1CompleteAsync(true);
        IsSelectionPhaseActive = true;
    }

    public async Task MarkProcessCompleteAsync()
    {
        if (IsProcessComplete) return;
        await _settingsClient.SetIsProcessCompleteAsync(true);
        IsProcessComplete = true;
    }
}
