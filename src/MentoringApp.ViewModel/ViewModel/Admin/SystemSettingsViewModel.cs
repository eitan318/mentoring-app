using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class SystemSettingsViewModel : ObservableObject, INavigatable
{
    private readonly ReferenceApiClient _referenceClient;
    private readonly SettingsApiClient _settingsClient;
    private readonly INavigationService _navigationService;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    public ObservableCollection<SchoolClassResponse> AllClasses { get; set; } = new();
    public ObservableCollection<GradeResponse> AvailableGrades { get; set; } = new();

    [ObservableProperty] private GradeResponse? _selectedGrade;
    [ObservableProperty] private string _classNumInput = "";
    [ObservableProperty] private SchoolClassResponse? _selectedClass;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanContinueToDashboard))]
    private bool _isFirstTimeSetup;

    public bool CanContinueToDashboard => IsFirstTimeSetup && AllClasses.Count > 0;

    public SystemSettingsViewModel(
        ReferenceApiClient referenceClient,
        SettingsApiClient settingsClient,
        INavigationService navigationService,
        IToastService toastService,
        ILocalizationService loc)
    {
        _referenceClient = referenceClient;
        _settingsClient = settingsClient;
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
            _loc.Format("SysSettings_Confirm_RemoveClass_Body", $"Grade {SelectedClass.GradeId} Class {SelectedClass.ClassNum}"));
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
}
