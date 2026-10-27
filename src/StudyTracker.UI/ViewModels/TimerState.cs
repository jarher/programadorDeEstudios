namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Estado observable del temporizador de estudio.
/// </summary>
public enum TimerState
{
    Idle,
    Running,
    Paused,
    Completed,
    Stopped
}
