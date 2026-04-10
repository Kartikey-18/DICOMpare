using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DiCOMpare.Services;

public class ThemeService : INotifyPropertyChanged
{
    private bool _isDark = true;

    public bool IsDark
    {
        get => _isDark;
        set
        {
            _isDark = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowBackground));
            OnPropertyChanged(nameof(PanelBackground));
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(TextPrimary));
            OnPropertyChanged(nameof(TextSecondary));
            OnPropertyChanged(nameof(TextMuted));
            OnPropertyChanged(nameof(AccentBlue));
            OnPropertyChanged(nameof(AccentRed));
            OnPropertyChanged(nameof(AccentGreen));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(InputBackground));
            OnPropertyChanged(nameof(HeaderBackground));
            OnPropertyChanged(nameof(RowAlt));
            OnPropertyChanged(nameof(SelectedRow));
            OnPropertyChanged(nameof(ButtonBackground));
            OnPropertyChanged(nameof(ButtonHover));
            OnPropertyChanged(nameof(ToggleLabel));
        }
    }

    public void Toggle() => IsDark = !IsDark;

    // Window
    public SolidColorBrush WindowBackground => Brush(_isDark ? "#1E1E2E" : "#F5F5F5");
    public SolidColorBrush PanelBackground => Brush(_isDark ? "#181825" : "#FFFFFF");
    public SolidColorBrush CardBackground => Brush(_isDark ? "#181825" : "#FAFAFA");

    // Text
    public SolidColorBrush TextPrimary => Brush(_isDark ? "#CDD6F4" : "#1E1E2E");
    public SolidColorBrush TextSecondary => Brush(_isDark ? "#A6ADC8" : "#555555");
    public SolidColorBrush TextMuted => Brush(_isDark ? "#6C7086" : "#999999");

    // Accent colors (same in both themes for safety badges)
    public SolidColorBrush AccentBlue => Brush(_isDark ? "#89B4FA" : "#2563EB");
    public SolidColorBrush AccentRed => Brush(_isDark ? "#F38BA8" : "#DC2626");
    public SolidColorBrush AccentGreen => Brush(_isDark ? "#A6E3A1" : "#16A34A");

    // UI elements
    public SolidColorBrush BorderColor => Brush(_isDark ? "#45475A" : "#D4D4D4");
    public SolidColorBrush InputBackground => Brush(_isDark ? "#313244" : "#FFFFFF");
    public SolidColorBrush HeaderBackground => Brush(_isDark ? "#313244" : "#E5E5E5");
    public SolidColorBrush RowAlt => Brush(_isDark ? "#1B1B2B" : "#F0F0F0");
    public SolidColorBrush SelectedRow => Brush(_isDark ? "#45475A" : "#DBEAFE");
    public SolidColorBrush ButtonBackground => Brush(_isDark ? "#45475A" : "#E5E5E5");
    public SolidColorBrush ButtonHover => Brush(_isDark ? "#585B70" : "#D4D4D4");
    public SolidColorBrush GridLine => Brush(_isDark ? "#313244" : "#E5E5E5");

    public string ToggleLabel => _isDark ? "Light Mode" : "Dark Mode";

    private static SolidColorBrush Brush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
