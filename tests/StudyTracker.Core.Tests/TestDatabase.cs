using Microsoft.Data.Sqlite;
using StudyTracker.Core.Data;

namespace StudyTracker.Core.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private TestDatabase(string databasePath)
    {
        DatabasePath = databasePath;
        Initializer = new DatabaseInitializer(databasePath);
        Topics = new TopicRepository(Initializer);
        Sessions = new SessionRepository(Initializer);
    }

    public string DatabasePath { get; }

    public DatabaseInitializer Initializer { get; }

    public TopicRepository Topics { get; }

    public SessionRepository Sessions { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "StudyTracker.Tests",
            Guid.NewGuid().ToString("N"));

        var database = new TestDatabase(Path.Combine(directory, "study_tracker.db"));
        await database.Initializer.InitializeAsync();
        return database;
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
