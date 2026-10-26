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
}
