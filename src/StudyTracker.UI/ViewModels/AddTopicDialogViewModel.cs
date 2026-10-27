using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Estado y validación del diálogo modal de alta de temáticas. No depende de WPF.
/// </summary>
public sealed partial class AddTopicDialogViewModel : ObservableObject
{
    private static readonly ColorOption DefaultColor = new("Azul", "#0078D4");

    private static readonly Regex ColorHexPattern = new(
        "^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _targetHoursText = "10";

    [ObservableProperty]
    private ColorOption _selectedColor = DefaultColor;

    public IReadOnlyList<ColorOption> Palette { get; } =
    [
        DefaultColor,
        new("Violeta", "#8764B8"),
        new("Verde", "#107C10"),
        new("Teal", "#038387"),
        new("Ámbar", "#C19C00"),
        new("Naranja", "#CA5010"),
        new("Rojo", "#C42B1C"),
        new("Rosa", "#E3008C"),
        new("Índigo", "#5B5FC7"),
        new("Grafito", "#4F4F4F")
    ];

    public bool CanCreate =>
        !string.IsNullOrWhiteSpace(Title)
        && TryParseTargetHours(TargetHoursText, out _)
        && IsColorValid;

    public string? ValidationMessage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                return "Escribe un título para la temática.";
            }

            if (!TryParseTargetHours(TargetHoursText, out _))
            {
                return "Introduce una meta de horas entre 0 y 1000.";
            }

            if (!IsColorValid)
            {
                return "Selecciona un color para la temática.";
            }

            return null;
        }
    }

    /// <summary>
    /// Construye la petición normalizada o devuelve null si los datos no son válidos.
    /// </summary>
    public NewTopicRequest? TryBuildRequest()
    {
        if (!CanCreate || !TryParseTargetHours(TargetHoursText, out var targetHours) || SelectedColor is null)
        {
            return null;
        }

        var title = Truncate(Title.Trim(), 120);
        var description = string.IsNullOrWhiteSpace(Description) ? null : Truncate(Description.Trim(), 400);
        var colorHex = SelectedColor.Hex.Trim().ToUpperInvariant();

        return new NewTopicRequest(
            title,
            description,
            Math.Round(targetHours, 2, MidpointRounding.AwayFromZero),
            colorHex);
    }

    partial void OnTitleChanged(string value)
    {
        NotifyValidationChanged();
    }

    partial void OnTargetHoursTextChanged(string value)
    {
        NotifyValidationChanged();
    }

    partial void OnSelectedColorChanged(ColorOption value)
    {
        NotifyValidationChanged();
    }

    private bool IsColorValid =>
        SelectedColor is { } color && ColorHexPattern.IsMatch(color.Hex);

    private void NotifyValidationChanged()
    {
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    private static bool TryParseTargetHours(string? text, out double hours)
    {
        hours = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');

        if (!double.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out hours))
        {
            return false;
        }

        return double.IsFinite(hours) && hours is >= 0 and <= 1000;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
