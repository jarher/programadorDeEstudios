namespace StudyTracker.Core.Interfaces;

public interface INotificationService
{
    void NotifySessionStarted(string topicTitle, TimeSpan duration);

    void NotifySessionCompleted(string topicTitle, int actualMinutes);
}
