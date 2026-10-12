using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using TtcImageJudge.Core;

namespace TtcImageJudge.Api;

public sealed class JudgeDatabase(IConfiguration configuration, IWebHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public string DatabasePath { get; } = Path.GetFullPath(configuration["Database:Path"] ??
        Path.Combine(environment.ContentRootPath, "data", "judge.db"));
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString();

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS exercises (
              id TEXT PRIMARY KEY, slug TEXT NOT NULL UNIQUE, title TEXT NOT NULL, difficulty TEXT NOT NULL,
              version INTEGER NOT NULL, reference_version INTEGER NOT NULL, rules_version INTEGER NOT NULL,
              payload_json TEXT NOT NULL, updated_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS submissions (
              id TEXT PRIMARY KEY, exercise_id TEXT NOT NULL, submitted_at TEXT NOT NULL, verdict TEXT NOT NULL,
              exercise_version INTEGER NOT NULL, reference_version INTEGER NOT NULL, rules_version INTEGER NOT NULL,
              payload_json TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_submissions_exercise_date ON submissions(exercise_id, submitted_at DESC);
            CREATE TABLE IF NOT EXISTS expected_artifacts (
              test_id TEXT NOT NULL, reference_version INTEGER NOT NULL, rules_version INTEGER NOT NULL,
              png BLOB NOT NULL, sha256 TEXT NOT NULL,
              PRIMARY KEY(test_id, reference_version, rules_version));
            """;
        await command.ExecuteNonQueryAsync();
        if ((long)(await ScalarAsync(connection, "SELECT COUNT(*) FROM exercises") ?? 0L) == 0)
            await UpsertExerciseAsync(SeedData.InitialExercise, false, connection);
    }

    public async Task<IReadOnlyList<ExerciseSummary>> ListExercisesAsync()
    {
        var list = new List<ExerciseSummary>();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, slug, title, version, difficulty FROM exercises ORDER BY title";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(new ExerciseSummary(Guid.Parse(reader.GetString(0)),
            reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4)));
        return list;
    }

    public async Task<ExerciseDetails?> GetExerciseAsync(Guid id)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT payload_json FROM exercises WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return await command.ExecuteScalarAsync() is string json
            ? JsonSerializer.Deserialize<ExerciseDetails>(json, JsonOptions) : null;
    }

    public Task UpsertExerciseAsync(ExerciseDetails exercise, bool invalidateGeneratedAnswers) =>
        UpsertExerciseAsync(exercise, invalidateGeneratedAnswers, null);

    private async Task UpsertExerciseAsync(ExerciseDetails exercise, bool invalidateGeneratedAnswers, SqliteConnection? existing)
    {
        var ownsConnection = existing is null;
        var connection = existing ?? new SqliteConnection(ConnectionString);
        if (ownsConnection) await connection.OpenAsync();
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO exercises(id,slug,title,difficulty,version,reference_version,rules_version,payload_json,updated_at)
                VALUES($id,$slug,$title,$difficulty,$version,$reference,$rules,$json,$now)
                ON CONFLICT(id) DO UPDATE SET slug=$slug,title=$title,difficulty=$difficulty,version=$version,
                reference_version=$reference,rules_version=$rules,payload_json=$json,updated_at=$now
                """;
            command.Parameters.AddWithValue("$id", exercise.Id.ToString());
            command.Parameters.AddWithValue("$slug", exercise.Slug);
            command.Parameters.AddWithValue("$title", exercise.Title);
            command.Parameters.AddWithValue("$difficulty", exercise.Difficulty);
            command.Parameters.AddWithValue("$version", exercise.Version);
            command.Parameters.AddWithValue("$reference", exercise.ReferenceVersion);
            command.Parameters.AddWithValue("$rules", exercise.RulesVersion);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(exercise, JsonOptions));
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync();
            if (invalidateGeneratedAnswers)
            {
                foreach (var test in exercise.Tests)
                {
                    var invalidate = connection.CreateCommand();
                    invalidate.CommandText = "DELETE FROM expected_artifacts WHERE test_id=$id AND (reference_version<>$r OR rules_version<>$v)";
                    invalidate.Parameters.AddWithValue("$id", test.Id.ToString());
                    invalidate.Parameters.AddWithValue("$r", exercise.ReferenceVersion);
                    invalidate.Parameters.AddWithValue("$v", exercise.RulesVersion);
                    await invalidate.ExecuteNonQueryAsync();
                }
            }
        }
        finally { if (ownsConnection) await connection.DisposeAsync(); }
    }

    public async Task SaveSubmissionAsync(SubmissionView submission)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO submissions VALUES($id,$exercise,$date,$verdict,$ev,$rv,$uv,$json)";
        command.Parameters.AddWithValue("$id", submission.Id.ToString());
        command.Parameters.AddWithValue("$exercise", submission.ExerciseId.ToString());
        command.Parameters.AddWithValue("$date", submission.SubmittedAt.ToString("O"));
        command.Parameters.AddWithValue("$verdict", submission.Verdict.ToString());
        command.Parameters.AddWithValue("$ev", submission.ExerciseVersion);
        command.Parameters.AddWithValue("$rv", submission.ReferenceVersion);
        command.Parameters.AddWithValue("$uv", submission.RulesVersion);
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(submission, JsonOptions));
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveExpectedArtifactsAsync(ExerciseDetails exercise, IReadOnlyList<TestResult> results)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        foreach (var result in results.Where(item => !string.IsNullOrWhiteSpace(item.ExpectedImageBase64)))
        {
            var test = exercise.Tests.FirstOrDefault(item => item.Name == result.TestName);
            if (test is null) continue;
            var png = Convert.FromBase64String(result.ExpectedImageBase64!);
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT OR REPLACE INTO expected_artifacts(test_id,reference_version,rules_version,png,sha256)
                VALUES($id,$reference,$rules,$png,$sha)
                """;
            command.Parameters.AddWithValue("$id", test.Id.ToString());
            command.Parameters.AddWithValue("$reference", exercise.ReferenceVersion);
            command.Parameters.AddWithValue("$rules", exercise.RulesVersion);
            command.Parameters.AddWithValue("$png", png);
            command.Parameters.AddWithValue("$sha", Convert.ToHexString(SHA256.HashData(png)));
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<SubmissionView?> GetSubmissionAsync(Guid id)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT payload_json FROM submissions WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return await command.ExecuteScalarAsync() is string json
            ? JsonSerializer.Deserialize<SubmissionView>(json, JsonOptions) : null;
    }

    public async Task<IReadOnlyList<SubmissionView>> ListSubmissionsAsync(Guid? exerciseId)
    {
        var list = new List<SubmissionView>();
        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = exerciseId is null
            ? "SELECT payload_json FROM submissions ORDER BY submitted_at DESC LIMIT 100"
            : "SELECT payload_json FROM submissions WHERE exercise_id=$id ORDER BY submitted_at DESC LIMIT 100";
        if (exerciseId is not null) command.Parameters.AddWithValue("$id", exerciseId.Value.ToString());
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(JsonSerializer.Deserialize<SubmissionView>(reader.GetString(0), JsonOptions)!);
        return list;
    }

    private static async Task<object?> ScalarAsync(SqliteConnection connection, string sql)
    { var command = connection.CreateCommand(); command.CommandText = sql; return await command.ExecuteScalarAsync(); }
}
