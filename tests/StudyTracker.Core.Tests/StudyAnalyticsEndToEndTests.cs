using StudyTracker.Core.Models;
using StudyTracker.Core.Services;

namespace StudyTracker.Core.Tests;

public class StudyAnalyticsEndToEndTests
{
    [Fact]
    public async Task PersistedStudySessions_FlowIntoHeatmapStreakAndTopicGoalSummaries()
    {
        await using var database = await TestDatabase.CreateAsync();
        var date = new DateTime(2026, 10, 27);
        var algebra = new Topic
        {
            Title = "Álgebra",
            ColorHex = "#0078D4",
            TargetHours = 2
        };
        var history = new Topic
        {
            Title = "Historia",
            ColorHex = "#107C10",
            TargetHours = 0
        };
        await database.Topics.CreateAsync(algebra);
        await database.Topics.CreateAsync(history);

        await RecordAsync(database, algebra.Id, date.AddDays(-1).AddHours(9), 60);
        await RecordAsync(database, algebra.Id, date.AddHours(9), 30);
        await RecordAsync(database, history.Id, date.AddHours(11), 30);

        var statistics = await new StatsService(database.Sessions).GetStatisticsAsync(date);
        var topicSummaries = (await database.Topics.GetSummariesAsync())
            .ToDictionary(summary => summary.Topic.Id);

        Assert.Equal(2, statistics.CurrentStreak);
        Assert.Equal(60, statistics.DailyActivity[^1].TotalMinutes);
        Assert.Equal(3, statistics.DailyActivity[^1].IntensityLevel);
        Assert.Equal(90, topicSummaries[algebra.Id].TotalMinutesStudied);
        Assert.Equal(75, topicSummaries[algebra.Id].ProgressPercentage, 6);
        Assert.Equal(30, topicSummaries[history.Id].TotalMinutesStudied);
        Assert.Equal(0, topicSummaries[history.Id].ProgressPercentage);
    }

    private static Task<int> RecordAsync(
        TestDatabase database,
        int topicId,
        DateTime startedAt,
        int minutes) =>
        database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(minutes),
            PlannedMinutes = minutes,
            ActualMinutes = minutes
        });
}
