using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Time.Testing;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;
using StudyTracker.Core.Services;
using StudyTracker.UI.ViewModels;

var repoRoot = args.Length == 1
    ? Path.GetFullPath(args[0])
    : throw new ArgumentException("Debe indicarse la raíz del repositorio.");

var ui = new QueueSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(ui);

var checker = new Checker();

checker.Scenario("D1. Constructor rechaza dependencias nulas", () =>
{
    checker.Throws<ArgumentNullException>(
        () => _ = new TimerViewModel(null!, new FakeTopicRepository()),
        "timerService nulo");

    checker.Throws<ArgumentNullException>(
        () => _ = new TimerViewModel(new FakeTimerService(), null!),
        "topicRepository nulo");
});

checker.Scenario("D2. Estado inicial, valores por defecto y comandos", () =>
{
    using var vm = new TimerViewModel(new FakeTimerService(), new FakeTopicRepository());

    checker.Eq(TimerState.Idle, vm.State, "State");
    checker.True(vm.IsIdle, "IsIdle");
    checker.True(!vm.IsRunning && !vm.IsPaused && !vm.IsCompleted && !vm.IsStopped, "estados terminales");
    checker.True(!vm.IsActive, "IsActive");
    checker.True(vm.CanConfigure, "CanConfigure");
    checker.True(!vm.CanStart, "CanStart sin temática");
    checker.True(!vm.CanPause && !vm.CanResume && !vm.CanStop, "comandos deshabilitados");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "RemainingTime");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "ElapsedTime");
    checker.Eq(0d, vm.ProgressPercentage, "ProgressPercentage");
    checker.Null(vm.SessionStartedAt, "SessionStartedAt");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt");
    checker.Null(vm.SelectedTopic, "SelectedTopic");
    checker.True(!vm.HasTopics, "HasTopics");
    checker.Eq("25", vm.PlannedMinutesText, "PlannedMinutesText");
    checker.Eq(25d, vm.PlannedDuration!.Value.TotalMinutes, "PlannedDuration");
    checker.Eq("25 min", vm.PlannedDurationText, "PlannedDurationText");
    checker.Eq("Listo para empezar", vm.StatusText, "StatusText");
    checker.Eq(3, vm.DurationPresets.Count, "presets");
    checker.Eq(25, vm.DurationPresets[0], "preset 25");
    checker.Eq(45, vm.DurationPresets[1], "preset 45");
    checker.Eq(60, vm.DurationPresets[2], "preset 60");
});

checker.Scenario("D3. Validación de duración manual", () =>
{
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(new FakeTimerService(), repo)
    {
        SelectedTopic = repo.Topics[0]
    };

    vm.PlannedMinutesText = "0.1";
    checker.True(vm.HasValidDuration, "0.1 válido");
    checker.Eq(TimeSpan.FromSeconds(6), vm.PlannedDuration!.Value, "0.1 min");
    checker.Contains(vm.PlannedDurationText, "min", "texto 0.1");

    vm.PlannedMinutesText = "12,5";
    checker.True(vm.HasValidDuration, "coma decimal válida");
    checker.Eq(12.5d, vm.PlannedDuration!.Value.TotalMinutes, "12,5 min");

    vm.PlannedMinutesText = "480";
    checker.True(vm.HasValidDuration, "480 válido");
    checker.Eq("8 h", vm.PlannedDurationText, "texto 480");

    foreach (var invalid in new[] { "480.1", "0", "-1", "abc", "", "NaN", "Infinity" })
    {
        vm.PlannedMinutesText = invalid;
        checker.True(!vm.HasValidDuration, $"'{invalid}' inválido");
        checker.Null(vm.PlannedDuration, $"PlannedDuration '{invalid}'");
        checker.Eq("Duración no válida", vm.PlannedDurationText, $"texto '{invalid}'");
    }

    vm.PlannedMinutesText = "25";
    checker.True(vm.CanStart, "CanStart con temática y duración válidas");
});

checker.Scenario("D4. Presets y bloqueo durante sesión activa", () =>
{
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(new FakeTimerService(), repo)
    {
        SelectedTopic = repo.Topics[0]
    };

    vm.SelectDuration(45);
    checker.Eq("45", vm.PlannedMinutesText, "preset 45");

    vm.PlannedMinutesText = "1";
    vm.Start();
    vm.SelectDuration(60);
    checker.Eq("1", vm.PlannedMinutesText, "preset ignorado en Running");

    vm.Pause();
    vm.SelectDuration(25);
    checker.Eq("1", vm.PlannedMinutesText, "preset ignorado en Paused");

    vm.Stop();
    vm.SelectDuration(60);
    checker.Eq("60", vm.PlannedMinutesText, "preset permitido en Stopped");
});

checker.Scenario("D5. Carga de temáticas activas, selección y error", () =>
{
    var repo = new FakeTopicRepository(
        ActiveTopic(1, "C#"),
        InactiveTopic(2, "Archivada"),
        ActiveTopic(3, "SQL"));

    using (var vm = new TimerViewModel(new FakeTimerService(), repo))
    {
        PumpUntilComplete(vm.LoadTopicsAsync(), ui);

        checker.Eq(2, vm.Topics.Count, "solo activas");
        checker.True(vm.HasTopics, "HasTopics");
        checker.Eq(1, vm.Topics[0].Id, "primera temática");
        checker.Eq(3, vm.Topics[1].Id, "segunda temática");
        checker.Eq(1, vm.SelectedTopic!.Id, "autoselección primera");
        checker.True(!vm.IsLoadingTopics, "IsLoadingTopics restaurado");
        checker.Null(vm.ErrorMessage, "sin error");

        vm.SelectedTopic = vm.Topics[1];
        PumpUntilComplete(vm.LoadTopicsAsync(), ui);
        checker.Eq(3, vm.SelectedTopic!.Id, "selección preservada");
    }

    using (var vm = new TimerViewModel(new FakeTimerService(), new FakeTopicRepository()))
    {
        PumpUntilComplete(vm.LoadTopicsAsync(), ui);
        checker.Eq(0, vm.Topics.Count, "lista vacía");
        checker.Null(vm.SelectedTopic, "sin selección");
        checker.True(!vm.HasTopics, "HasTopics vacío");
    }

    var failing = new FakeTopicRepository { ThrowOnGetAll = true };
    using (var vm = new TimerViewModel(new FakeTimerService(), failing))
    {
        PumpUntilComplete(vm.LoadTopicsAsync(), ui);
        checker.Eq(0, vm.Topics.Count, "no conserva temáticas tras error");
        checker.True(!vm.IsLoadingTopics, "IsLoadingTopics restaurado tras error");
        checker.Eq(
            "No se pudieron cargar las temáticas para el temporizador. Inténtalo de nuevo.",
            vm.ErrorMessage,
            "mensaje de error de carga");
    }
});

checker.Scenario("D6. Start valida temática, duración y estado", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo);

    vm.Start();
    checker.Eq(0, service.StartCalls, "sin temática no inicia");
    checker.Eq(TimerState.Idle, vm.State, "sigue Idle sin temática");

    vm.SelectedTopic = repo.Topics[0];
    vm.PlannedMinutesText = "0";
    vm.Start();
    checker.Eq(0, service.StartCalls, "duración inválida no inicia");
    checker.Eq(TimerState.Idle, vm.State, "sigue Idle con duración inválida");

    vm.PlannedMinutesText = "25";
    vm.Start();
    checker.Eq(1, service.StartCalls, "inicio válido");
    var startedAt = vm.SessionStartedAt;

    vm.Start();
    checker.Eq(1, service.StartCalls, "Start ignorado en Running");
    checker.Eq(startedAt, vm.SessionStartedAt, "SessionStartedAt no cambia");

    vm.Pause();
    vm.Start();
    checker.Eq(1, service.StartCalls, "Start ignorado en Paused");
    checker.Eq(TimerState.Paused, vm.State, "sigue Paused");
});

checker.Scenario("D7. Start exitoso publica estado y tiempos", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();

    checker.Eq(1, service.StartCalls, "StartCalls");
    checker.Eq(TimeSpan.FromMinutes(25), service.StartedDuration, "duración enviada");
    checker.Eq(TimerState.Running, vm.State, "State");
    checker.True(vm.IsRunning && vm.IsActive, "flags activos");
    checker.Eq(TimeSpan.FromMinutes(25), vm.RemainingTime, "RemainingTime");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "ElapsedTime");
    checker.Eq(0d, vm.ProgressPercentage, "ProgressPercentage");
    checker.Eq(25, vm.SessionPlannedMinutes, "SessionPlannedMinutes");
    checker.NotNull(vm.SessionStartedAt, "SessionStartedAt");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt");
    checker.True(!vm.CanStart && !vm.CanConfigure, "configuración bloqueada");
    checker.True(vm.CanPause && vm.CanStop && !vm.CanResume, "comandos en Running");
    checker.Eq("25:00", vm.RemainingTimeText, "RemainingTimeText");
    checker.Eq("00:00", vm.ElapsedTimeText, "ElapsedTimeText");
    checker.Eq("0 %", vm.ProgressText, "ProgressText");
    checker.Eq("En curso", vm.StatusText, "StatusText");
});

checker.Scenario("D8. Pause conserva tiempo efectivo y bloquea doble pausa", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(10));
    service.RaiseTick(TimeSpan.FromMinutes(15));
    vm.Pause();

    checker.Eq(1, service.PauseCalls, "PauseCalls");
    checker.True(service.IsPaused, "servicio pausado");
    checker.Eq(TimerState.Paused, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo");
    checker.Eq(40d, vm.ProgressPercentage, "Progreso");
    checker.Null(vm.SessionEndedAt, "sin fin de sesión");
    checker.True(vm.CanResume && vm.CanStop && !vm.CanPause, "comandos en Paused");
    checker.Eq("En pausa", vm.StatusText, "StatusText");

    vm.Pause();
    checker.Eq(1, service.PauseCalls, "segunda pausa ignorada");
});

checker.Scenario("D9. Resume reanuda y acepta ticks nuevos", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(10));
    service.RaiseTick(TimeSpan.FromMinutes(15));
    vm.Pause();
    vm.Resume();

    checker.Eq(1, service.ResumeCalls, "ResumeCalls");
    checker.Eq(TimerState.Running, vm.State, "State");
    checker.True(!service.IsPaused, "servicio reanudado");
    checker.True(vm.CanPause && !vm.CanResume, "comandos en Running");

    service.SetProgress(TimeSpan.FromMinutes(12));
    service.RaiseTick(TimeSpan.FromMinutes(13));
    ui.Drain();

    checker.Eq(TimeSpan.FromMinutes(13), vm.RemainingTime, "Restante tras tick");
    checker.Eq(TimeSpan.FromMinutes(12), vm.ElapsedTime, "Efectivo tras tick");
    checker.Eq(48d, vm.ProgressPercentage, "Progreso tras tick");

    vm.Resume();
    checker.Eq(1, service.ResumeCalls, "segundo resume ignorado");
});

checker.Scenario("D10. Stop conserva tiempo parcial y permite reconfigurar", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(10));
    service.RaiseTick(TimeSpan.FromMinutes(15));
    vm.Stop();

    checker.Eq(1, service.StopCalls, "StopCalls");
    checker.Eq(TimerState.Stopped, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo");
    checker.Eq(40d, vm.ProgressPercentage, "Progreso");
    checker.NotNull(vm.SessionEndedAt, "SessionEndedAt");
    checker.True(vm.CanConfigure && vm.CanStart, "configuración habilitada");
    checker.True(!vm.CanPause && !vm.CanResume && !vm.CanStop, "comandos desactivados");
    checker.Eq("Sesión detenida", vm.StatusText, "StatusText");

    vm.Stop();
    checker.Eq(1, service.StopCalls, "segundo Stop ignorado");

    vm.Start();
    checker.Eq(2, service.StartCalls, "nueva sesión desde Stopped");
    checker.Eq(TimerState.Running, vm.State, "nueva sesión en Running");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt limpiado");
});

checker.Scenario("D11. Tick refresca desde el servicio e ignora el payload", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(10));
    service.RaiseTick(TimeSpan.FromMinutes(3));

    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "Restante desde servicio");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo desde servicio");
    checker.Eq(40d, vm.ProgressPercentage, "Progreso desde servicio");
    checker.Eq("15:00", vm.RemainingTimeText, "texto restante");
    checker.Eq("10:00", vm.ElapsedTimeText, "texto efectivo");

    service.RaiseTick(TimeSpan.FromMinutes(-5));

    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "payload negativo ignorado");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "efectivo sin cambios");
    checker.Eq(40d, vm.ProgressPercentage, "progreso sin cambios");
});

checker.Scenario("D12. SessionCompleted normal finaliza una sola vez", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;

    RunOffUiThread(service.CompleteFromBackground);
    checker.Eq(TimerState.Running, vm.State, "estado antes de drenar");
    checker.True(ui.PendingCount >= 2, "tick y completion encolados");

    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "State");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(25), vm.ElapsedTime, "Efectivo");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.NotNull(vm.SessionEndedAt, "SessionEndedAt");
    checker.Eq(1, endedChanges, "una única notificación de fin");
    checker.Eq(0, ui.PendingCount, "cola drenada");
    checker.True(!vm.IsActive && vm.CanConfigure && vm.CanStart, "estado final");
    checker.Eq("Sesión completada", vm.StatusText, "StatusText");
});

checker.Scenario("D13. Dispose desuscribe eventos e ignora ticks posteriores", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    checker.Eq(1, service.TickSubscribers, "suscriptor Tick");
    checker.Eq(1, service.CompletionSubscribers, "suscriptor SessionCompleted");

    vm.Dispose();

    checker.Eq(0, service.TickSubscribers, "Tick desuscrito");
    checker.Eq(0, service.CompletionSubscribers, "SessionCompleted desuscrito");

    RunOffUiThread(() => service.RaiseTick(TimeSpan.FromMinutes(5)));
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "estado sin cambios tras Dispose");
    checker.Eq(TimeSpan.FromMinutes(25), vm.RemainingTime, "tick ignorado tras Dispose");
    checker.Eq(0, ui.PendingCount, "sin callbacks pendientes");

    vm.Dispose();
});

checker.Scenario("D14. Auditoría estática del fuente real", () =>
{
    var sourcePath = Path.Combine(
        repoRoot,
        "src",
        "StudyTracker.UI",
        "ViewModels",
        "TimerViewModel.cs");

    var source = File.ReadAllText(sourcePath);
    var start = ExtractMethod(source, "public void Start()");
    var pause = ExtractMethod(source, "public void Pause()");
    var resume = ExtractMethod(source, "public void Resume()");
    var stop = ExtractMethod(source, "public void Stop()");
    var tick = ExtractMethod(source, "private void OnTimerTick");
    var completed = ExtractMethod(source, "private void OnSessionCompleted");
    var refresh = ExtractMethod(source, "private void RefreshFromService()");
    var apply = ExtractMethod(source, "private void ApplyCompletedSession()");

    checker.True(!source.Contains("sessionGeneration", StringComparison.Ordinal),
        "sin campo/uso de sessionGeneration");
    checker.True(!source.Contains("Interlocked.Increment", StringComparison.Ordinal),
        "sin incrementos de época");
    checker.True(!start.Contains("Interlocked", StringComparison.Ordinal)
        && !pause.Contains("Interlocked", StringComparison.Ordinal)
        && !resume.Contains("Interlocked", StringComparison.Ordinal)
        && !stop.Contains("Interlocked", StringComparison.Ordinal),
        "Start/Pause/Resume/Stop sin sincronización de época");
    checker.True(source.Contains("Interlocked.Exchange(ref _disposed, 1)", StringComparison.Ordinal)
        && source.Contains("Volatile.Read(ref _disposed)", StringComparison.Ordinal),
        "Dispose conserva Interlocked.Exchange y Volatile.Read");

    checker.True(tick.Contains("TimeSpan _", StringComparison.Ordinal)
        && !tick.Contains("remaining", StringComparison.Ordinal),
        "OnTimerTick ignora el payload");
    checker.True(tick.Contains("RunOnUiThread", StringComparison.Ordinal)
        && tick.Contains("if (IsDisposed)", StringComparison.Ordinal)
        && tick.Contains("RefreshFromService();", StringComparison.Ordinal),
        "OnTimerTick refresca en UI y respeta Dispose");
    checker.True(tick.IndexOf("if (IsDisposed)", StringComparison.Ordinal)
        < tick.IndexOf("RefreshFromService();", StringComparison.Ordinal),
        "OnTimerTick valida Dispose antes de refrescar");

    checker.True(refresh.Contains("_timerService.RemainingTime", StringComparison.Ordinal)
        && refresh.Contains("_timerService.ElapsedTime", StringComparison.Ordinal)
        && refresh.Contains("CalculateProgress(ElapsedTime, duration)", StringComparison.Ordinal),
        "RefreshFromService usa el servicio como fuente autoritativa");

    checker.True(!completed.Contains("_sessionGeneration", StringComparison.Ordinal)
        && !completed.Contains("Interlocked", StringComparison.Ordinal),
        "OnSessionCompleted no toca épocas");
    checker.True(
        completed.Contains("if (IsDisposed)", StringComparison.Ordinal)
        && completed.Contains("if (_timerService.IsRunning || _timerService.IsPaused)", StringComparison.Ordinal)
        && completed.Contains("if (_timerService.RemainingTime > TimeSpan.Zero)", StringComparison.Ordinal)
        && completed.Contains("ApplyCompletedSession();", StringComparison.Ordinal),
        "OnSessionCompleted filtra servicio vivo e completion obsoleto");
    checker.True(
        completed.IndexOf("if (IsDisposed)", StringComparison.Ordinal)
        < completed.IndexOf("if (_timerService.IsRunning || _timerService.IsPaused)", StringComparison.Ordinal)
        && completed.IndexOf("if (_timerService.IsRunning || _timerService.IsPaused)", StringComparison.Ordinal)
        < completed.IndexOf("if (_timerService.RemainingTime > TimeSpan.Zero)", StringComparison.Ordinal)
        && completed.IndexOf("if (_timerService.RemainingTime > TimeSpan.Zero)", StringComparison.Ordinal)
        < completed.IndexOf("ApplyCompletedSession();", StringComparison.Ordinal),
        "orden de guardas de OnSessionCompleted");

    checker.True(pause.Contains("if (_timerService.IsPaused)", StringComparison.Ordinal)
        && pause.Contains("!_timerService.IsRunning && _timerService.RemainingTime <= TimeSpan.Zero", StringComparison.Ordinal)
        && pause.Contains("ApplyCompletedSession();", StringComparison.Ordinal),
        "Pause pausa o reconcilia sesión terminada");

    var applyGuard = apply.IndexOf("if (State == TimerState.Completed)", StringComparison.Ordinal);
    checker.True(applyGuard >= 0
        && applyGuard < apply.IndexOf("RemainingTime = TimeSpan.Zero", StringComparison.Ordinal)
        && applyGuard < apply.IndexOf("ProgressPercentage = 100", StringComparison.Ordinal)
        && applyGuard < apply.IndexOf("State = TimerState.Completed", StringComparison.Ordinal)
        && applyGuard < apply.IndexOf("SessionEndedAt = DateTimeOffset.Now", StringComparison.Ordinal),
        "ApplyCompletedSession es idempotente");
    checker.True(source.Contains("Solo debe invocarse cuando", StringComparison.Ordinal)
        && source.Contains("detenido, no pausado y sin tiempo restante", StringComparison.Ordinal),
        "ApplyCompletedSession documenta su precondición");

    checker.True(source.Contains("context.Post(_ => action(), null)", StringComparison.Ordinal)
        && !source.Contains("context.Send(", StringComparison.Ordinal),
        "eventos publicados de forma asíncrona al contexto de UI");
    checker.True(!source.Contains("Dapper", StringComparison.Ordinal)
        && !source.Contains("Microsoft.Data.Sqlite", StringComparison.Ordinal)
        && !source.Contains("MessageBox", StringComparison.Ordinal)
        && !source.Contains("System.Windows", StringComparison.Ordinal),
        "ViewModel sin SQL ni dependencias directas de WPF");
    checker.True(!source.Contains(".Result", StringComparison.Ordinal)
        && !source.Contains(".Wait(", StringComparison.Ordinal),
        "sin bloqueos síncronos sobre tareas");
});

checker.Scenario("D15. Error del servicio al iniciar se comunica y no deja sesión activa", () =>
{
    var service = new FakeTimerService { ThrowOnStart = true };
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();

    checker.Eq(1, service.StartCalls, "intento de inicio");
    checker.Eq(TimerState.Idle, vm.State, "State");
    checker.Null(vm.SessionStartedAt, "SessionStartedAt");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "RemainingTime");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "ElapsedTime");
    checker.Eq(0d, vm.ProgressPercentage, "ProgressPercentage");
    checker.Eq(0, vm.SessionPlannedMinutes, "SessionPlannedMinutes");
    checker.Eq(
        "No se pudo iniciar el temporizador. Inténtalo de nuevo.",
        vm.ErrorMessage,
        "ErrorMessage");
    checker.True(vm.CanStart, "se puede reintentar");
});

checker.Scenario("R1a. Completion encolado + Pause se reconcilia como Completed", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;

    RunOffUiThread(service.CompleteFromBackground);
    checker.Eq(TimerState.Running, vm.State, "estado antes de drenar");
    checker.True(ui.PendingCount >= 2, "tick y completion encolados");

    vm.Pause();

    checker.Eq(TimerState.Completed, vm.State, "Pause reconcilia a Completed");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(25), vm.ElapsedTime, "Efectivo");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.NotNull(vm.SessionEndedAt, "SessionEndedAt");
    checker.True(!service.IsRunning && !service.IsPaused, "servicio detenido");

    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "estado tras drenar");
    checker.Eq(1, endedChanges, "una única notificación de fin");
    checker.Eq(0, ui.PendingCount, "cola drenada");
});

checker.Scenario("R1b. Completion encolado + Stop se reconcilia como Completed", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;

    RunOffUiThread(service.CompleteFromBackground);
    vm.Stop();

    checker.Eq(TimerState.Completed, vm.State, "Stop no deja Stopped");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(25), vm.ElapsedTime, "Efectivo");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.NotNull(vm.SessionEndedAt, "SessionEndedAt");

    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "estado tras drenar");
    checker.Eq(1, endedChanges, "una única notificación de fin");
});

checker.Scenario("R1c. Pause normal descarta un tick obsoleto", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    RunOffUiThread(() =>
    {
        service.SetProgress(TimeSpan.FromMinutes(8));
        service.RaiseTick(TimeSpan.FromMinutes(17));
        service.SetProgress(TimeSpan.FromMinutes(10));
    });

    vm.Pause();
    ui.Drain();

    checker.Eq(TimerState.Paused, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "Restante de pausa");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo de pausa");
    checker.Eq(40d, vm.ProgressPercentage, "Progreso de pausa");
    checker.True(service.IsPaused, "servicio pausado");
    checker.Eq(0, ui.PendingCount, "tick obsoleto procesado sin regresión");
});

checker.Scenario("R1d. Stop parcial conserva el efectivo y descarta un tick obsoleto", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    RunOffUiThread(() =>
    {
        service.SetProgress(TimeSpan.FromMinutes(8));
        service.RaiseTick(TimeSpan.FromMinutes(17));
        service.SetProgress(TimeSpan.FromMinutes(10));
    });

    vm.Stop();
    ui.Drain();

    checker.Eq(TimerState.Stopped, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(15), vm.RemainingTime, "Restante efectivo");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo");
    checker.Eq(40d, vm.ProgressPercentage, "Progreso");
    checker.NotNull(vm.SessionEndedAt, "SessionEndedAt");
    checker.Eq(0, ui.PendingCount, "tick obsoleto procesado sin regresión");
});

checker.Scenario("R1e. Completion normal no duplica el fin de sesión", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;

    RunOffUiThread(service.CompleteFromBackground);
    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "State");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.Eq(1, endedChanges, "exactamente un SessionEndedAt");
    checker.Eq(0, ui.PendingCount, "cola drenada");
});

checker.Scenario("Carrera 1. Tick viejo tras Stop + Start no contamina B", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    RunOffUiThread(() => service.RaiseTick(TimeSpan.FromMinutes(20)));
    vm.Stop();
    vm.PlannedMinutesText = "10";
    vm.Start();
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "Restante B");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "Efectivo B");
    checker.Eq(0d, vm.ProgressPercentage, "Progreso B");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt B");
    checker.True(service.IsRunning, "servicio B en marcha");
});

checker.Scenario("Carrera 2. Completion viejo encolado no completa B", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    RunOffUiThread(service.CompleteFromBackground);
    vm.Stop();
    vm.PlannedMinutesText = "10";
    vm.Start();
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "Restante B");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "Efectivo B");
    checker.Eq(0d, vm.ProgressPercentage, "Progreso B");
    checker.Null(vm.SessionEndedAt, "SessionEndedAt B");
});

checker.Scenario("Carrera 3. Tick de B se aplica desde el servicio", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "10"
    };

    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(5));
    RunOffUiThread(() => service.RaiseTick(TimeSpan.FromMinutes(5)));
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "State");
    checker.Eq(TimeSpan.FromMinutes(5), vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(5), vm.ElapsedTime, "Efectivo");
    checker.Eq(50d, vm.ProgressPercentage, "Progreso");
});

checker.Scenario("R2a. Tick(0) + completion de A tras Stop + Start no alteran B", () =>
{
    var time = new FakeTimeProvider();
    var service = new TimerService(time);
    using var tickEntered = new ManualResetEventSlim(false);
    using var releaseTick = new ManualResetEventSlim(false);

    service.Tick += (_, remaining) =>
    {
        if (remaining == TimeSpan.Zero)
        {
            tickEntered.Set();
            releaseTick.Wait(TimeSpan.FromSeconds(5));
        }
    };

    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    var advance = Task.Run(() => time.Advance(TimeSpan.FromMinutes(26)));

    checker.True(tickEntered.Wait(TimeSpan.FromSeconds(5)), "llegó el Tick(0) de A");
    checker.True(!advance.IsCompleted, "Tick(0) y completion de A retenidos");

    vm.Stop();
    checker.Eq(TimerState.Completed, vm.State, "A completada por Stop");

    vm.PlannedMinutesText = "10";
    vm.Start();
    checker.Eq(TimerState.Running, vm.State, "B iniciada");
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "B antes de liberar A");

    releaseTick.Set();
    advance.GetAwaiter().GetResult();
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "B sigue Running");
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "B conserva 10:00");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "B conserva 00:00");
    checker.Eq(0d, vm.ProgressPercentage, "B conserva 0 %");
    checker.Null(vm.SessionEndedAt, "B sin SessionEndedAt");
    checker.Eq("En curso", vm.StatusText, "B sigue En curso");
    checker.Eq(0, ui.PendingCount, "cola drenada");
});

checker.Scenario("R2a2. Tick parcial viejo de A tras Start de B no altera B", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    vm.Stop();
    vm.PlannedMinutesText = "10";
    vm.Start();
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "B iniciada");

    RunOffUiThread(() => service.RaiseTick(TimeSpan.FromMinutes(5)));
    ui.Drain();

    checker.Eq(TimerState.Running, vm.State, "B sigue Running");
    checker.Eq(TimeSpan.FromMinutes(10), vm.RemainingTime, "B conserva 10:00");
    checker.Eq(TimeSpan.Zero, vm.ElapsedTime, "B conserva 00:00");
    checker.Eq(0d, vm.ProgressPercentage, "B conserva 0 %");
    checker.Null(vm.SessionEndedAt, "B sin SessionEndedAt");
});

checker.Scenario("R2b. Completion viejo tras Pause de B no altera B", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.CompleteFromBackground();
    ui.Drain();
    checker.Eq(TimerState.Completed, vm.State, "A completada");

    vm.PlannedMinutesText = "10";
    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(3));
    service.RaiseTick(TimeSpan.FromMinutes(7));
    vm.Pause();
    checker.Eq(TimerState.Paused, vm.State, "B pausada");

    RunOffUiThread(service.RaiseStaleCompletionOnly);
    ui.Drain();

    checker.Eq(TimerState.Paused, vm.State, "B sigue Paused");
    checker.Eq(TimeSpan.FromMinutes(7), vm.RemainingTime, "restante B intacto");
    checker.Null(vm.SessionEndedAt, "B sin SessionEndedAt");
});

checker.Scenario("R2c. Completion viejo tras Stop parcial de B no altera B", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "25"
    };

    vm.Start();
    service.CompleteFromBackground();
    ui.Drain();
    checker.Eq(TimerState.Completed, vm.State, "A completada");

    vm.PlannedMinutesText = "10";
    vm.Start();
    service.SetProgress(TimeSpan.FromMinutes(4));
    service.RaiseTick(TimeSpan.FromMinutes(6));
    vm.Stop();
    var endB = vm.SessionEndedAt;

    checker.Eq(TimerState.Stopped, vm.State, "B detenida");
    checker.Eq(TimeSpan.FromMinutes(6), vm.RemainingTime, "B con 06:00");
    checker.NotNull(endB, "B con SessionEndedAt");

    RunOffUiThread(service.RaiseStaleCompletionOnly);
    ui.Drain();

    checker.Eq(TimerState.Stopped, vm.State, "B sigue Stopped");
    checker.Eq(TimeSpan.FromMinutes(6), vm.RemainingTime, "restante B intacto");
    checker.Eq(TimeSpan.FromMinutes(4), vm.ElapsedTime, "efectivo B intacto");
    checker.Eq(endB, vm.SessionEndedAt, "SessionEndedAt B intacto");
});

checker.Scenario("R2d. Completion legítimo vigente completa una sola vez", () =>
{
    var service = new FakeTimerService();
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "10"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;
    RunOffUiThread(service.CompleteFromBackground);
    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "State");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.Eq(1, endedChanges, "un SessionEndedAt");
});

checker.Scenario("R2e. Completion legítimo al llegar a cero sin acción del usuario", () =>
{
    var time = new FakeTimeProvider();
    var service = new TimerService(time);
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "10"
    };

    var endedChanges = 0;
    vm.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TimerViewModel.SessionEndedAt))
        {
            endedChanges++;
        }
    };

    vm.Start();
    endedChanges = 0;
    RunOffUiThread(() => time.Advance(TimeSpan.FromMinutes(11)));
    ui.Drain();

    checker.Eq(TimerState.Completed, vm.State, "State");
    checker.Eq(TimeSpan.Zero, vm.RemainingTime, "Restante");
    checker.Eq(TimeSpan.FromMinutes(10), vm.ElapsedTime, "Efectivo");
    checker.Eq(100d, vm.ProgressPercentage, "Progreso");
    checker.Eq(1, endedChanges, "un SessionEndedAt");
});

checker.Scenario("Tick real monótono, pausa congela y resume continúa", () =>
{
    var time = new FakeTimeProvider();
    var service = new TimerService(time);
    var repo = new FakeTopicRepository(ActiveTopic(1, "C#"));
    using var vm = new TimerViewModel(service, repo)
    {
        SelectedTopic = repo.Topics[0],
        PlannedMinutesText = "10"
    };

    vm.Start();
    CheckTick(vm, 10, 0, 0, "inicio");
    RunOffUiThread(() => time.Advance(TimeSpan.FromMinutes(1)));
    ui.Drain();
    CheckTick(vm, 9, 1, 10, "tras 1 min");
    RunOffUiThread(() => time.Advance(TimeSpan.FromMinutes(2)));
    ui.Drain();
    CheckTick(vm, 7, 3, 30, "tras 3 min");

    vm.Pause();
    checker.Eq(TimerState.Paused, vm.State, "Paused");
    CheckTick(vm, 7, 3, 30, "pausa");
    RunOffUiThread(() => time.Advance(TimeSpan.FromMinutes(5)));
    ui.Drain();
    CheckTick(vm, 7, 3, 30, "pausa tras avanzar tiempo");

    vm.Resume();
    checker.Eq(TimerState.Running, vm.State, "Running tras resume");
    RunOffUiThread(() => time.Advance(TimeSpan.FromMinutes(1)));
    ui.Drain();
    CheckTick(vm, 6, 4, 40, "tras reanudar 1 min");
});

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine($"Escenarios: {checker.Passed + checker.Failures} | Pasados: {checker.Passed} | Fallidos: {checker.Failures}");
Console.WriteLine($"Aserciones: {checker.Assertions}");
Console.WriteLine("========================================");

return checker.Failures == 0 ? 0 : 1;

static Topic ActiveTopic(int id, string title) => new()
{
    Id = id,
    Title = title,
    Status = Topic.StatusActive
};

static Topic InactiveTopic(int id, string title) => new()
{
    Id = id,
    Title = title,
    Status = Topic.StatusArchived
};

static void CheckTick(TimerViewModel vm, int remainingMinutes, int elapsedMinutes, int progress, string context)
{
    if (vm.RemainingTime != TimeSpan.FromMinutes(remainingMinutes)
        || vm.ElapsedTime != TimeSpan.FromMinutes(elapsedMinutes)
        || Math.Abs(vm.ProgressPercentage - progress) > 0.5)
    {
        throw new InvalidOperationException(
            $"{context}: esperado restante {remainingMinutes}:00, efectivo {elapsedMinutes}:00, " +
            $"progreso {progress} %; obtenido restante {vm.RemainingTime}, efectivo {vm.ElapsedTime}, " +
            $"progreso {vm.ProgressPercentage:0.##} %");
    }
}

static void RunOffUiThread(Action action)
{
    Exception? error = null;
    var thread = new Thread(() =>
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            error = ex;
        }
    })
    {
        IsBackground = true
    };

    thread.Start();
    thread.Join();

    if (error is not null)
    {
        throw new InvalidOperationException("Fallo en el hilo de fondo.", error);
    }
}

static void PumpUntilComplete(Task task, QueueSynchronizationContext ui)
{
    var stopwatch = Stopwatch.StartNew();

    while (!task.IsCompleted)
    {
        ui.Drain();
        Thread.Sleep(1);

        if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
        {
            throw new TimeoutException("La tarea no terminó en 5 segundos.");
        }
    }

    ui.Drain();
    task.GetAwaiter().GetResult();
}

static string ExtractMethod(string source, string signature)
{
    var start = source.IndexOf(signature, StringComparison.Ordinal);
    if (start < 0)
    {
        throw new InvalidOperationException($"No se encontró la firma: {signature}");
    }

    var brace = source.IndexOf('{', start);
    if (brace < 0)
    {
        throw new InvalidOperationException($"No se encontró el cuerpo de: {signature}");
    }

    var depth = 0;
    for (var i = brace; i < source.Length; i++)
    {
        if (source[i] == '{')
        {
            depth++;
        }
        else if (source[i] == '}')
        {
            depth--;
            if (depth == 0)
            {
                return source.Substring(start, i - start + 1);
            }
        }
    }

    throw new InvalidOperationException($"Cuerpo incompleto de: {signature}");
}

internal sealed class QueueSynchronizationContext : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();

    public int PendingCount => _queue.Count;

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }

    public void Drain()
    {
        while (_queue.TryDequeue(out var item))
        {
            item.Callback(item.State);
        }
    }
}

internal sealed class Checker
{
    public int Assertions { get; private set; }

    public int Passed { get; private set; }

    public int Failures { get; private set; }

    public void Scenario(string name, Action action)
    {
        var before = Assertions;

        try
        {
            action();
            Passed++;
            Console.WriteLine($"[PASS] {name} ({Assertions - before} aserciones)");
        }
        catch (Exception ex)
        {
            Failures++;
            Console.WriteLine($"[FAIL] {name}: {ex.Message}");
        }
    }

    public void Eq<T>(T expected, T actual, string context)
    {
        Assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Fail($"{context}: esperado <{expected}>, obtenido <{actual}>");
        }
    }

    public void True(bool condition, string context)
    {
        Assertions++;
        if (!condition)
        {
            Fail($"{context}: se esperaba true");
        }
    }

    public void Null(object? value, string context)
    {
        Assertions++;
        if (value is not null)
        {
            Fail($"{context}: se esperaba null, obtenido <{value}>");
        }
    }

    public void NotNull(object? value, string context)
    {
        Assertions++;
        if (value is null)
        {
            Fail($"{context}: se esperaba un valor no nulo");
        }
    }

    public void Contains(string value, string expected, string context)
    {
        Assertions++;
        if (!value.Contains(expected, StringComparison.Ordinal))
        {
            Fail($"{context}: no contiene <{expected}>. Valor: <{value}>");
        }
    }

    public void Throws<T>(Action action, string context)
        where T : Exception
    {
        Assertions++;

        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        catch (Exception ex)
        {
            Fail($"{context}: se esperaba {typeof(T).Name}, se obtuvo {ex.GetType().Name}");
            return;
        }

        Fail($"{context}: se esperaba {typeof(T).Name} y no se lanzó ninguna excepción");
    }

    private static void Fail(string message)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class FakeTimerService : ITimerService
{
    private EventHandler<TimeSpan>? _tick;
    private EventHandler? _sessionCompleted;

    public TimeSpan RemainingTime { get; private set; }

    public TimeSpan ElapsedTime { get; private set; }

    public bool IsRunning { get; private set; }

    public bool IsPaused { get; private set; }

    public TimeSpan Duration { get; private set; }

    public TimeSpan StartedDuration { get; private set; }

    public int StartCalls { get; private set; }

    public int PauseCalls { get; private set; }

    public int ResumeCalls { get; private set; }

    public int StopCalls { get; private set; }

    public bool ThrowOnStart { get; set; }

    public int TickSubscribers => _tick?.GetInvocationList().Length ?? 0;

    public int CompletionSubscribers => _sessionCompleted?.GetInvocationList().Length ?? 0;

    public event EventHandler<TimeSpan> Tick
    {
        add => _tick += value;
        remove => _tick -= value;
    }

    public event EventHandler SessionCompleted
    {
        add => _sessionCompleted += value;
        remove => _sessionCompleted -= value;
    }

    public void Start(TimeSpan duration)
    {
        StartCalls++;
        StartedDuration = duration;

        if (ThrowOnStart)
        {
            throw new InvalidOperationException("Fallo simulado de inicio.");
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        Duration = duration;
        RemainingTime = duration;
        ElapsedTime = TimeSpan.Zero;
        IsRunning = true;
        IsPaused = false;
        RaiseTick(duration);
    }

    public void Pause()
    {
        PauseCalls++;

        if (!IsRunning || IsPaused)
        {
            return;
        }

        if (ElapsedTime >= Duration)
        {
            IsRunning = false;
            IsPaused = false;
            RemainingTime = TimeSpan.Zero;
            RaiseTick(TimeSpan.Zero);
            _sessionCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsPaused = true;
        RaiseTick(RemainingTime);
    }

    public void Resume()
    {
        ResumeCalls++;

        if (!IsRunning || !IsPaused)
        {
            return;
        }

        IsPaused = false;
        RaiseTick(RemainingTime);
    }

    public void Stop()
    {
        StopCalls++;

        if (!IsRunning && !IsPaused)
        {
            return;
        }

        IsRunning = false;
        IsPaused = false;
        RaiseTick(RemainingTime);
    }

    public void SetProgress(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed > Duration)
        {
            elapsed = Duration;
        }

        ElapsedTime = elapsed;
        RemainingTime = Duration - elapsed;
    }

    public void RaiseTick(TimeSpan remaining)
    {
        _tick?.Invoke(this, remaining);
    }

    public void RaiseStaleCompletionOnly()
    {
        _sessionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void CompleteFromBackground()
    {
        ElapsedTime = Duration;
        RemainingTime = TimeSpan.Zero;
        IsRunning = false;
        IsPaused = false;

        RaiseTick(TimeSpan.Zero);
        _sessionCompleted?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeTopicRepository : ITopicRepository
{
    public FakeTopicRepository(params Topic[] topics)
    {
        Topics = topics.Select(Clone).ToList();
    }

    public List<Topic> Topics { get; }

    public bool ThrowOnGetAll { get; set; }

    public Task<IEnumerable<Topic>> GetAllAsync()
    {
        if (ThrowOnGetAll)
        {
            return Task.FromException<IEnumerable<Topic>>(
                new InvalidOperationException("Fallo simulado de carga."));
        }

        return Task.FromResult<IEnumerable<Topic>>(
            Topics.Select(Clone).ToList());
    }

    public Task<Topic?> GetByIdAsync(int id)
    {
        return Task.FromResult(
            Topics.Where(topic => topic.Id == id)
                .Select(Clone)
                .FirstOrDefault());
    }

    public Task<int> CreateAsync(Topic topic)
    {
        throw new NotSupportedException();
    }

    public Task<bool> UpdateAsync(Topic topic)
    {
        throw new NotSupportedException();
    }

    public Task<bool> SetStatusAsync(int id, string status)
    {
        throw new NotSupportedException();
    }

    public Task<IEnumerable<TopicSummaryDto>> GetSummariesAsync()
    {
        return Task.FromResult<IEnumerable<TopicSummaryDto>>([]);
    }

    private static Topic Clone(Topic topic) => new()
    {
        Id = topic.Id,
        Title = topic.Title,
        Description = topic.Description,
        ColorHex = topic.ColorHex,
        TargetHours = topic.TargetHours,
        Status = topic.Status,
        CompletedAt = topic.CompletedAt,
        CreatedAt = topic.CreatedAt
    };
}
