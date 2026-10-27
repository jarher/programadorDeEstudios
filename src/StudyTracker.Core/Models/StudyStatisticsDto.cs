namespace StudyTracker.Core.Models;

public sealed class StudyStatisticsDto
{
    public DateTime FromDate { get; init; }

    public DateTime ToDate { get; init; }

    public IReadOnlyList<DailyActivityDto> DailyActivity { get; init; } = [];

    public int CurrentStreak { get; init; }
}
