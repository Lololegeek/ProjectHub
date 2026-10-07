using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectHub.Core.Models;
namespace ProjectHub.Core.Services.Persistence;

public sealed class HubRepository
{
    public string DatabasePath
    {
        get;
    }
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, IgnoreReadOnlyProperties = true };
    public HubRepository(string path)
    {
        DatabasePath = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }
    private async Task<SqliteConnection> Open()
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString());
        await c.OpenAsync();
        return c;
    }
    public async Task InitializeAsync()
    {
        await using var c = await Open();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE IF NOT EXISTS Projects(Id TEXT PRIMARY KEY, Path TEXT NOT NULL UNIQUE COLLATE NOCASE, Json TEXT NOT NULL); CREATE TABLE IF NOT EXISTS UserSettings(Key TEXT PRIMARY KEY, Json TEXT NOT NULL); PRAGMA user_version=1;";
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<List<ProjectRecord>> LoadProjectsAsync()
    {
        await using var c = await Open();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Json FROM Projects";
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = new List<ProjectRecord>();
        while (await reader.ReadAsync())
        {
            var p = JsonSerializer.Deserialize<ProjectRecord>(reader.GetString(0));
            if (p != null)
                result.Add(p);
        }
        return result;
    }
    public async Task SaveProjectsAsync(IEnumerable<ProjectRecord> projects)
    {
        await gate.WaitAsync();
        try
        {
            await using var c = await Open();
            await using var tx = c.BeginTransaction();
            foreach (var project in projects)
            {
                await using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO Projects(Id,Path,Json) VALUES($id,$path,$json) ON CONFLICT(Id) DO UPDATE SET Path=$path,Json=$json";
                string payload;
                lock (project)
                    payload = JsonSerializer.Serialize(project, Json);
                cmd.Parameters.AddWithValue("$id", project.Id);
                cmd.Parameters.AddWithValue("$path", project.Path);
                cmd.Parameters.AddWithValue("$json", payload);
                await cmd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        finally { gate.Release(); }
    }
    public async Task<HubSettings> LoadSettingsAsync()
    {
        await using var c = await Open();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Json FROM UserSettings WHERE Key='settings'";
        var value = await cmd.ExecuteScalarAsync() as string;
        return value == null ? new() : JsonSerializer.Deserialize<HubSettings>(value) ?? new();
    }
    public async Task SaveSettingsAsync(HubSettings settings)
    {
        await gate.WaitAsync();
        try
        {
            await using var c = await Open();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO UserSettings(Key,Json) VALUES('settings',$json) ON CONFLICT(Key) DO UPDATE SET Json=$json";
            cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings));
            await cmd.ExecuteNonQueryAsync();
        }
        finally { gate.Release(); }
    }
    public async Task ReplaceMovedAsync(ProjectRecord current, string previousId)
    {
        await gate.WaitAsync();
        var newId = current.Id;
        try
        {
            current.Id = previousId;
            string payload;
            lock (current)
                payload = JsonSerializer.Serialize(current, Json);
            await using var c = await Open();
            await using var tx = c.BeginTransaction();
            await using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Projects WHERE Id=$new; INSERT INTO Projects(Id,Path,Json) VALUES($old,$path,$json) ON CONFLICT(Id) DO UPDATE SET Path=$path,Json=$json";
            cmd.Parameters.AddWithValue("$new", newId);
            cmd.Parameters.AddWithValue("$old", previousId);
            cmd.Parameters.AddWithValue("$path", current.Path);
            cmd.Parameters.AddWithValue("$json", payload);
            await cmd.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch { current.Id = newId; throw; }
        finally { gate.Release(); }
    }
}
