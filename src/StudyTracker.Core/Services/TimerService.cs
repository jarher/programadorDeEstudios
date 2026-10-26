using StudyTracker.Core.Interfaces;

namespace StudyTracker.Core.Services;

public sealed class TimerService : ITimerService, IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();

    private ITimer? _timer;
    private TimeSpan _duration;
    private TimeSpan _accumulated;
    private DateTimeOffset _runStartedAt;
    private bool _running;
    private bool _paused;
    private bool _disposed;

    public TimerService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public TimeSpan RemainingTime
    {
        get
        {
            lock (_gate)
            {
                return GetRemaining(GetElapsed());
            }
        }
    }

    public TimeSpan ElapsedTime
    {
        get
        {
            lock (_gate)
            {
                return GetElapsed();
            }
        }
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _running;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _paused;
            }
        }
    }

    public event EventHandler<TimeSpan> Tick = delegate { };

    public event EventHandler SessionCompleted = delegate { };

    public void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
        }

        EventHandler<TimeSpan>? tick;
        TimeSpan remaining;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            StopTimer();
            _duration = duration;
            _accumulated = TimeSpan.Zero;
            _runStartedAt = _timeProvider.GetUtcNow();
            _running = true;
            _paused = false;
            StartTimer();
            remaining = _duration;
            tick = Tick;
        }

        tick?.Invoke(this, remaining);
    }

    public void Pause()
    {
        EventHandler<TimeSpan>? tick;
        EventHandler? completed = null;
        TimeSpan remaining;

        lock (_gate)
        {
            if (!_running || _paused)
            {
                return;
            }

            var elapsed = GetElapsed();
            if (elapsed >= _duration)
            {
                Complete();
                remaining = TimeSpan.Zero;
                completed = SessionCompleted;
            }
            else
            {
                _accumulated = elapsed;
                _paused = true;
                StopTimer();
                remaining = GetRemaining(elapsed);
            }

            tick = Tick;
        }

        tick?.Invoke(this, remaining);
        completed?.Invoke(this, EventArgs.Empty);
    }

    public void Resume()
    {
        EventHandler<TimeSpan>? tick;
        TimeSpan remaining;

        lock (_gate)
        {
            if (!_running || !_paused)
            {
                return;
            }

            _runStartedAt = _timeProvider.GetUtcNow();
            _paused = false;
            StartTimer();
            remaining = GetRemaining(GetElapsed());
            tick = Tick;
        }

        tick?.Invoke(this, remaining);
    }

    public void Stop()
    {
        EventHandler<TimeSpan>? tick;
        TimeSpan remaining;

        lock (_gate)
        {
            if (!_running && !_paused)
            {
                return;
            }

            _accumulated = GetElapsed();
            _running = false;
            _paused = false;
            StopTimer();
            remaining = GetRemaining(_accumulated);
            tick = Tick;
        }

        tick?.Invoke(this, remaining);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _running = false;
            _paused = false;
            StopTimer();
        }
    }

    private void OnTick()
    {
        EventHandler<TimeSpan>? tick;
        EventHandler? completed = null;
        TimeSpan remaining;

        lock (_gate)
        {
            if (!_running || _paused)
            {
                return;
            }

            var elapsed = GetElapsed();
            if (elapsed >= _duration)
            {
                Complete();
                remaining = TimeSpan.Zero;
                completed = SessionCompleted;
            }
            else
            {
                remaining = GetRemaining(elapsed);
            }

            tick = Tick;
        }

        tick?.Invoke(this, remaining);
        completed?.Invoke(this, EventArgs.Empty);
    }

    private void Complete()
    {
        _accumulated = _duration;
        _running = false;
        _paused = false;
        StopTimer();
    }

    private TimeSpan GetElapsed()
    {
        if (!_running || _paused)
        {
            return _accumulated;
        }

        var elapsed = _accumulated + (_timeProvider.GetUtcNow() - _runStartedAt);
        return elapsed > _duration ? _duration : elapsed;
    }

    private TimeSpan GetRemaining(TimeSpan elapsed)
    {
        var remaining = _duration - elapsed;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    private void StartTimer()
    {
        _timer = _timeProvider.CreateTimer(_ => OnTick(), null, TickInterval, TickInterval);
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
