using Microsoft.Data.Sqlite;
using StudyTracker.Core.Models;

namespace StudyTracker.Core.Tests;

public class SessionRepositoryTests
{
    [Fact]
    public async Task RecordSessionAsync_RoundTripsAllFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Cálculo" });
        var createdAt = new DateTime(2026, 10, 26, 12, 0, 0);
        var session = new StudySession
        {
            TopicId = topicId,
            StartedAt = new DateTime(2026, 10, 26, 10, 0, 0),
            EndedAt = new DateTime(2026, 10, 26, 10, 45, 0),
            PlannedMinutes = 50,
            ActualMinutes = 45,
            SessionType = StudySession.TypePomodoro,
            Notes = "Derivadas",
            CreatedAt = createdAt
        };

        var id = await database.Sessions.RecordSessionAsync(session);

        Assert.True(id > 0);
        Assert.Equal(id, session.Id);

        var saved = Assert.Single(await database.Sessions.GetByTopicIdAsync(topicId));

        Assert.Equal(session.TopicId, saved.TopicId);
        Assert.Equal(session.StartedAt, saved.StartedAt);
        Assert.Equal(session.EndedAt, saved.EndedAt);
        Assert.Equal(session.PlannedMinutes, saved.PlannedMinutes);
        Assert.Equal(session.ActualMinutes, saved.ActualMinutes);
        Assert.Equal(session.SessionType, saved.SessionType);
        Assert.Equal(session.Notes, saved.Notes);
        Assert.Equal(createdAt, saved.CreatedAt);
    }

    [Fact]
    public async Task GetByTopicIdAsync_FiltersByTopicAndOrdersNewestFirst()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Historia" });
        var otherTopicId = await database.Topics.CreateAsync(new Topic { Title = "Física" });

        var earlyId = await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = new DateTime(2026, 10, 24, 9, 0, 0),
            EndedAt = new DateTime(2026, 10, 24, 9, 30, 0),
            PlannedMinutes = 30,
            ActualMinutes = 30
        });
        var lateId = await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = new DateTime(2026, 10, 25, 18, 0, 0),
            EndedAt = new DateTime(2026, 10, 25, 19, 0, 0),
            PlannedMinutes = 60,
            ActualMinutes = 55
        });
        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = otherTopicId,
            StartedAt = new DateTime(2026, 10, 26, 8, 0, 0),
            EndedAt = new DateTime(2026, 10, 26, 8, 20, 0),
            PlannedMinutes = 20,
            ActualMinutes = 20
        });

        var sessions = (await database.Sessions.GetByTopicIdAsync(topicId)).ToList();

        Assert.Equal(new[] { lateId, earlyId }, sessions.Select(session => session.Id));
        Assert.All(sessions, session => Assert.Equal(topicId, session.TopicId));
    }

    [Fact]
    public async Task GetDailyActivityAsync_GroupsByDayAndAssignsIntensity()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Hábitos" });

        await RecordAsync(database, topicId, new DateTime(2026, 10, 20, 9, 0, 0), 20);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 24, 9, 0, 0), 29);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 25, 9, 0, 0), 30);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 25, 18, 0, 0), 29);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 26, 9, 0, 0), 60);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 27, 9, 0, 0), 119);
        await RecordAsync(database, topicId, new DateTime(2026, 10, 28, 9, 0, 0), 120);

        var activity = (await database.Sessions.GetDailyActivityAsync(
            new DateTime(2026, 10, 24),
            new DateTime(2026, 10, 28))).ToList();

        Assert.Equal(5, activity.Count);
        Assert.Collection(
            activity,
            day => AssertDay(day, new DateTime(2026, 10, 24), 29, 1),
            day => AssertDay(day, new DateTime(2026, 10, 25), 59, 2),
            day => AssertDay(day, new DateTime(2026, 10, 26), 60, 3),
            day => AssertDay(day, new DateTime(2026, 10, 27), 119, 3),
            day => AssertDay(day, new DateTime(2026, 10, 28), 120, 4));
    }

    [Fact]
    public async Task RecordSessionAsync_WithUnknownTopic_ThrowsSqliteException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var session = new StudySession
        {
            TopicId = 999,
            StartedAt = new DateTime(2026, 10, 26, 10, 0, 0),
            EndedAt = new DateTime(2026, 10, 26, 10, 25, 0),
            PlannedMinutes = 25,
            ActualMinutes = 25
        };

        await Assert.ThrowsAsync<SqliteException>(
            () => database.Sessions.RecordSessionAsync(session));
    }

    [Fact]
    public async Task DeletingTopic_CascadesSessions()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Efímera" });
        await RecordAsync(database, topicId, new DateTime(2026, 10, 26, 9, 0, 0), 30);

        await using (var connection = database.Initializer.CreateConnection())
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM topics WHERE id = $id;";
            command.Parameters.AddWithValue("$id", topicId);
            await command.ExecuteNonQueryAsync();
        }

        Assert.Empty(await database.Sessions.GetByTopicIdAsync(topicId));
    }

    [Fact]
    public async Task EmptyDatabase_ReturnsEmptySessionResults()
    {
        await using var database = await TestDatabase.CreateAsync();

        Assert.Empty(await database.Sessions.GetByTopicIdAsync(1));
        Assert.Empty(await database.Sessions.GetDailyActivityAsync(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31)));
    }

    private static async Task RecordAsync(TestDatabase database, int topicId, DateTime startedAt, int minutes)
    {
        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = startedAt,
            EndedAt = startedAt.AddMinutes(minutes),
            PlannedMinutes = minutes,
            ActualMinutes = minutes
        });
    }

    private static void AssertDay(DailyActivityDto day, DateTime date, int totalMinutes, int intensityLevel)
    {
        Assert.Equal(date, day.Date);
        Assert.Equal(totalMinutes, day.TotalMinutes);
        Assert.Equal(intensityLevel, day.IntensityLevel);
    }
}
