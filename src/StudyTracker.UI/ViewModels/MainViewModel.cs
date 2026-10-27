using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Gestiona la navegación y los estados globales de la ventana principal.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentView = "Dashboard";

    public MainViewModel(DashboardViewModel dashboard, TimerViewModel timer)
        : this(dashboard, timer, CreateStatsViewModel())
    {
    }

    public MainViewModel(DashboardViewModel dashboard, TimerViewModel timer, StatsViewModel stats)
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(stats);

        Dashboard = dashboard;
        Timer = timer;
        Stats = stats;
    }

    public DashboardViewModel Dashboard { get; }

    public TimerViewModel Timer { get; }

    public StatsViewModel Stats { get; }

    public bool IsDashboardActive => CurrentView == "Dashboard";

    public bool IsTimerActive => CurrentView == "Timer";

    public bool IsStatsActive => CurrentView == "Stats";

    private static StatsViewModel CreateStatsViewModel()
    {
        var database = new StudyTracker.Core.Data.DatabaseInitializer();
        var sessionRepository = new StudyTracker.Core.Data.SessionRepository(database);

        return new StatsViewModel(
            new StudyTracker.Core.Services.StatsService(sessionRepository),
            new StudyTracker.Core.Data.TopicRepository(database));
    }

    [RelayCommand]
    public void NavigateTo(string view)
    {
        if (string.IsNullOrWhiteSpace(view) || CurrentView == view)
        {
            return;
        }

        CurrentView = view;
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsTimerActive));
        OnPropertyChanged(nameof(IsStatsActive));

        if (view == "Timer")
        {
            _ = Timer.LoadTopicsCommand.ExecuteAsync(null);
        }
        else if (view == "Stats")
        {
            _ = Stats.LoadCommand.ExecuteAsync(null);
        }
    }
}
