namespace HRMS.Services;

public class ThemeService
{
    private string _currentTheme = "lemonade";

    public string CurrentTheme => _currentTheme;

    public bool IsDark => _currentTheme == "business";

    public event Action? OnThemeChanged;

    public void Initialize(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return;
        }

        SetTheme(theme);
    }

    public void SetTheme(string theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        _currentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    public void Toggle()
    {
        SetTheme(IsDark ? "lemonade" : "business");
    }
}
