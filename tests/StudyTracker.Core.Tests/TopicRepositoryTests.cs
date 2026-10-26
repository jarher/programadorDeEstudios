using StudyTracker.Core.Models;

namespace StudyTracker.Core.Tests;

public class TopicRepositoryTests
{
    [Fact]
    public async Task CreateAsync_RoundTripsAllFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var createdAt = new DateTime(2026, 10, 26, 9, 30, 0);
        var topic = new Topic
        {
            Title = "Álgebra lineal",
            Description = "Matrices y espacios vectoriales",
            ColorHex = "#FF0000",
            TargetHours = 12.5,
            Status = Topic.StatusActive,
            CreatedAt = createdAt
        };

        var id = await database.Topics.CreateAsync(topic);

        Assert.True(id > 0);
        Assert.Equal(id, topic.Id);

        var saved = await database.Topics.GetByIdAsync(id);

        Assert.NotNull(saved);
        Assert.Equal(topic.Title, saved.Title);
        Assert.Equal(topic.Description, saved.Description);
        Assert.Equal(topic.ColorHex, saved.ColorHex);
        Assert.Equal(topic.TargetHours, saved.TargetHours);
        Assert.Equal(topic.Status, saved.Status);
        Assert.Equal(createdAt, saved.CreatedAt);
        Assert.Null(saved.CompletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTopicsOrderedById()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstId = await database.Topics.CreateAsync(new Topic { Title = "Primera" });
        var secondId = await database.Topics.CreateAsync(new Topic { Title = "Segunda" });

        var topics = (await database.Topics.GetAllAsync()).ToList();

        Assert.Equal(new[] { firstId, secondId }, topics.Select(topic => topic.Id));
    }

    [Fact]
    public async Task UpdateAsync_ChangesEditableFields()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topic = new Topic { Title = "Original", TargetHours = 1 };
        await database.Topics.CreateAsync(topic);

        topic.Title = "Actualizada";
        topic.Description = "Nueva descripción";
        topic.ColorHex = "#00FF00";
        topic.TargetHours = 8;

        Assert.True(await database.Topics.UpdateAsync(topic));

        var saved = await database.Topics.GetByIdAsync(topic.Id);

        Assert.NotNull(saved);
        Assert.Equal("Actualizada", saved.Title);
        Assert.Equal("Nueva descripción", saved.Description);
        Assert.Equal("#00FF00", saved.ColorHex);
        Assert.Equal(8, saved.TargetHours);

        topic.Id = 999;
        Assert.False(await database.Topics.UpdateAsync(topic));
    }

    [Fact]
    public async Task SetStatusAsync_CompletesAndReactivatesTopic()
    {
        await using var database = await TestDatabase.CreateAsync();
        var id = await database.Topics.CreateAsync(new Topic { Title = "Estado" });

        Assert.True(await database.Topics.SetStatusAsync(id, Topic.StatusCompleted));

        var completed = await database.Topics.GetByIdAsync(id);
        Assert.NotNull(completed);
        Assert.Equal(Topic.StatusCompleted, completed.Status);
        Assert.NotNull(completed.CompletedAt);

        Assert.True(await database.Topics.SetStatusAsync(id, Topic.StatusActive));

        var active = await database.Topics.GetByIdAsync(id);
        Assert.NotNull(active);
        Assert.Equal(Topic.StatusActive, active.Status);
        Assert.Null(active.CompletedAt);

        await Assert.ThrowsAsync<ArgumentException>(
            () => database.Topics.SetStatusAsync(id, "INVALID"));
    }

    [Fact]
    public async Task CreateAsync_WithoutOptionalFields_RoundTripsDefaults()
    {
        await using var database = await TestDatabase.CreateAsync();

        var id = await database.Topics.CreateAsync(new Topic { Title = "Sin extras" });

        var saved = await database.Topics.GetByIdAsync(id);

        Assert.NotNull(saved);
        Assert.Null(saved.Description);
        Assert.Equal("#0078D4", saved.ColorHex);
        Assert.Equal(0, saved.TargetHours);
        Assert.Equal(Topic.StatusActive, saved.Status);
        Assert.Null(saved.CompletedAt);
        Assert.NotEqual(default, saved.CreatedAt);
    }

    [Fact]
    public async Task GetSummariesAsync_WithZeroTarget_ReportsZeroProgress()
    {
        await using var database = await TestDatabase.CreateAsync();
        var topicId = await database.Topics.CreateAsync(new Topic { Title = "Sin meta", TargetHours = 0 });

        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = topicId,
            StartedAt = new DateTime(2026, 10, 26, 9, 0, 0),
            EndedAt = new DateTime(2026, 10, 26, 9, 30, 0),
            PlannedMinutes = 30,
            ActualMinutes = 30
        });

        var summary = Assert.Single(await database.Topics.GetSummariesAsync());

        Assert.Equal(30, summary.TotalMinutesStudied);
        Assert.Equal(0, summary.ProgressPercentage);
    }

    [Fact]
    public async Task EmptyDatabase_ReturnsEmptyTopicResults()
    {
        await using var database = await TestDatabase.CreateAsync();

        Assert.Empty(await database.Topics.GetAllAsync());
        Assert.Empty(await database.Topics.GetSummariesAsync());
        Assert.Null(await database.Topics.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetSummariesAsync_AggregatesMinutesProgressAndLastSession()
    {
        await using var database = await TestDatabase.CreateAsync();
        var studiedId = await database.Topics.CreateAsync(new Topic { Title = "Estudiada", TargetHours = 2 });
        var emptyId = await database.Topics.CreateAsync(new Topic { Title = "Vacía", TargetHours = 0 });

        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = studiedId,
            StartedAt = new DateTime(2026, 10, 24, 10, 0, 0),
            EndedAt = new DateTime(2026, 10, 24, 10, 30, 0),
            PlannedMinutes = 30,
            ActualMinutes = 30
        });
        await database.Sessions.RecordSessionAsync(new StudySession
        {
            TopicId = studiedId,
            StartedAt = new DateTime(2026, 10, 25, 18, 0, 0),
            EndedAt = new DateTime(2026, 10, 25, 18, 45, 0),
            PlannedMinutes = 45,
            ActualMinutes = 45
        });

        var summaries = (await database.Topics.GetSummariesAsync()).ToDictionary(summary => summary.Topic.Id);

        var studied = summaries[studiedId];
        Assert.Equal(75, studied.TotalMinutesStudied);
        Assert.Equal(new DateTime(2026, 10, 25, 18, 45, 0), studied.LastStudiedAt);
        Assert.Equal(62.5, studied.ProgressPercentage, 6);

        var empty = summaries[emptyId];
        Assert.Equal(0, empty.TotalMinutesStudied);
        Assert.Null(empty.LastStudiedAt);
        Assert.Equal(0, empty.ProgressPercentage);
    }
}
