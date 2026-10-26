namespace StudyTracker.Core.Models;

public class DailyActivityDto
{
    public DateTime Date { get; set; }

    public int TotalMinutes { get; set; }

    public int IntensityLevel { get; set; }
}
