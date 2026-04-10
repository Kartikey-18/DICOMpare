using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DiCOMpare.Models;

namespace DiCOMpare.Converters;

public class SafetyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TagSafety safety)
        {
            return safety switch
            {
                TagSafety.Safe => new SolidColorBrush(Color.FromRgb(46, 160, 67)),      // Green
                TagSafety.Caution => new SolidColorBrush(Color.FromRgb(210, 153, 34)),   // Amber
                TagSafety.Unsafe => new SolidColorBrush(Color.FromRgb(218, 54, 51)),     // Red
                _ => new SolidColorBrush(Colors.Gray),
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class SafetyToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TagSafety safety)
        {
            return safety switch
            {
                TagSafety.Safe => "SAFE",
                TagSafety.Caution => "CAUTION",
                TagSafety.Unsafe => "UNSAFE",
                _ => "?",
            };
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class SafetyToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TagSafety safety)
        {
            return safety switch
            {
                TagSafety.Safe => new SolidColorBrush(Color.FromArgb(25, 46, 160, 67)),
                TagSafety.Caution => new SolidColorBrush(Color.FromArgb(25, 210, 153, 34)),
                TagSafety.Unsafe => new SolidColorBrush(Color.FromArgb(35, 218, 54, 51)),
                _ => new SolidColorBrush(Colors.Transparent),
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class MatchStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MatchStatus status)
        {
            return status switch
            {
                MatchStatus.Match => new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                MatchStatus.Mismatch => new SolidColorBrush(Color.FromRgb(255, 200, 100)),
                MatchStatus.MissingLeft => new SolidColorBrush(Color.FromRgb(150, 180, 255)),
                MatchStatus.MissingRight => new SolidColorBrush(Color.FromRgb(150, 180, 255)),
                _ => new SolidColorBrush(Colors.Gray),
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class VerdictToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            if (text.StartsWith("INCOMPATIBLE"))
                return new SolidColorBrush(Color.FromRgb(218, 54, 51));
            if (text.StartsWith("REVIEW"))
                return new SolidColorBrush(Color.FromRgb(210, 153, 34));
            if (text.StartsWith("CONVERTIBLE"))
                return new SolidColorBrush(Color.FromRgb(46, 160, 67));
            if (text.StartsWith("IDENTICAL"))
                return new SolidColorBrush(Color.FromRgb(46, 160, 67));
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
