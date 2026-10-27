using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;
using SkiaSharp;

namespace StudyTracker.UI.ViewModels;

public sealed partial class StatsViewModel : ObservableObject
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-ES");
    private readonly IStatsService _statsService;
    private readonly ITopicRepository _topicRepository;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private int _totalStudyMinutes;

    [ObservableProperty]
    private int _activeDays;

    [ObservableProperty]
    private string _dateRangeText = string.Empty;

    [ObservableProperty]
    private double _goalChartHeight = 240;

    public StatsViewModel(IStatsService statsService, ITopicRepository topicRepository)
    {
        ArgumentNullException.ThrowIfNull(statsService);
        ArgumentNullException.ThrowIfNull(topicRepository);
        _statsService = statsService;
        _topicRepository = topicRepository;
    }

    public ObservableCollection<HeatmapWeekViewModel> Weeks { get; } = [];

    public ObservableCollection<ISeries> TopicDistributionSeries { get; } = [];

    public ObservableCollection<ISeries> GoalProgressSeries { get; } = [];

    public ObservableCollection<TopicChartLegendItem> TopicDistributionLegend { get; } = [];

    public ObservableCollection<TopicGoalProgressItem> TopicGoalProgress { get; } = [];

    public Axis[] GoalXAxes { get; private set; } = [];

    public Axis[] GoalYAxes { get; } =
    [
        new Axis
        {
            Labels = []
        }
    ];

    public int TopicsWithStudyTime { get; private set; }

    public int TopicsWithGoals { get; private set; }

    public bool HasStudyActivity => ActiveDays > 0;

    public bool HasTopicDistribution => TopicDistributionSeries.Count > 0;

    public bool HasGoals => GoalProgressSeries.Count > 0;

    public string TopicsWithStudyTimeText => TopicsWithStudyTime == 1
        ? "1 temática con tiempo registrado"
        : $"{TopicsWithStudyTime} temáticas con tiempo registrado";

    public string TopicsWithGoalsText => TopicsWithGoals == 1
        ? "1 temática con meta"
        : $"{TopicsWithGoals} temáticas con meta";

    public string StudyTimeText
    {
        get
        {
            var hours = TotalStudyMinutes / 60;
            var minutes = TotalStudyMinutes % 60;

            return hours == 0
                ? $"{minutes} min"
                : minutes == 0
                    ? $"{hours} h"
                    : $"{hours} h {minutes} min";
        }
    }

    public string StreakText => CurrentStreak == 1
        ? "1 día"
        : $"{CurrentStreak} días";

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var statisticsTask = _statsService.GetStatisticsAsync();
            var summariesTask = _topicRepository.GetSummariesAsync();
            await Task.WhenAll(statisticsTask, summariesTask);
            var statistics = await statisticsTask;
            var summaries = (await summariesTask).ToList();
            var activityByDate = statistics.DailyActivity.ToDictionary(day => day.Date.Date);
            var weeks = BuildWeeks(statistics.FromDate.Date, statistics.ToDate.Date, activityByDate);

            Weeks.Clear();
            foreach (var week in weeks)
            {
                Weeks.Add(week);
            }

            CurrentStreak = statistics.CurrentStreak;
            TotalStudyMinutes = statistics.DailyActivity.Sum(day => day.TotalMinutes);
            ActiveDays = statistics.DailyActivity.Count(day => day.TotalMinutes > 0);
            DateRangeText = $"{statistics.FromDate.ToString("d MMM", SpanishCulture)} – {statistics.ToDate.ToString("d MMM yyyy", SpanishCulture)}";
            OnPropertyChanged(nameof(HasStudyActivity));
            OnPropertyChanged(nameof(StudyTimeText));
            OnPropertyChanged(nameof(StreakText));
            UpdateCharts(summaries);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"No se pudieron cargar las estadísticas de estudio: {ex}");
            ErrorMessage = "No se pudieron cargar las estadísticas. Inténtalo de nuevo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCharts(IReadOnlyCollection<TopicSummaryDto> summaries)
    {
        var studiedTopics = summaries
            .Where(summary => summary.TotalMinutesStudied > 0)
            .ToList();
        var totalMinutes = studiedTopics.Sum(summary => summary.TotalMinutesStudied);
        var pieSeries = studiedTopics
            .Select(summary =>
            {
                var share = summary.TotalMinutesStudied / (double)totalMinutes * 100;
                var color = SKColor.Parse(summary.Topic.ColorHex);

                return (ISeries)new PieSeries<double>
                {
                    Name = $"{summary.Topic.Title} ({share:0.#} %)",
                    Values = [summary.TotalMinutesStudied],
                    Fill = new SolidColorPaint(color),
                    InnerRadius = 52
                };
            })
            .ToList();

        TopicDistributionSeries.Clear();
        foreach (var series in pieSeries)
        {
            TopicDistributionSeries.Add(series);
        }

        TopicsWithStudyTime = studiedTopics.Count;
        OnPropertyChanged(nameof(HasTopicDistribution));
        OnPropertyChanged(nameof(TopicsWithStudyTime));
        OnPropertyChanged(nameof(TopicsWithStudyTimeText));

        TopicDistributionLegend.Clear();
        foreach (var summary in studiedTopics.OrderByDescending(item => item.TotalMinutesStudied))
        {
            var share = summary.TotalMinutesStudied / (double)totalMinutes * 100;
            TopicDistributionLegend.Add(new TopicChartLegendItem(
                summary.Topic.Title,
                summary.Topic.ColorHex,
                $"{share:0.#} %",
                FormatStudyTime(summary.TotalMinutesStudied)));
        }

        var topicsWithGoals = summaries
            .Where(summary => summary.Topic.TargetHours > 0)
            .ToList();
        var values = topicsWithGoals
            .Select(summary => Math.Clamp(summary.ProgressPercentage, 0, 100))
            .ToArray();

        GoalProgressSeries.Clear();
        if (topicsWithGoals.Count > 0)
        {
            GoalProgressSeries.Add(new RowSeries<double>
            {
                Name = "Avance de la meta",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#0F6CBD")),
                MaxBarWidth = 22
            });
        }

        GoalXAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = value => $"{value:0}%"
            }
        ];
        GoalYAxes[0].Labels = topicsWithGoals.Select(summary => summary.Topic.Title).ToArray();
        TopicGoalProgress.Clear();
        foreach (var summary in topicsWithGoals)
        {
            TopicGoalProgress.Add(new TopicGoalProgressItem(
                summary.Topic.Title,
                summary.Topic.ColorHex,
                Math.Clamp(summary.ProgressPercentage, 0, 100),
                FormatStudyTime(summary.TotalMinutesStudied),
                $"{summary.Topic.TargetHours:0.#} h"));
        }

        TopicsWithGoals = topicsWithGoals.Count;
        GoalChartHeight = Math.Clamp(topicsWithGoals.Count * 42, 220, 420);
        OnPropertyChanged(nameof(GoalXAxes));
        OnPropertyChanged(nameof(GoalYAxes));
        OnPropertyChanged(nameof(HasGoals));
        OnPropertyChanged(nameof(TopicsWithGoals));
        OnPropertyChanged(nameof(TopicsWithGoalsText));
    }

    private static string FormatStudyTime(int totalMinutes)
    {
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return hours == 0
            ? $"{minutes} min"
            : minutes == 0
                ? $"{hours} h"
                : $"{hours} h {minutes} min";
    }

    private static IReadOnlyList<HeatmapWeekViewModel> BuildWeeks(
        DateTime fromDate,
        DateTime toDate,
        IReadOnlyDictionary<DateTime, DailyActivityDto> activityByDate)
    {
        var firstWeekStart = StartOfWeek(fromDate);
        var lastWeekStart = StartOfWeek(toDate);
        var weeks = new List<HeatmapWeekViewModel>();

        for (var weekStart = firstWeekStart; weekStart <= lastWeekStart; weekStart = weekStart.AddDays(7))
        {
            var days = new List<HeatmapDayViewModel>(7);

            for (var offset = 0; offset < 7; offset++)
            {
                var date = weekStart.AddDays(offset);
                var isInRange = date >= fromDate && date <= toDate;
                activityByDate.TryGetValue(date, out var activity);
                days.Add(new HeatmapDayViewModel(date, activity, isInRange));
            }

            var firstOfMonth = days.FirstOrDefault(day => day.IsInRange && day.Date.Day == 1);
            var firstInRangeDay = days.First(day => day.IsInRange);
            var monthDate = firstOfMonth?.Date ?? (weeks.Count == 0 ? firstInRangeDay.Date : (DateTime?)null);
            var monthLabel = monthDate?.ToString("MMM", SpanishCulture) ?? string.Empty;

            weeks.Add(new HeatmapWeekViewModel(days, monthLabel));
        }

        return weeks;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday).Date;
    }
}

public sealed class HeatmapWeekViewModel(IReadOnlyList<HeatmapDayViewModel> days, string monthLabel)
{
    public IReadOnlyList<HeatmapDayViewModel> Days { get; } = days;

    public string MonthLabel { get; } = monthLabel;
}

public sealed class HeatmapDayViewModel
{
    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-ES");

    public HeatmapDayViewModel(DateTime date, DailyActivityDto? activity, bool isInRange)
    {
        Date = date;
        IsInRange = isInRange;
        TotalMinutes = isInRange ? activity?.TotalMinutes ?? 0 : 0;
        IntensityLevel = isInRange ? activity?.IntensityLevel ?? 0 : 0;
        Tooltip = !isInRange
            ? string.Empty
            : TotalMinutes > 0
                ? $"{date.ToString("d MMM yyyy", SpanishCulture)}: {TotalMinutes} min de estudio"
                : $"{date.ToString("d MMM yyyy", SpanishCulture)}: sin estudio";
    }

    public DateTime Date { get; }

    public bool IsInRange { get; }

    public int TotalMinutes { get; }

    public int IntensityLevel { get; }

    public string Tooltip { get; }
}

public sealed record TopicChartLegendItem(
    string Title,
    string ColorHex,
    string PercentageText,
    string StudyTimeText);

public sealed record TopicGoalProgressItem(
    string Title,
    string ColorHex,
    double ProgressPercentage,
    string StudyTimeText,
    string TargetHoursText);
