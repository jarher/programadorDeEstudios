namespace StudyTracker.Core.Interfaces;

public interface ITimerService
{
    TimeSpan RemainingTime { get; }

    TimeSpan ElapsedTime { get; }

    bool IsRunning { get; }

    bool IsPaused { get; }

    event EventHandler<TimeSpan> Tick;

    event EventHandler SessionCompleted;

    void Start(TimeSpan duration);

    void Pause();

    void Resume();

    void Stop();
}
