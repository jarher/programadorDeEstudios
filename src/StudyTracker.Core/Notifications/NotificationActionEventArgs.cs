namespace StudyTracker.Core.Notifications;

public sealed class NotificationActionEventArgs : EventArgs
{
    public NotificationActionEventArgs(NotificationAction action)
    {
        Action = action;
    }

    public NotificationAction Action { get; }
}
