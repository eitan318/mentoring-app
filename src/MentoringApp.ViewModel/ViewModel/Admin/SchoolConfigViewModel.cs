using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

/// <summary>
/// Manages the school's grade/class roster. Used both inline on the admin dashboard
/// (step 1 of the onboarding stepper) and inside the System Settings page.
/// </summary>
public partial class SchoolConfigViewModel : ObservableObject
{
    private readonly ReferenceApiClient _referenceClient;
    private readonly AdminProgressStore _progress;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    public ObservableCollection<SchoolClassModel> AllClasses { get; } = [];
    public ObservableCollection<GradeModel> AvailableGrades { get; } = [];

    [ObservableProperty] private GradeModel? _selectedGrade;
    [ObservableProperty] private string _classNumInput = "";
    [ObservableProperty] private SchoolClassModel? _selectedClass;

    public SchoolConfigViewModel(
        ReferenceApiClient referenceClient,
        AdminProgressStore progress,
        IToastService toastService,
        ILocalizationService loc)
    {
        _referenceClient = referenceClient;
        _progress = progress;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task LoadAsync()
    {
        var grades = await _referenceClient.GetGradesAsync();
        AvailableGrades.Clear();
        foreach (var g in grades) AvailableGrades.Add(g);
        await RefreshClassesAsync();
    }

    public async Task RefreshClassesAsync()
    {
        var classes = await _referenceClient.GetSchoolClassesAsync();
        AllClasses.Clear();
        foreach (var c in classes) AllClasses.Add(c);
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
        }
        catch (Exception ex)
        {
            _toastService.Error(ex.Message);
        }
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
}
