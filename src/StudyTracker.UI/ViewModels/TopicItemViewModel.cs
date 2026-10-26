using CommunityToolkit.Mvvm.ComponentModel;
using StudyTracker.Core.Models;

namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Expone una temática con su resumen de estudio lista para el bindeo del dashboard.
/// </summary>
public sealed class TopicItemViewModel : ObservableObject
{
    private readonly TopicSummaryDto _summary;
    private string _status;
    private DateTime? _completedAt;

    public TopicItemViewModel(TopicSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(summary.Topic);

        _summary = summary;
        _status = summary.Topic.Status;
        _completedAt = summary.Topic.CompletedAt;
    }

    public int Id => _summary.Topic.Id;

    public string Title => _summary.Topic.Title;

    public string? Description => _summary.Topic.Description;

    public string ColorHex => _summary.Topic.ColorHex;

    public string Status => _status;

    public bool IsCompleted => string.Equals(_status, Topic.StatusCompleted, StringComparison.Ordinal);

    public bool IsArchived => string.Equals(_status, Topic.StatusArchived, StringComparison.Ordinal);

    public DateTime? CompletedAt => _completedAt;

    public DateTime? LastStudiedAt => _summary.LastStudiedAt;

    public int TotalMinutesStudied => _summary.TotalMinutesStudied;

    public double TargetHours => _summary.Topic.TargetHours;

    public double ProgressPercentage
    {
        get
        {
            if (!HasTarget)
            {
                return 0;
            }

            var progress = TotalMinutesStudied / (TargetHours * 60) * 100;
            return Math.Clamp(progress, 0, 100);
        }
    }

    public bool HasTarget => TargetHours > 0;

    public string StudiedTimeText => FormatMinutes(TotalMinutesStudied);

    public string TargetText => HasTarget
        ? $"Meta {FormatHours(TargetHours)}"
        : "Sin meta";

    public string ProgressText => HasTarget
        ? $"{ProgressPercentage:0} % de la meta"
        : "Sin meta";

    /// <summary>
    /// Aplica localmente un nuevo estado y su fecha de finalización, sin persistir cambios.
    /// </summary>
    public void ApplyStatus(string status, DateTime? completedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (string.Equals(_status, status, StringComparison.Ordinal) && _completedAt == completedAt)
        {
            return;
        }

        _status = status;
        _completedAt = completedAt;

        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsArchived));
        OnPropertyChanged(nameof(CompletedAt));
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;

        return remainingMinutes == 0
            ? $"{hours} h"
            : $"{hours} h {remainingMinutes} min";
    }

    private static string FormatHours(double hours)
    {
        var rounded = Math.Round(hours, 1);

        return rounded == Math.Floor(rounded)
            ? $"{rounded:0} h"
            : $"{rounded:0.#} h";
    }
}
