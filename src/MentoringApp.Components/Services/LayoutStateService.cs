namespace MentoringApp.Components.Services;

public class LayoutStateService
{
    private string _language = "en";

    public string Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            OnChange?.Invoke();
        }
    }

    public event Action? OnChange;
}
