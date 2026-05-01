using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MentoringApp.Web.Services;

/// <summary>
/// Blazor implementation of INavigationService.
/// Translates ViewModel-based navigation calls into URL-based Blazor route navigation.
/// </summary>
public class WebNavigationService : INavigationService
{
    private readonly NavigationManager _navManager;
    private readonly NavigationParameterStore _paramStore;

    private static readonly Dictionary<Type, string> _routes = new()
    {
        [typeof(LoginViewModel)] = "/login",
        [typeof(AuthenticatedDashboardViewModel)] = "/authenticated",
        [typeof(StudentDashboardViewModel)] = "/student",
        [typeof(SupervisorDashboardViewModel)] = "/supervisor",
        [typeof(AdminDashboardViewModel)] = "/admin",
        [typeof(MyProfileViewModel)] = "/profile",
        [typeof(OtherProfileViewModel)] = "/profile/other",
        [typeof(AddIssueViewModel)] = "/add-issue",
        [typeof(AddReviewViewModel)] = "/add-review",
        [typeof(IssueViewModel)] = "/issue",
    };

    private readonly IJSRuntime? _js;

    public WebNavigationService(NavigationManager navManager, NavigationParameterStore paramStore, IJSRuntime js)
    {
        _navManager = navManager;
        _paramStore = paramStore;
        _js = js;
    }

    public Task NavigateToAsync<TViewModel>() where TViewModel : class, INavigatable
    {
        if (_routes.TryGetValue(typeof(TViewModel), out var url))
            _navManager.NavigateTo(url);
        return Task.CompletedTask;
    }

    public Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : class, INavigatable<TParameter>
    {
        _paramStore.Set(typeof(TViewModel), parameter!);
        if (_routes.TryGetValue(typeof(TViewModel), out var url))
            _navManager.NavigateTo(url);
        return Task.CompletedTask;
    }

    public IDisposable UseContext(Action<INavigatable> contextSetter)
    {
        // In Blazor, navigation context is the URL itself — each route maps to its own page.
        return new EmptyDisposable();
    }

    public async Task GoBackAsync()
    {
        // Browser-history back; falls back to /student if there's nothing to pop.
        if (_js != null)
        {
            try { await _js.InvokeVoidAsync("history.back"); return; }
            catch { /* prerender or no JS — fall through */ }
        }
        _navManager.NavigateTo("/student");
    }

    public bool CanGoBack() => true;

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Holds parameters for parameterized VM navigation (e.g. NavigateToAsync&lt;VM, int&gt;(id))
/// across the URL hop. Pages read the parameter on initialization.
/// </summary>
public class NavigationParameterStore
{
    private readonly Dictionary<Type, object> _pending = new();

    public void Set(Type vmType, object value) => _pending[vmType] = value;

    public T? Take<T>(Type vmType)
    {
        if (_pending.TryGetValue(vmType, out var value))
        {
            _pending.Remove(vmType);
            return value is T t ? t : default;
        }
        return default;
    }
}
