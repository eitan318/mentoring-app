# 08 — Validation & Value Converters

> **Curriculum context.** Section 10 of the *Integrated Data Applications* rubric lists, as advanced extension topics:
> * "Writing and using **value converter** classes."
> * "Writing and using **validation** classes."
>
> Both are ubiquitous WPF/MVVM concepts when used correctly; this document explains the two pipelines side by side because they share a property — they sit between the bind-source value and the bind-target value.

---

## 1. Two-Layer Validation

The codebase validates user input twice. Each layer answers a different question.

| Layer | Question | Tool |
|---|---|---|
| Client | "Is this individually valid as-the-user-types?" | DataAnnotations + `ObservableValidator` |
| Server | "Is this **internally consistent** at write time?" | FluentValidation |

### 1.1 Layer 1 — DataAnnotations on ViewModels

The `ObservableValidator` base class (CommunityToolkit.Mvvm) reads `System.ComponentModel.DataAnnotations` attributes on `[ObservableProperty]` fields and re-runs them on every set.

```csharp
public partial class LoginViewModel : ObservableValidator, INavigatable
{
    [ObservableProperty]
    [Required(ErrorMessage = "National ID is required")]
    [RegularExpression(@"^\d{6,12}$", ErrorMessage = "Numeric, 6–12 digits")]
    private string _nationalId = "";

    [RelayCommand]
    private async Task SendVerificationCode()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        // ...
    }
}
```

**Properties:**

- Errors are exposed via `INotifyDataErrorInfo`; WPF's binding pipeline binds `Validation.HasError`, `Validation.Errors[0].ErrorContent` automatically.
- `ValidateAllProperties()` walks the type with reflection once.
- Per-field re-validation runs on every set, so the user gets immediate feedback.

> **Reviewer note** — DataAnnotations are right for *single-property* rules. They are wrong for *cross-property* rules ("if `IsMentor` is true, `MentorProfile` must be set"); those go on the server.

### 1.2 Layer 2 — FluentValidation in services

```csharp
// src/MentoringApp.Service/Validator/UserValidator.cs
public class UserValidator : AbstractValidator<UserModel>
{
    public UserValidator()
    {
        RuleFor(u => u.Email).NotEmpty().EmailAddress();
        RuleFor(u => u.UserName).NotEmpty().MaximumLength(60);
        RuleFor(u => u.NationalId).NotEmpty().Matches(@"^\d{6,12}$");

        // Cross-property rule
        RuleFor(u => u).ChildRules(c =>
            c.When(x => x is StudentModel s && s.IsMentor,
                () => c.RuleFor(x => ((StudentModel)x).MentorProfile)
                       .NotNull()
                       .SetValidator(new MentorProfileValidator())));
    }
}
```

**Properties:**

- Stateless — registered as Singleton in DI.
- `RuleFor(u => u).ChildRules(...)` is the FluentValidation idiom for "rules that depend on the whole entity".
- Failure is converted into `Result.ValidationFailure(errorsByField)` so the API response can carry per-field errors and the client UI can highlight individual inputs.

### 1.3 The two-layer interaction

```
User types in TextBox
    │
    ▼
[Layer 1] DataAnnotations re-run → red border in WPF if invalid
    │
    ▼ (user submits)
[Server] FluentValidation runs → if invalid, return ValidationFailure
    │
    ▼
ViewModel binds errors back into the field-level validation state
    │
    ▼
WPF renders the per-field server errors using the same Validation.* templates
```

> **Reviewer note** — duplicating *every* rule on both sides is unnecessary. The right split is:
> * Pure-format rules (regex, length, required) — both layers (client for UX, server for safety).
> * Cross-field rules — server only.
> * Database-dependent rules ("national-id must be unique") — server only.

---

## 2. Value Converters

`IValueConverter` is the WPF binding-engine extension point. It transforms a source value into a target value at bind time.

### 2.1 The `IValueConverter` contract

```csharp
public interface IValueConverter
{
    object? Convert    (object value, Type targetType, object parameter, CultureInfo culture);
    object  ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

The two methods correspond to `Mode=OneWay` and `Mode=TwoWay` data flow.

### 2.2 Catalogue (with rationale)

| Converter | Source → Target | Why a converter, not a property |
|---|---|---|
| `InverseBoolConverter` | `bool → !bool` | Negation in XAML is impossible; a converter is the only option |
| `InverseBooleanToVisibilityConverter` | `bool → Visibility` (reversed) | One-line inversion of the standard converter |
| `NullToVisibilityConverter` | `T? → Visibility` | Avoid wrapping every nullable property in a separate `IsXxxNull` |
| `StringToImageSourceConverter` | `string → BitmapImage` | Loads file, freezes, handles missing-file gracefully |
| `StringToFlowDirectionConverter` | `"he"/"en" → FlowDirection` | RTL/LTR switch driven by language code |
| `DbTranslationConverter` | DB key → localised string | Keeps DB rows language-agnostic |
| `LocalizedFormatConverter` | composite → resx-templated string | Multi-binding, no string interpolation in code-behind |
| `LocalizedClassCountConverter` | `(count, gradeName) → "5 classes"` | Pluralisation aware |
| `GradeClassDisplayConverter` | `(GradeNum, ClassNum) → "10A"` | Shared label format across views |
| `PercentToStrokeDashArrayConverter` | `double → string` | Donut/ring chart segment computation |

### 2.3 Anatomy — `StringToImageSourceConverter`

```csharp
public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path)) return null;
        if (!File.Exists(path)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;       // ① close handle ASAP
            bitmap.UriSource   = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();                                     // ② cross-thread safe
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

> **Notes**
> ① `BitmapCacheOption.OnLoad` is essential: without it, the bitmap holds the file open — the user can't replace it through the OS.
> ② `Freeze()` prevents WPF from cloning the bitmap whenever it is assigned to a control, and makes it safe to use from multiple dispatcher threads.

### 2.4 Authoring rules

- **Pure functions.** A converter must not depend on instance state — it should be safe to register as a singleton in `App.xaml`.
- **No exceptions out.** Conversion failures return `DependencyProperty.UnsetValue`, `Binding.DoNothing`, or `null` (with `TargetNullValue` configured in the binding).
- **Implement `ConvertBack` only when meaningful.** If two-way binding doesn't apply, throw `NotSupportedException` so misuse fails fast.
- **Culture-aware.** When a converter handles human-readable text, always use the `culture` parameter rather than `CultureInfo.CurrentCulture`.

### 2.5 Multi-value converters — `IMultiValueConverter`

```csharp
public class LocalizedFormatConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string formatKey) return null;
        var format = TranslationSource.Instance[formatKey];
        return string.Format(culture, format, values);
    }
    // ConvertBack throws.
}
```

> **Why prefer this to inline `MultiBinding.StringFormat`?** `MultiBinding.StringFormat` does not pass the format through the localiser; the format string would have to live in XAML. With `LocalizedFormatConverter`, the format key is resolved against the active `Strings.resx` at the time of binding evaluation — RTL and Hebrew variants are honoured.

---

## 3. Where to Put a Validation vs a Converter

These two mechanisms are sometimes confused because both sit between a model and the screen.

| Concern | Mechanism |
|---|---|
| "Reject this value as malformed" | Validator (DataAnnotations or FluentValidation) |
| "Display this value differently" | Converter |
| "The user can edit a derived view of this property" | Converter with `ConvertBack` |
| "Two properties together must be consistent" | Validator (FluentValidation) |
| "Show a spinner while a value is being computed" | ViewModel state, not a converter |

> **Reviewer rule of thumb** — if you find yourself writing business rules inside `IValueConverter.Convert(...)`, the rule belongs in the ViewModel or the validator instead.

---

## 4. Reviewer Checklist

### Validation
- [ ] DataAnnotations exist on every user-editable VM field.
- [ ] `ObservableValidator` is the base for any VM that surfaces field errors.
- [ ] Cross-property rules are server-side only (FluentValidation).
- [ ] `Result.ValidationFailure` is the *only* path for a failed validation; never throw from a service.
- [ ] Server-side rules are unit-tested exhaustively.

### Converters
- [ ] No converter holds mutable state.
- [ ] `BitmapImage` converters call `Freeze()` and use `BitmapCacheOption.OnLoad`.
- [ ] `ConvertBack` throws when one-way binding is the only sensible mode.
- [ ] Converters that depend on culture take the `culture` parameter seriously.
- [ ] All converters are registered as `<x:Static>` resources in `App.xaml` for app-wide reuse.

---

## 5. Curriculum Alignment

| Rubric phrase | Realisation | Section |
|---|---|---|
| "Writing & using validation classes" | `UserValidator`, `MentorProfileValidator` (FluentValidation) | §1.2 |
| "Writing & using value converter classes" | 10 converters under `Converter/` | §2 |
| "Comfortable UI" (mandatory #5) | Live validation feedback via `ObservableValidator` | §1.1 |
| "Smart data management" (mandatory #6) | Server-side validation prevents bad data reaching SQL | §1.2 |
