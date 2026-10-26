using Dapper;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;

namespace StudyTracker.Core.Data;

public sealed class TopicRepository : ITopicRepository
{
    private const string TopicColumns = """
        id AS Id,
        title AS Title,
        description AS Description,
        color_hex AS ColorHex,
        target_hours AS TargetHours,
        status AS Status,
        completed_at AS CompletedAt,
        created_at AS CreatedAt
        """;

    private readonly DatabaseInitializer _database;

    public TopicRepository(DatabaseInitializer database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<IEnumerable<Topic>> GetAllAsync()
    {
        var sql = $"""
            SELECT {TopicColumns}
            FROM topics
            ORDER BY id;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<Topic>(sql);
    }

    public async Task<Topic?> GetByIdAsync(int id)
    {
        var sql = $"SELECT {TopicColumns} FROM topics WHERE id = @Id;";

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        return await connection.QuerySingleOrDefaultAsync<Topic>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        if (topic.CreatedAt == default)
        {
            topic.CreatedAt = DateTime.Now;
        }

        const string sql = """
            INSERT INTO topics (title, description, color_hex, target_hours, status, completed_at, created_at)
            VALUES (@Title, @Description, @ColorHex, @TargetHours, @Status, @CompletedAt, @CreatedAt)
            RETURNING id;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        var id = await connection.ExecuteScalarAsync<long>(sql, topic);

        topic.Id = (int)id;
        return topic.Id;
    }

    public async Task<bool> UpdateAsync(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        const string sql = """
            UPDATE topics
            SET title = @Title,
                description = @Description,
                color_hex = @ColorHex,
                target_hours = @TargetHours,
                status = @Status,
                completed_at = @CompletedAt
            WHERE id = @Id;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        var affected = await connection.ExecuteAsync(sql, topic);
        return affected > 0;
    }

    public async Task<bool> SetStatusAsync(int id, string status)
    {
        if (status is not (Topic.StatusActive or Topic.StatusCompleted or Topic.StatusArchived))
        {
            throw new ArgumentException("Invalid topic status.", nameof(status));
        }

        const string sql = """
            UPDATE topics
            SET status = @Status,
                completed_at = @CompletedAt
            WHERE id = @Id;
            """;

        var completedAt = status == Topic.StatusCompleted ? DateTime.Now : (DateTime?)null;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        var affected = await connection.ExecuteAsync(sql, new { Id = id, Status = status, CompletedAt = completedAt });
        return affected > 0;
    }

    public async Task<IEnumerable<TopicSummaryDto>> GetSummariesAsync()
    {
        const string sql = """
            SELECT t.id AS Id,
                   t.title AS Title,
                   t.description AS Description,
                   t.color_hex AS ColorHex,
                   t.target_hours AS TargetHours,
                   t.status AS Status,
                   t.completed_at AS CompletedAt,
                   t.created_at AS CreatedAt,
                   COALESCE(SUM(s.actual_minutes), 0) AS TotalMinutesStudied,
                   MAX(s.ended_at) AS LastStudiedAt
            FROM topics t
            LEFT JOIN study_sessions s ON s.topic_id = t.id
            GROUP BY t.id
            ORDER BY t.id;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<Topic, TopicSummaryAggregate, TopicSummaryDto>(
            sql,
            (topic, aggregate) => new TopicSummaryDto
            {
                Topic = topic,
                TotalMinutesStudied = aggregate.TotalMinutesStudied,
                LastStudiedAt = aggregate.LastStudiedAt
            },
            splitOn: "TotalMinutesStudied");
    }
}

internal sealed class TopicSummaryAggregate
{
    public int TotalMinutesStudied { get; set; }

    public DateTime? LastStudiedAt { get; set; }
}
