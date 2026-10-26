using Microsoft.Data.Sqlite;

namespace StudyTracker.Core.Data;

public sealed class DatabaseInitializer
{
    private const string SchemaSql = """
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS topics (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            title TEXT NOT NULL,
            description TEXT,
            color_hex TEXT DEFAULT '#0078D4',
            target_hours REAL DEFAULT 0,
            status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK(status IN ('ACTIVE', 'COMPLETED', 'ARCHIVED')),
            completed_at TEXT,
            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS study_sessions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            topic_id INTEGER NOT NULL,
            started_at TEXT NOT NULL,
            ended_at TEXT NOT NULL,
            planned_minutes INTEGER NOT NULL,
            actual_minutes INTEGER NOT NULL,
            session_type TEXT DEFAULT 'TIMED',
            notes TEXT,
            created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY(topic_id) REFERENCES topics(id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS idx_sessions_topic ON study_sessions(topic_id);
        CREATE INDEX IF NOT EXISTS idx_sessions_started_at ON study_sessions(started_at);
        """;

    public DatabaseInitializer(string? databasePath = null)
    {
        DatabasePath = string.IsNullOrWhiteSpace(databasePath) ? DefaultDatabasePath : databasePath;
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();
    }

    public static string DefaultDatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StudyTracker",
        "study_tracker.db");

    public string DatabasePath { get; }

    public string ConnectionString { get; }

    public SqliteConnection CreateConnection() => new(ConnectionString);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        EnsureDatabaseDirectory();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureDatabaseDirectory()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(DatabasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
