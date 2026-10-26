using Microsoft.Toolkit.Uwp.Notifications;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Notifications;

namespace StudyTracker.Core.Services;

public sealed class WindowsNotificationService : INotificationService, IDisposable
{
    private bool _disposed;

    public WindowsNotificationService()
    {
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
    }

    public event EventHandler<NotificationActionEventArgs>? ActionInvoked;

    public void NotifySessionStarted(string topicTitle, TimeSpan duration)
    {
        new ToastContentBuilder()
            .AddText("Sesión de estudio iniciada")
            .AddText($"{topicTitle} · {FormatDuration(duration)}")
            .AddArgument("action", "started")
            .AddAudio(new ToastAudio { Silent = true })
            .Show();
    }

    public void NotifySessionCompleted(string topicTitle, int actualMinutes)
    {
        new ToastContentBuilder()
            .AddText("Sesión completada")
            .AddText($"{topicTitle} · {actualMinutes} min")
            .AddArgument("action", "completed")
            .AddButton("Descanso 5m", ToastActivationType.Foreground, "action=break")
            .AddButton("Extender 15m", ToastActivationType.Foreground, "action=extend")
            .AddButton("Finalizar", ToastActivationType.Foreground, "action=finish")
            .Show();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ToastNotificationManagerCompat.OnActivated -= OnToastActivated;
        _disposed = true;
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat args)
    {
        if (string.IsNullOrWhiteSpace(args.Argument))
        {
            return;
        }

        var arguments = ToastArguments.Parse(args.Argument);
        if (!arguments.TryGetValue("action", out var action))
        {
            return;
        }

        var parsed = action switch
        {
            "break" => NotificationAction.Break,
            "extend" => NotificationAction.Extend,
            "finish" => NotificationAction.Finish,
            _ => (NotificationAction?)null
        };

        if (parsed is { } value)
        {
            ActionInvoked?.Invoke(this, new NotificationActionEventArgs(value));
        }
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalMinutes >= 1
            ? $"{duration.TotalMinutes:0.#} min"
            : $"{duration.TotalSeconds:0} s";
}
