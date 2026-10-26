namespace StudyTracker.Core.Models;

public class TopicSummaryDto
{
    public Topic Topic { get; set; } = new();

    public int TotalMinutesStudied { get; set; }

    public DateTime? LastStudiedAt { get; set; }

    public double ProgressPercentage =>
        Topic.TargetHours <= 0
            ? 0
            : TotalMinutesStudied / (Topic.TargetHours * 60) * 100;
}
