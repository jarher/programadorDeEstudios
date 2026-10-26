using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StudyTracker.UI.Converters;

/// <summary>
/// Convierte un color hexadecimal (#RRGGBB o #AARRGGBB) en un pincel congelado.
/// Devuelve un gris neutro cuando el valor falta o no es válido.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FallbackBrush = CreateFallbackBrush();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && TryCreateBrush(hex, out var brush))
        {
            return brush;
        }

        return FallbackBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static bool TryCreateBrush(string hex, out SolidColorBrush brush)
    {
        brush = FallbackBrush;

        var candidate = hex.Trim();
        if (candidate.Length is not (7 or 9) || candidate[0] != '#')
        {
            return false;
        }

        for (var index = 1; index < candidate.Length; index++)
        {
            if (!Uri.IsHexDigit(candidate[index]))
            {
                return false;
            }
        }

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(candidate);
            brush = new SolidColorBrush(color);
            brush.Freeze();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static SolidColorBrush CreateFallbackBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A));
        brush.Freeze();
        return brush;
    }
}
