using StudyTracker.Core.Models;
using StudyTracker.Core.Services;

namespace StudyTracker.Core.Tests;

public class StatsServiceTests
{
    [Fact]
    public async Task GetStatisticsAsync_ReturnsThreeMonthsIncludingInactiveDaysAndCurrentStreak()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Cálculo" });
        var asOfDate = new DateTime(2026, 10, 27);
        var fromDate = asOfDate.AddMonths(-3);
        await RecordAsync(database, topicId, fromDate, 45);
        await RecordAsync(database, topicId, asOfDate.AddDays(-2), 25);
        await RecordAsync(database, topicId, asOfDate.AddDays(-1), 35);
        await RecordAsync(database, topicId, asOfDate, 60);
        await RecordAsync(database, topicId, asOfDate.AddDays(1), 90);

        var statistics = await new StatsService(database.Sessions).GetStatisticsAsync(asOfDate);

        Assert.Equal(fromDate, statistics.FromDate);
        Assert.Equal(asOfDate, statistics.ToDate);
        Assert.Equal((asOfDate - fromDate).Days + 1, statistics.DailyActivity.Count);
        Assert.Equal(fromDate, statistics.DailyActivity[0].Date);
        Assert.Equal(asOfDate, statistics.DailyActivity[^1].Date);
        Assert.Equal(45, statistics.DailyActivity[0].TotalMinutes);
        Assert.Equal(0, statistics.DailyActivity[1].TotalMinutes);
        Assert.Equal(0, statistics.DailyActivity[1].IntensityLevel);
        Assert.Equal(3, statistics.CurrentStreak);
    }

    [Fact]
    public async Task GetStatisticsAsync_StreakContinuesFromYesterdayWhenTodayIsInactive()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Historia" });
        var asOfDate = new DateTime(2026, 10, 27);
        await RecordAsync(database, topicId, asOfDate.AddDays(-1), 20);
        await RecordAsync(database, topicId, asOfDate.AddDays(-2), 20);
        await RecordAsync(database, topicId, asOfDate.AddDays(-4), 20);
        await RecordAsync(database, topicId, asOfDate, 0);

        var statistics = await new StatsService(database.Sessions).GetStatisticsAsync(asOfDate);

        Assert.Equal(2, statistics.CurrentStreak);
        Assert.Equal(0, statistics.DailyActivity[^1].TotalMinutes);
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyHistoryReturnsZeroActivityAndStreak()
    {
        await using var database = await TestDatabase.CreateAsync();
        var statistics = await new StatsService(database.Sessions)
            .GetStatisticsAsync(new DateTime(2026, 10, 27));

        Assert.Equal((statistics.ToDate - statistics.FromDate).Days + 1, statistics.DailyActivity.Count);
        Assert.All(statistics.DailyActivity, day =>
        {
            Assert.Equal(0, day.TotalMinutes);
            Assert.Equal(0, day.IntensityLevel);
        });
        Assert.Equal(0, statistics.CurrentStreak);
    }

    [Fact]
    public async Task GetStatisticsAsync_CountsStreakLongerThanThreeMonths()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Idiomas" });
        var asOfDate = new DateTime(2026, 10, 27);
        for (var daysAgo = 100; daysAgo >= 0; daysAgo--)
        {
            await RecordAsync(database, topicId, asOfDate.AddDays(-daysAgo), 10);
        }

        var statistics = await new StatsService(database.Sessions).GetStatisticsAsync(asOfDate);

        Assert.Equal(101, statistics.CurrentStreak);
        Assert.Equal(10, statistics.DailyActivity[0].TotalMinutes);
    }

    private static Task<int> RecordAsync(TestDatabase database, int topicId, DateTime startedAt, int minutes) =>
        database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(minutes),
            PlannedMinutes = minutes,
            ActualMinutes = minutes
        });
}
