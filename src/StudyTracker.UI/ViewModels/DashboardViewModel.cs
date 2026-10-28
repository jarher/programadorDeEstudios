using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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
    private static readonly Regex ColorHexPattern = new(
        "^#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITopicRepository _topicRepository;
    private readonly ISessionRepository _sessionRepository;
    private double _totalTargetHours;
    private long _sessionLoadVersion;

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

    [ObservableProperty]
    private TopicItemViewModel? _selectedTopic;

    [ObservableProperty]
    private bool _isLoadingSelectedTopic;

    [ObservableProperty]
    private string? _selectedTopicErrorMessage;

    public DashboardViewModel(ITopicRepository topicRepository, ISessionRepository sessionRepository)
    {
        ArgumentNullException.ThrowIfNull(topicRepository);
        ArgumentNullException.ThrowIfNull(sessionRepository);

        _topicRepository = topicRepository;
        _sessionRepository = sessionRepository;
        Topics.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasTopics));
            OnPropertyChanged(nameof(IsEmpty));
        };
        SelectedSessions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSelectedSessions));
            OnPropertyChanged(nameof(HasNoSelectedSessions));
        };
    }

    public ObservableCollection<TopicItemViewModel> Topics { get; } = [];

    public ObservableCollection<StudySession> SelectedSessions { get; } = [];

    public bool HasTopics => Topics.Count > 0;

    public bool IsEmpty => !IsBusy && !HasTopics;

    public bool HasSelectedTopic => SelectedTopic is not null;

    public bool HasSelectedSessions => SelectedSessions.Count > 0;

    public bool HasSelectedTopicError => !string.IsNullOrEmpty(SelectedTopicErrorMessage);

    public bool HasNoSelectedSessions =>
        HasSelectedTopic
        && !IsLoadingSelectedTopic
        && !HasSelectedSessions
        && !HasSelectedTopicError;

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

    partial void OnSelectedTopicChanging(TopicItemViewModel? value)
    {
        SelectedTopic?.ApplySelection(false);
    }

    partial void OnSelectedTopicChanged(TopicItemViewModel? value)
    {
        value?.ApplySelection(true);
        OnPropertyChanged(nameof(HasSelectedTopic));
        OnPropertyChanged(nameof(HasNoSelectedSessions));
    }

    partial void OnIsLoadingSelectedTopicChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoSelectedSessions));
    }

    partial void OnSelectedTopicErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasSelectedTopicError));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await RefreshAsync();
        }
        catch (Exception)
        {
            Topics.Clear();
            ClearSelectedTopic();
            UpdateSummary();
            ErrorMessage = "No se pudieron cargar las temáticas. Inténtalo de nuevo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task AddTopicAsync(NewTopicRequest? request)
    {
        if (request is null || IsBusy)
        {
            return;
        }

        var title = (request.Title ?? string.Empty).Trim();
        var colorHex = (request.ColorHex ?? string.Empty).Trim();

        var isValid = title.Length is > 0 and <= 120
            && double.IsFinite(request.TargetHours)
            && request.TargetHours is >= 0 and <= 1000
            && ColorHexPattern.IsMatch(colorHex);

        if (!isValid)
        {
            ErrorMessage = "Los datos de la temática no son válidos. Revísalos e inténtalo de nuevo.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

            if (description is { Length: > 400 })
            {
                description = description[..400];
            }

            var topic = new Topic
            {
                Title = title,
                Description = description,
                ColorHex = colorHex.ToUpperInvariant(),
                TargetHours = Math.Round(request.TargetHours, 2, MidpointRounding.AwayFromZero),
                Status = Topic.StatusActive,
                CompletedAt = null,
                CreatedAt = DateTime.Now
            };

            await _topicRepository.CreateAsync(topic);
            await RefreshAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudo crear la temática. Inténtalo de nuevo.";
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

    [RelayCommand]
    public async Task SelectTopicAsync(TopicItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        SelectedTopic = item;
        SelectedSessions.Clear();
        SelectedTopicErrorMessage = null;
        IsLoadingSelectedTopic = true;
        var requestVersion = ++_sessionLoadVersion;

        try
        {
            var sessions = await _sessionRepository.GetByTopicIdAsync(item.Id);

            if (requestVersion != _sessionLoadVersion)
            {
                return;
            }

            foreach (var session in sessions)
            {
                SelectedSessions.Add(session);
            }
        }
        catch (Exception)
        {
            if (requestVersion == _sessionLoadVersion)
            {
                SelectedTopicErrorMessage =
                    "No se pudo cargar el historial de esta temática. Inténtalo de nuevo.";
            }
        }
        finally
        {
            if (requestVersion == _sessionLoadVersion)
            {
                IsLoadingSelectedTopic = false;
            }
        }
    }

    [RelayCommand]
    public void ClearSelectedTopic()
    {
        _sessionLoadVersion++;
        SelectedTopic = null;
        SelectedSessions.Clear();
        SelectedTopicErrorMessage = null;
        IsLoadingSelectedTopic = false;
    }

    private async Task RefreshAsync()
    {
        var selectedTopicId = SelectedTopic?.Id;
        var summaries = await _topicRepository.GetSummariesAsync();
        var items = summaries.Select(summary => new TopicItemViewModel(summary)).ToList();

        Topics.Clear();

        foreach (var item in items)
        {
            Topics.Add(item);
        }

        if (selectedTopicId is not null)
        {
            var refreshedSelection = Topics.FirstOrDefault(topic => topic.Id == selectedTopicId);
            if (refreshedSelection is null)
            {
                ClearSelectedTopic();
            }
            else
            {
                await SelectTopicAsync(refreshedSelection);
            }
        }

        UpdateSummary();
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
