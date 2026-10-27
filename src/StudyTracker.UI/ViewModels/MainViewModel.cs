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
    {
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(timer);

        Dashboard = dashboard;
        Timer = timer;
    }

    public DashboardViewModel Dashboard { get; }

    public TimerViewModel Timer { get; }

    public bool IsDashboardActive => CurrentView == "Dashboard";

    public bool IsTimerActive => CurrentView == "Timer";

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

        if (view == "Timer")
        {
            _ = Timer.LoadTopicsCommand.ExecuteAsync(null);
        }
    }
}
