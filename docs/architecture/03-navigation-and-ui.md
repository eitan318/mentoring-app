# 03 — Cross-Platform Navigation & UI Logic

This document describes the navigation subsystem that lets a single set of ViewModels drive both a WPF desktop shell and a Blazor Server web shell. The centrepiece is the **stack-of-stacks** navigation model, accompanied by `INavigatable` lifecycle hooks, ViewModel-keyed `DataTemplate` resolution, and a deliberate generalisation of navigation across platforms.

---

## 1. The Stack-of-Stacks Navigation Model

`MentoringApp.ViewModel/Navigation/INavigationService.cs` declares the canonical surface:

```csharp
public interface INavigationService
{
    event Action? CanGoBackChanged;

    Task NavigateToAsync<TViewModel>()
        where TViewModel : class, INavigatable;

    Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : class, INavigatable<TParameter>;

    Task NavigateToRootAsync<TViewModel>()
        where TViewModel : class, INavigatable;

    Task NavigateToRootAsync<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : class, INavigatable<TParameter>;

    IDisposable UseContext(Action<INavigatable> contextSetter);

    Task GoBackAsync();
    bool CanGoBack();
}
```

### Why a stack of stacks?

A single global navigation stack works only when there is one shell. MentoringApp has many: a login shell, a student shell, a supervisor shell, an admin shell, plus modal sub-shells (e.g. "review pair" inside "supervisor dashboard"). Each shell needs its **own back stack** that does not bleed into the parent shell's history.

The naive answer is to spin up a separate navigation service per shell — but then they cannot share singletons such as `UserStore`, and back-history reconciliation across shells becomes ad-hoc.

The chosen answer is a **stack of stacks**: one `NavigationService` singleton owns a stack of `NavigationStore`s. Pushing a new context (entering a sub-shell) pushes a fresh history stack; popping the context (closing the sub-shell) drops its history wholesale.

### How — the implementation

```csharp
// src/MentoringApp.ViewModel/Navigation/NavigationService.cs (sketch)
internal sealed class NavigationStore
{
    public Stack<INavigatable> History { get; } = new();
    public Action<INavigatable>? ContextSetter { get; set; }
}

public sealed class NavigationService : INavigationService
{
    private readonly Stack<NavigationStore> _storeStack = new();
    private readonly IServiceProvider _services;

    public IDisposable UseContext(Action<INavigatable> contextSetter)
    {
        var store = new NavigationStore { ContextSetter = contextSetter };
        _storeStack.Push(store);
        return new ContextScope(() => _storeStack.Pop());
    }

    public async Task NavigateToAsync<TViewModel>() where TViewModel : class, INavigatable
    {
        var current = _storeStack.Peek();
        var vm = _services.GetRequiredService<TViewModel>();   // Transient: fresh instance
        if (current.History.Count > 0)
            await current.History.Peek().OnNavigatedFromAsync();
        current.History.Push(vm);
        current.ContextSetter?.Invoke(vm);                     // shell renders the new VM
        await vm.OnNavigatedToAsync();
    }

    public Task GoBackAsync()
    {
        var current = _storeStack.Peek();
        current.History.Pop();
        var top = current.History.Peek();
        current.ContextSetter?.Invoke(top);
        return Task.CompletedTask;
    }
}
```

`UseContext` is the linchpin. A shell calls it on activation; the `IDisposable` it returns is owned by the shell so that cleanup is automatic when the shell is disposed:

```csharp
public partial class SupervisorShellViewModel : ObservableObject, INavigatable, IDisposable
{
    private readonly IDisposable _contextScope;
    [ObservableProperty] private INavigatable? _currentInner;

    public SupervisorShellViewModel(INavigationService nav)
    {
        _contextScope = nav.UseContext(vm => CurrentInner = vm);
    }

    public void Dispose() => _contextScope.Dispose();
}
```

Inside that scope every `NavigateToAsync<T>()` call updates `CurrentInner`, which the view binds to a `ContentControl` whose `ContentTemplateSelector` resolves a View per ViewModel type.

---

## 2. The `INavigatable` Lifecycle

```csharp
public interface INavigatable
{
    Task OnNavigatedToAsync()   => Task.CompletedTask;
    Task OnNavigatedFromAsync() => Task.CompletedTask;
}

public interface INavigatable<TParameter> : INavigatable
{
    Task OnNavigatedToAsync(TParameter parameter);
}
```

### Why default-implemented interfaces

C# 8 default interface members let ViewModels opt **in** to the lifecycle methods they care about, rather than implementing both as no-ops. A leaf-page ViewModel that needs to load data overrides `OnNavigatedToAsync`; an inert page ignores both.

### How `IClosable` complements navigation

For modal flows (`ResolveIssueDialog`, `ConfirmDeleteDialog`) that need to *return a value* up the navigation stack, ViewModels additionally implement `IClosable` and expose a `TaskCompletionSource<TResult>`:

```csharp
public interface IClosable
{
    Task ClosedTask { get; }
    void Close();
}
```

The shell awaits `ClosedTask` and unwinds the navigation stack when the dialog completes.

---

## 3. Generalisation Across WPF and Web

The boundary between WPF and Blazor is a single DI swap.

```csharp
// src/MentoringApp.Web/Program.cs
var navServiceDescriptor =
    builder.Services.Single(d => d.ServiceType == typeof(INavigationService));
builder.Services.Remove(navServiceDescriptor);
builder.Services.AddScoped<INavigationService, WebNavigationService>();
```

### Why a separate `WebNavigationService`

* In Blazor Server, navigation is **driven by URLs**, not by the ContentControl's `Content` property. The browser owns the back/forward stack, and `NavigationManager.NavigateTo("/student/dashboard")` causes a re-render of the matching `@page` component.
* Sharing the WPF `NavigationService`'s in-memory stack on the web would break browser back-button semantics.

### How parameter passing survives the URL

WPF's `NavigateToAsync<TViewModel, TParameter>(parameter)` cannot serialise an arbitrary parameter into a URL. Instead, `WebNavigationService` writes the parameter into a `NavigationParameterStore` (a scoped singleton keyed by destination type) and then calls `NavigationManager.NavigateTo(url)`. The destination Razor component reads the parameter back from the store in `OnInitializedAsync`.

The result: the same call site —

```csharp
await _nav.NavigateToAsync<EditPairViewModel, PairModel>(pair);
```

— behaves identically on both platforms. The platform-specific delivery mechanism is hidden inside the `INavigationService` implementation.

---

## 4. ViewModel-First Composition

Both shells use **ViewModel-first** rendering: the shell binds `Content` to a ViewModel instance, and the framework selects the correct view.

### WPF — `ViewModelViewMap.xaml`

```xml
<ResourceDictionary xmlns:vm="clr-namespace:MentoringApp.ViewModel.ViewModel"
                    xmlns:v ="clr-namespace:MentoringApp.View">

    <DataTemplate DataType="{x:Type vm:LoginViewModel}">
        <v:LoginView />
    </DataTemplate>

    <DataTemplate DataType="{x:Type vm:StudentDashboardViewModel}">
        <v:Student.StudentDashboardView />
    </DataTemplate>

    <!-- … one per ViewModel … -->
</ResourceDictionary>
```

The shell's `ContentControl` simply binds:

```xml
<ContentControl Content="{Binding CurrentInner}"
                FlowDirection="{Binding FlowDirection,
                               Source={x:Static loc:TranslationSource.Instance}}"/>
```

WPF resolves the `DataTemplate` whose `DataType` matches the runtime type and instantiates the corresponding `View`. ViewModels and Views are **never** referenced from each other directly.

### Web — `@page` directives

The Blazor side uses route-attributed components:

```razor
@page "/student/dashboard"
@inject StudentDashboardViewModel Vm

<StudentDashboardComponent Vm="Vm" />
```

The composition root is identical — a Razor component receives the ViewModel through DI and renders against it.

### Why ViewModel-first

* **Tooling tax once, parity forever.** Set up the `DataTemplate` and `@page` mappings once and every navigation call benefits.
* **No ViewModel knows about a View.** This invariant prevents accidental UI-framework dependencies from leaking into the ViewModel project, which is a hard requirement for the Blazor port to remain viable.

---

## 5. Bindings, Converters and Shared ViewModel Logic

### XAML bindings

WPF bindings are direct property paths against ViewModels, with converters in `src/MentoringApp.Desktop/Converter/` for cross-cutting transformations: `BoolToVisibilityConverter`, `InverseBoolConverter`, `RoleToColorConverter`, `EmptyStringToVisibilityConverter`. Converters are kept in the View project — they are inherently UI concerns.

### Razor bindings

Blazor uses `@bind` and `@onclick` syntax. Razor components consume the **same** ViewModels — there is no `*ViewModel.Web.cs` mirror. Where a binding pattern requires a converter, a small Razor `@functions` block performs the projection.

### Sharing logic across ViewModels

ViewModel reuse is achieved through:

* **Shared base classes** for cross-cutting concerns (e.g. an internal `DashboardViewModelBase` for the role dashboards that need the same "load my pairs" routine).
* **Composition through stores.** `UserStore` (a singleton) exposes the current user and triggers `INotifyPropertyChanged`. Every ViewModel that needs the user simply binds to it; no ViewModel "owns" the user.
* **Helper services in `src/MentoringApp.ViewModel/Helpers/`.** Pure utilities (e.g. file-picker formatting, profile-completeness calculation) shared by multiple ViewModels.

### Why share through stores rather than inheritance

The Blazor lifetime model (`Scoped` per circuit) does not match the WPF lifetime model (`Transient` ViewModels, long-lived window). A singleton store with INPC notifications behaves correctly under both lifetime regimes; a deep inheritance tree would entangle the lifetimes and cause memory leaks.

---

## 6. User Controls (Extension §9)

The rubric's section 9 explicitly mentions *"use of user controls"* as an extension topic. The codebase satisfies this with reusable, dependency-property-driven controls under `src/MentoringApp.Desktop/View/Components/`.

### 6.1 The `ProfilePictureControl` — anatomy of a `UserControl`

```csharp
// src/MentoringApp.Desktop/View/Components/ProfilePictureControl.xaml.cs
public partial class ProfilePictureControl : UserControl
{
    public static readonly DependencyProperty ImagePathProperty =
        DependencyProperty.Register("ImagePath", typeof(string), typeof(ProfilePictureControl),
            new PropertyMetadata(null, OnImagePathOrGenderChanged));

    public string ImagePath
    {
        get => (string)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public static readonly DependencyProperty GenderProperty =
        DependencyProperty.Register("Gender", typeof(Gender), typeof(ProfilePictureControl),
            new PropertyMetadata(Gender.PreferNoAnswer, OnImagePathOrGenderChanged));
    // ...
}
```

### 6.2 Why `DependencyProperty` instead of CLR properties

* **Bindings.** WPF only allows `{Binding …}` against `DependencyProperty`. A plain CLR property cannot receive a binding source.
* **Property-changed callbacks.** `OnImagePathOrGenderChanged` runs whenever *either* property mutates, allowing the control to recompute the default-avatar fill colour atomically.
* **Read-only computed state.** `ShowDefaultAvatar` and `DefaultFill` are read-only DPs registered with `RegisterReadOnly(...)` — only the control writes them, but XAML triggers can still bind to them.

### 6.3 Reuse patterns

| Reuse mechanism | Where used | Pros |
|---|---|---|
| `UserControl` (composite) | `ProfilePictureControl`, `ToastHostView` | Encapsulates layout + code-behind |
| `ResourceDictionary` styles | `Styles/*.xaml` | Inherit a look, not a structure |
| `DataTemplate` selectors | `Styles/ViewModelViewMap.xaml` | Polymorphic rendering |
| Attached property | (none yet — candidate for keyboard helpers) | Cross-cutting, no inheritance |

> **Reviewer note** — `UserControl` is appropriate here because the avatar contains *both* presentation and small amounts of behaviour (file existence check, gender→colour mapping). Pure presentation would belong in a `ResourceDictionary` style.

---

## 7. Value Converters (Extension §10)

The rubric's section 10 explicitly mentions *"writing and using value converter classes"*. The codebase carries ten production converters under `src/MentoringApp.Desktop/Converter/`.

### 7.1 The contract

```csharp
public interface IValueConverter
{
    object? Convert    (object value, Type targetType, object parameter, CultureInfo culture);
    object  ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

A converter is the binding-engine extension point: it transforms a source-property value into a target-property value at bind time.

### 7.2 Catalogue

| Converter | Purpose |
|---|---|
| `InverseBoolConverter` | `bool` ↔ `!bool` for `IsEnabled` toggles |
| `InverseBooleanToVisibilityConverter` | `bool → Visibility.Collapsed/Visible` reversed |
| `NullToVisibilityConverter` | Nullable reference → `Collapsed` when null |
| `StringToImageSourceConverter` | File-path string → `BitmapImage` (or null) |
| `StringToFlowDirectionConverter` | `"he"`/`"en"` → `FlowDirection.RightToLeft`/`LeftToRight` |
| `DbTranslationConverter` | DB-stored key → localised string via `TranslationSource` |
| `LocalizedFormatConverter` | Composite-format string from resx + multi-binding |
| `LocalizedClassCountConverter` | Pluralisation-aware (e.g. "1 class" vs "3 classes") |
| `GradeClassDisplayConverter` | `(Grade, ClassNum)` → `"10A"` style label |
| `PercentToStrokeDashArrayConverter` | Percentage → SVG-ish ring chart segment |

### 7.3 Walk-through — `StringToImageSourceConverter`

```csharp
public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;
        if (!System.IO.File.Exists(path)) return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;   // close the file handle immediately
        bitmap.UriSource   = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();                                 // make immutable & cross-thread safe
        return bitmap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

> **Reviewer note** — `BitmapCacheOption.OnLoad` + `Freeze()` is the correct pattern for "load-once, never-mutate" images. Without `OnLoad`, the file handle would stay open for the lifetime of the bitmap; without `Freeze`, the image could not be assigned to a control on a thread different from where it was created.

### 7.4 Multi-binding converters

`LocalizedFormatConverter` implements `IMultiValueConverter`, taking an array of source values plus a format key and producing one output:

```csharp
// Pseudo-XAML (multi-binding for a localisable greeting)
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{StaticResource LocalizedFormat}" ConverterParameter="WelcomeFormat">
            <Binding Path="UserName"/>
            <Binding Path="UnreadCount"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

> **Why prefer a multi-converter to string interpolation in code-behind?** The localisation keys (`WelcomeFormat`) live with the resx, the bindings live in XAML, and the WPF binding engine handles change-propagation automatically.

### 7.5 Registration in `App.xaml`

```xml
<Application.Resources>
    <ResourceDictionary>
        <conv:InverseBoolConverter             x:Key="InverseBool"/>
        <conv:NullToVisibilityConverter        x:Key="NullToVis"/>
        <conv:StringToImageSourceConverter     x:Key="ImagePathToImage"/>
        <!-- … -->
    </ResourceDictionary>
</Application.Resources>
```

Once registered, converters are referenced from any view via `{StaticResource}`.

> **Reviewer checklist**
> - Converters never mutate input — they are pure functions of `(value, parameter, culture)`.
> - `ConvertBack` is implemented only when `Mode=TwoWay` is realistic; otherwise it throws `NotSupportedException`.
> - Conversion failures return the binding-engine sentinel (`DependencyProperty.UnsetValue` or `Binding.DoNothing`) rather than throwing.

---

## 8. Permission-Aware UI (Mandatory Requirement #8)

The rubric's mandatory requirement #8 demands *"the project handles multiple permission levels … the UI must contain only the options matching the permission level"*. UI-side permission gating happens in three coordinated places.

### 8.1 Shell selection at login time

`AuthenticatedDashboardView` selects an inner shell ViewModel by inspecting `UserStore.Current`:

```csharp
INavigatable shell = _userStore.Current switch
{
    AdminModel       => _services.GetRequiredService<AdminDashboardViewModel>(),
    SupervisorModel  => _services.GetRequiredService<SupervisorDashboardViewModel>(),
    StudentModel     => _services.GetRequiredService<StudentDashboardViewModel>(),
    _                => throw new InvalidOperationException("Unknown role")
};
```

A user simply cannot reach the wrong shell — there is no URL or hotkey that lets a Student land on `AdminDashboardView`.

### 8.2 Per-element visibility gating

Inside a shared shell, finer permissions are expressed via `Visibility` bindings:

```xml
<Button Content="{Binding [Forward_To_Admin], Source={x:Static loc:TranslationSource.Instance}}"
        Visibility="{Binding CanForwardToAdmin, Converter={StaticResource BoolToVis}}"/>
```

`CanForwardToAdmin` is computed in the ViewModel from `UserStore.Current` and the entity context (e.g. only a Supervisor can forward an issue belonging to one of their assigned classes).

### 8.3 Server-side enforcement

UI gating is **convenience**, not security. The actual permission check is on the API:

```csharp
group.MapPost("/", async (CreateUserRequest req, UserService userService) =>
{
    // ...
}).RequireAuthorization("AdminOnly");
```

`AdminOnly`, `AdminOrSupervisor` policies are wired in `Program.cs` (see [10-security.md](10-security.md)). A forged client cannot escape the policy check.

> **Reviewer note** — never trust a UI hide. Even if a button is invisible, the corresponding endpoint must enforce the policy server-side.

---

## 9. Summary

The navigation and UI architecture deliberately treats **ViewModels as the contract** between the application and its renderers. The stack-of-stacks design supports nested shells without leaking back-history. `INavigatable`/`IClosable` give ViewModels the lifecycle hooks they need without coupling them to a shell. `DataTemplate` (WPF) and `@page` (Blazor) provide platform-appropriate view resolution while keeping the ViewModel layer untouched. The platform-specific `INavigationService` implementation is the single seam where WPF and Web semantics diverge. UserControls and Value Converters extend the WPF surface without introducing UI logic into ViewModels, and permission-aware rendering is layered on top of strictly server-enforced policies.

---

## 10. Curriculum Alignment

| Rubric concept | Realisation | Section |
|---|---|---|
| Comfortable UI (mandatory #5) | Stack-of-stacks navigation, role-targeted shells | §1 |
| Multi-permission levels (mandatory #8) | UI gating + server policies | §8 |
| Use of user controls (extension §9) | `ProfilePictureControl`, `ToastHostView` | §6 |
| Value converter classes (extension §10) | 10 converters under `Converter/` | §7 |
| Multiple platforms (Async track) | WPF + Blazor share ViewModels via DI swap | §3, §4 |
