using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;

namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Estado y operaciones del dashboard: listado de temáticas y resumen general.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly ITopicRepository _topicRepository;
    private double _totalTargetHours;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _totalTopics;

    [ObservableProperty]
    private int _activeTopics;

    [ObservableProperty]
    private int _completedTopics;

    [ObservableProperty]
    private int _archivedTopics;

    [ObservableProperty]
    private int _totalMinutesStudied;

    [ObservableProperty]
    private double _totalHoursStudied;

    [ObservableProperty]
    private double _overallProgressPercentage;

    [ObservableProperty]
    private bool _hasOverallTarget;

    public DashboardViewModel(ITopicRepository topicRepository)
    {
        ArgumentNullException.ThrowIfNull(topicRepository);

        _topicRepository = topicRepository;
        Topics.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasTopics));
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    public ObservableCollection<TopicItemViewModel> Topics { get; } = [];

    public bool HasTopics => Topics.Count > 0;

    public bool IsEmpty => !IsBusy && !HasTopics;

    public string OverallProgressText => HasOverallTarget
        ? $"{FormatMinutes(TotalMinutesStudied)} de {FormatHours(_totalTargetHours)} objetivo"
        : "Sin metas definidas";

    public string SummaryHeadline
    {
        get
        {
            if (TotalTopics == 0)
            {
                return "Sin temáticas todavía";
            }

            var parts = new List<string>(3);

            if (ActiveTopics > 0)
            {
                parts.Add(FormatCount(ActiveTopics, "activa", "activas"));
            }

            if (CompletedTopics > 0)
            {
                parts.Add(FormatCount(CompletedTopics, "completada", "completadas"));
            }

            if (ArchivedTopics > 0)
            {
                parts.Add(FormatCount(ArchivedTopics, "archivada", "archivadas"));
            }

            return parts.Count == 0
                ? FormatCount(TotalTopics, "temática", "temáticas")
                : string.Join(" · ", parts);
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var summaries = await _topicRepository.GetSummariesAsync();
            var items = summaries.Select(summary => new TopicItemViewModel(summary)).ToList();

            Topics.Clear();

            foreach (var item in items)
            {
                Topics.Add(item);
            }

            UpdateSummary();
        }
        catch (Exception)
        {
            Topics.Clear();
            UpdateSummary();
            ErrorMessage = "No se pudieron cargar las temáticas. Inténtalo de nuevo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ToggleCompletedAsync(TopicItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        var newStatus = item.IsCompleted ? Topic.StatusActive : Topic.StatusCompleted;
        var completedAt = newStatus == Topic.StatusCompleted ? DateTime.Now : (DateTime?)null;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var updated = await _topicRepository.SetStatusAsync(item.Id, newStatus);

            if (!updated)
            {
                ErrorMessage = "No se pudo actualizar el estado de la temática. Inténtalo de nuevo.";
                return;
            }

            item.ApplyStatus(newStatus, completedAt);
            UpdateSummary();
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudo actualizar el estado de la temática. Inténtalo de nuevo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateSummary()
    {
        TotalTopics = Topics.Count;
        ActiveTopics = Topics.Count(topic => topic.Status == Topic.StatusActive);
        CompletedTopics = Topics.Count(topic => topic.Status == Topic.StatusCompleted);
        ArchivedTopics = Topics.Count(topic => topic.Status == Topic.StatusArchived);
        TotalMinutesStudied = Topics.Sum(topic => topic.TotalMinutesStudied);
        TotalHoursStudied = TotalMinutesStudied / 60.0;

        _totalTargetHours = Topics
            .Where(topic => topic.HasTarget)
            .Sum(topic => topic.TargetHours);

        var totalTargetMinutes = _totalTargetHours * 60;

        OverallProgressPercentage = totalTargetMinutes > 0
            ? Math.Clamp(TotalMinutesStudied / totalTargetMinutes * 100, 0, 100)
            : 0;

        HasOverallTarget = _totalTargetHours > 0;

        OnPropertyChanged(nameof(HasTopics));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(OverallProgressText));
        OnPropertyChanged(nameof(SummaryHeadline));
    }

    private static string FormatCount(int count, string singular, string plural)
    {
        return $"{count} {(count == 1 ? singular : plural)}";
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
