# Service Contracts & Interfaces

## 1. ITopicRepository
```csharp
public interface ITopicRepository
{
    Task<IEnumerable<Topic>> GetAllAsync();
    Task<Topic?> GetByIdAsync(int id);
    Task<int> CreateAsync(Topic topic);
    Task<bool> UpdateAsync(Topic topic);
    Task<bool> SetStatusAsync(int id, string status);
    Task<IEnumerable<TopicSummaryDto>> GetSummariesAsync();
}
```

## 2. ISessionRepository
```csharp
public interface ISessionRepository
{
    Task<int> RecordSessionAsync(StudySession session);
    Task<IEnumerable<StudySession>> GetByTopicIdAsync(int topicId);
    Task<IEnumerable<DailyActivityDto>> GetDailyActivityAsync(DateTime fromDate, DateTime toDate);
}
```

## 3. ITimerService
```csharp
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
```

## 4. INotificationService
```csharp
public interface INotificationService
{
    void NotifySessionStarted(string topicTitle, TimeSpan duration);
    void NotifySessionCompleted(string topicTitle, int actualMinutes);
}
```
