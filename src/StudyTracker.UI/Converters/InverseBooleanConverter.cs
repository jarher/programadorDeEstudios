using System.Globalization;
using System.Windows.Data;

namespace StudyTracker.UI.Converters;

/// <summary>
/// Invierte un valor booleano; se usa para deshabilitar acciones mientras el ViewModel está ocupado.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not bool flag || !flag;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not bool flag || !flag;
    }
}
