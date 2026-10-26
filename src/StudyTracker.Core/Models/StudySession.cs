namespace StudyTracker.Core.Models;

public class StudySession
{
    public const string TypeTimed = "TIMED";
    public const string TypePomodoro = "POMODORO";

    public int Id { get; set; }

    public int TopicId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime EndedAt { get; set; }

    public int PlannedMinutes { get; set; }

    public int ActualMinutes { get; set; }

    public string SessionType { get; set; } = TypeTimed;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
