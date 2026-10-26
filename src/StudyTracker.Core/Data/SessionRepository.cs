using Dapper;
using StudyTracker.Core.Interfaces;
using StudyTracker.Core.Models;

namespace StudyTracker.Core.Data;

public sealed class SessionRepository : ISessionRepository
{
    private const string SessionColumns = """
        id AS Id,
        topic_id AS TopicId,
        started_at AS StartedAt,
        ended_at AS EndedAt,
        planned_minutes AS PlannedMinutes,
        actual_minutes AS ActualMinutes,
        session_type AS SessionType,
        notes AS Notes,
        created_at AS CreatedAt
        """;

    private readonly DatabaseInitializer _database;

    public SessionRepository(DatabaseInitializer database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<int> RecordSessionAsync(StudySession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CreatedAt == default)
        {
            session.CreatedAt = DateTime.Now;
        }

        const string sql = """
            INSERT INTO study_sessions
                (topic_id, started_at, ended_at, planned_minutes, actual_minutes, session_type, notes, created_at)
            VALUES
                (@TopicId, @StartedAt, @EndedAt, @PlannedMinutes, @ActualMinutes, @SessionType, @Notes, @CreatedAt)
            RETURNING id;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        var id = await connection.ExecuteScalarAsync<long>(sql, session);

        session.Id = (int)id;
        return session.Id;
    }

    public async Task<IEnumerable<StudySession>> GetByTopicIdAsync(int topicId)
    {
        var sql = $"""
            SELECT {SessionColumns}
            FROM study_sessions
            WHERE topic_id = @TopicId
            ORDER BY started_at DESC, id DESC;
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        return await connection.QueryAsync<StudySession>(sql, new { TopicId = topicId });
    }

    public async Task<IEnumerable<DailyActivityDto>> GetDailyActivityAsync(DateTime fromDate, DateTime toDate)
    {
        const string sql = """
            SELECT date(started_at) AS Date,
                   SUM(actual_minutes) AS TotalMinutes
            FROM study_sessions
            WHERE date(started_at) BETWEEN date(@FromDate) AND date(@ToDate)
            GROUP BY date(started_at)
            ORDER BY date(started_at);
            """;

        await using var connection = _database.CreateConnection();
        await connection.OpenAsync();
        var activity = (await connection.QueryAsync<DailyActivityDto>(
            sql,
            new { FromDate = fromDate, ToDate = toDate })).ToList();

        foreach (var day in activity)
        {
            day.IntensityLevel = CalculateIntensity(day.TotalMinutes);
        }

        return activity;
    }

    private static int CalculateIntensity(int totalMinutes) => totalMinutes switch
    {
        <= 0 => 0,
        < 30 => 1,
        < 60 => 2,
        < 120 => 3,
        _ => 4
    };
}
