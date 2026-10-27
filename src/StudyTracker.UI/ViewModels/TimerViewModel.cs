using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;

namespace StudyTracker.UI.ViewModels;

/// <summary>
/// Estado y comandos del temporizador de estudio. Conecta la temática seleccionada
/// y la duración configurada con <see cref="ITimerService"/>.
/// </summary>
public sealed partial class TimerViewModel : ObservableObject, IDisposable
{
    private const double MinimumPlannedMinutes = 0.1;
    private const double MaximumPlannedMinutes = 480;

    private readonly ITimerService _timerService;
    private readonly ITopicRepository _topicRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly SynchronizationContext? _syncContext;
    private readonly object _sessionDurationGate = new();

    private TimeSpan _sessionDuration;
    private StudySession? _pendingSession;

    private int _disposed;

    [ObservableProperty]
    private Topic? _selectedTopic;

    [ObservableProperty]
    private bool _isLoadingTopics;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _plannedMinutesText = "25";

    [ObservableProperty]
    private TimerState _state = TimerState.Idle;

    [ObservableProperty]
    private TimeSpan _remainingTime;

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private DateTimeOffset? _sessionStartedAt;

    [ObservableProperty]
    private DateTimeOffset? _sessionEndedAt;

    [ObservableProperty]
    private int _sessionPlannedMinutes;

    [ObservableProperty]
    private bool _isAwaitingJournal;

    [ObservableProperty]
    private string? _pendingJournalNotes;

    public TimerViewModel(
        ITimerService timerService,
        ITopicRepository topicRepository,
        ISessionRepository sessionRepository)
    {
        ArgumentNullException.ThrowIfNull(timerService);
        ArgumentNullException.ThrowIfNull(topicRepository);
        ArgumentNullException.ThrowIfNull(sessionRepository);

        _timerService = timerService;
        _topicRepository = topicRepository;
        _sessionRepository = sessionRepository;
        _syncContext = SynchronizationContext.Current;

        _timerService.Tick += OnTimerTick;
        _timerService.SessionCompleted += OnSessionCompleted;

        Topics.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasTopics));
    }

    public ObservableCollection<Topic> Topics { get; } = [];

    public bool HasTopics => Topics.Count > 0;

    public IReadOnlyList<int> DurationPresets { get; } = [25, 45, 60];

    public bool HasValidDuration => TryParsePlannedMinutes(PlannedMinutesText, out _);

    public TimeSpan? PlannedDuration => TryParsePlannedMinutes(PlannedMinutesText, out var minutes)
        ? TimeSpan.FromMinutes(minutes)
        : null;

    public string PlannedDurationText => PlannedDuration is { } duration
        ? FormatPlannedDuration(duration)
        : "Duración no válida";

    public bool IsIdle => State == TimerState.Idle;

    public bool IsRunning => State == TimerState.Running;

    public bool IsPaused => State == TimerState.Paused;

    public bool IsCompleted => State == TimerState.Completed;

    public bool IsStopped => State == TimerState.Stopped;

    public bool IsActive => IsRunning || IsPaused;

    public bool CanConfigure =>
        !IsAwaitingJournal && State is (TimerState.Idle or TimerState.Completed or TimerState.Stopped);

    public bool CanStart => SelectedTopic is not null && HasValidDuration && CanConfigure;

    public bool CanPause => State == TimerState.Running;

    public bool CanResume => State == TimerState.Paused;

    public bool CanStop => State is TimerState.Running or TimerState.Paused;

    public string RemainingTimeText => FormatTime(RemainingTime);

    public string ElapsedTimeText => FormatTime(ElapsedTime);

    public string ProgressText => $"{ProgressPercentage.ToString("0", CultureInfo.InvariantCulture)} %";

    public string StatusText => State switch
    {
        TimerState.Running => "En curso",
        TimerState.Paused => "En pausa",
        TimerState.Completed => "Sesión completada",
        TimerState.Stopped => "Sesión detenida",
        _ => "Listo para empezar"
    };

    public event EventHandler? JournalRequested;

    [RelayCommand]
    public async Task LoadTopicsAsync()
    {
        if (IsLoadingTopics)
        {
            return;
        }

        IsLoadingTopics = true;
        ErrorMessage = null;

        try
        {
            var activeTopics = (await _topicRepository.GetAllAsync())
                .Where(topic => topic.Status == Topic.StatusActive)
                .ToList();

            var selectedId = SelectedTopic?.Id;

            Topics.Clear();

            foreach (var topic in activeTopics)
            {
                Topics.Add(topic);
            }

            SelectedTopic = selectedId is { } id
                ? Topics.FirstOrDefault(topic => topic.Id == id)
                : null;

            if (SelectedTopic is null && Topics.Count > 0)
            {
                SelectedTopic = Topics[0];
            }
        }
        catch (Exception)
        {
            ErrorMessage = "No se pudieron cargar las temáticas para el temporizador. Inténtalo de nuevo.";
        }
        finally
        {
            IsLoadingTopics = false;
        }
    }

    [RelayCommand]
    public void SelectDuration(int minutes)
    {
        if (!CanConfigure)
        {
            return;
        }

        PlannedMinutesText = minutes.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    public void Start()
    {
        if (!CanStart || PlannedDuration is not { } duration)
        {
            return;
        }

        try
        {
            ErrorMessage = null;

            SetSessionDuration(duration);
            SessionStartedAt = DateTimeOffset.Now;
            SessionEndedAt = null;
            ProgressPercentage = 0;
            ElapsedTime = TimeSpan.Zero;
            RemainingTime = duration;
            SessionPlannedMinutes = (int)Math.Ceiling(duration.TotalMinutes);

            _timerService.Start(duration);

            State = TimerState.Running;
        }
        catch (Exception)
        {
            SessionStartedAt = null;
            SessionEndedAt = null;
            SessionPlannedMinutes = 0;
            ProgressPercentage = 0;
            ElapsedTime = TimeSpan.Zero;
            RemainingTime = TimeSpan.Zero;
            ErrorMessage = "No se pudo iniciar el temporizador. Inténtalo de nuevo.";
        }
    }

    [RelayCommand]
    public void Pause()
    {
        if (!CanPause)
        {
            return;
        }

        _timerService.Pause();

        if (_timerService.IsPaused)
        {
            State = TimerState.Paused;
            return;
        }

        // El servicio no pausó porque la sesión ya había terminado: reconcilia
        // el estado terminal sin invalidar el SessionCompleted aún encolado.
        if (!_timerService.IsRunning && _timerService.RemainingTime <= TimeSpan.Zero)
        {
            ApplyCompletedSession();
        }
    }

    [RelayCommand]
    public void Resume()
    {
        if (!CanResume)
        {
            return;
        }

        _timerService.Resume();
        State = TimerState.Running;
    }

    [RelayCommand]
    public void Stop()
    {
        if (!CanStop)
        {
            return;
        }

        _timerService.Stop();
        RefreshFromService();

        if (RemainingTime <= TimeSpan.Zero)
        {
            // La sesión ya había vencido: se registra como completada, no detenida.
            ApplyCompletedSession();
            return;
        }

        State = TimerState.Stopped;
        SessionEndedAt = DateTimeOffset.Now;
        RequestSessionJournal();
    }

    public async Task<bool> SavePendingSessionAsync(string? notes)
    {
        if (_pendingSession is null || !IsAwaitingJournal)
        {
            return false;
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
        normalizedNotes = normalizedNotes is { Length: > 1000 }
            ? normalizedNotes[..1000]
            : normalizedNotes;
        PendingJournalNotes = normalizedNotes;
        _pendingSession.Notes = normalizedNotes;

        try
        {
            await _sessionRepository.RecordSessionAsync(_pendingSession);
        }
        catch (SqliteException ex)
        {
            Debug.WriteLine($"No se pudo guardar la sesión de estudio: {ex}");
            ErrorMessage = "No se pudo guardar la sesión. Revisa el almacenamiento e inténtalo de nuevo.";
            return false;
        }

        _pendingSession = null;
        PendingJournalNotes = null;
        IsAwaitingJournal = false;
        ErrorMessage = null;
        return true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _timerService.Tick -= OnTimerTick;
        _timerService.SessionCompleted -= OnSessionCompleted;
    }

    partial void OnSelectedTopicChanged(Topic? value)
    {
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnPlannedMinutesTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasValidDuration));
        OnPropertyChanged(nameof(PlannedDuration));
        OnPropertyChanged(nameof(PlannedDurationText));
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnStateChanged(TimerState value)
    {
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(CanConfigure));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnIsAwaitingJournalChanged(bool value)
    {
        OnPropertyChanged(nameof(CanConfigure));
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnRemainingTimeChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(RemainingTimeText));
    }

    partial void OnElapsedTimeChanged(TimeSpan value)
    {
        OnPropertyChanged(nameof(ElapsedTimeText));
    }

    partial void OnProgressPercentageChanged(double value)
    {
        OnPropertyChanged(nameof(ProgressText));
    }

    private void OnTimerTick(object? sender, TimeSpan _)
    {
        // El payload del Tick puede pertenecer a una sesión anterior; se ignora.
        // El servicio es la fuente autoritativa y se refresca en el hilo de UI.
        RunOnUiThread(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            RefreshFromService();
        });
    }

    private void OnSessionCompleted(object? sender, EventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (IsDisposed)
            {
                return;
            }

            // Un completion legítimo deja el servicio detenido, no pausado y sin
            // tiempo restante. Si hay otra sesión viva o queda tiempo, el evento
            // es de una sesión anterior y debe descartarse.
            if (_timerService.IsRunning || _timerService.IsPaused)
            {
                return;
            }

            if (_timerService.RemainingTime > TimeSpan.Zero)
            {
                return;
            }

            ApplyCompletedSession();
        });
    }

    /// <summary>
    /// Aplica el estado terminal de la sesión actual. Solo debe invocarse cuando
    /// el servicio ya terminó: detenido, no pausado y sin tiempo restante.
    /// </summary>
    private void ApplyCompletedSession()
    {
        if (State == TimerState.Completed)
        {
            return;
        }

        var duration = GetSessionDuration();

        RemainingTime = TimeSpan.Zero;
        ElapsedTime = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        ProgressPercentage = 100;
        State = TimerState.Completed;
        SessionEndedAt = DateTimeOffset.Now;
        RequestSessionJournal();
    }

    private void RequestSessionJournal()
    {
        if (SessionStartedAt is not { } startedAt
            || SessionEndedAt is not { } endedAt
            || SelectedTopic is not { } topic
            || IsAwaitingJournal)
        {
            return;
        }

        _pendingSession = new StudySession
        {
            TopicId = topic.Id,
            StartedAt = startedAt.LocalDateTime,
            EndedAt = endedAt.LocalDateTime,
            PlannedMinutes = SessionPlannedMinutes,
            ActualMinutes = Math.Max(0, (int)Math.Floor(ElapsedTime.TotalMinutes)),
            SessionType = StudySession.TypeTimed
        };

        PendingJournalNotes = null;
        IsAwaitingJournal = true;
        JournalRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshFromService()
    {
        var duration = GetSessionDuration();
        var remaining = _timerService.RemainingTime;
        var elapsed = _timerService.ElapsedTime;

        RemainingTime = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        ElapsedTime = elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        ProgressPercentage = CalculateProgress(ElapsedTime, duration);
    }

    private void RunOnUiThread(Action action)
    {
        var context = _syncContext;

        if (context is null || SynchronizationContext.Current == context)
        {
            action();
            return;
        }

        context.Post(_ => action(), null);
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    private void SetSessionDuration(TimeSpan duration)
    {
        lock (_sessionDurationGate)
        {
            _sessionDuration = duration;
        }
    }

    private TimeSpan GetSessionDuration()
    {
        lock (_sessionDurationGate)
        {
            return _sessionDuration;
        }
    }

    private static double CalculateProgress(TimeSpan elapsed, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return 0;
        }

        var percentage = elapsed.TotalMilliseconds / duration.TotalMilliseconds * 100;

        return double.IsFinite(percentage)
            ? Math.Clamp(percentage, 0, 100)
            : 0;
    }

    private static bool TryParsePlannedMinutes(string? text, out double minutes)
    {
        minutes = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');

        if (!double.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out minutes))
        {
            return false;
        }

        return double.IsFinite(minutes)
            && minutes is >= MinimumPlannedMinutes and <= MaximumPlannedMinutes;
    }

    private static string FormatPlannedDuration(TimeSpan duration)
    {
        var totalMinutes = duration.TotalMinutes;

        if (totalMinutes < 60)
        {
            return $"{totalMinutes.ToString("0.##", CultureInfo.CurrentCulture)} min";
        }

        var roundedMinutes = (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero);
        var hours = roundedMinutes / 60;
        var remainingMinutes = roundedMinutes % 60;

        return remainingMinutes == 0
            ? $"{hours} h"
            : $"{hours} h {remainingMinutes} min";
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }

        var totalHours = (int)time.TotalHours;

        return totalHours >= 1
            ? $"{totalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }
}
