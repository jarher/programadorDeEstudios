namespace StudyTracker.Core.Models;

public class Topic
{
    public const string StatusActive = "ACTIVE";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusArchived = "ARCHIVED";

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ColorHex { get; set; } = "#0078D4";

    public double TargetHours { get; set; }

    public string Status { get; set; } = StatusActive;

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
