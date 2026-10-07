using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Persistence;
using Xunit;
namespace ProjectHub.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task CacheAndCustomizationSurviveRestart()
    {
        using var w = new TestWorkspace();
        var db = Path.Combine(w.Root, "data.db");
        var repo = new HubRepository(db);
        await repo.InitializeAsync();
        var p = new ProjectRecord { Path = Path.Combine(w.Root, "project"), Name = "A", Favorite = true, Notes = "Keep this", Tags = ["Personal"] };
        await repo.SaveProjectsAsync([p]);
        await repo.SaveSettingsAsync(new()
        {
            Roots = [w.Root]
        });
        var reopened = new HubRepository(db);
        var saved = Assert.Single(await reopened.LoadProjectsAsync());
        Assert.Equal(p.Id, saved.Id);
        Assert.True(saved.Favorite);
        Assert.Equal("Keep this", saved.Notes);
        Assert.Contains("Personal", saved.Tags);
        Assert.Single((await reopened.LoadSettingsAsync()).Roots);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
    [Fact]
    public async Task ExportIncludesPreferencesButNotCache()
    {
        using var w = new TestWorkspace();
        var path = Path.Combine(w.Root, "export.json");
        await ConfigurationTransfer.ExportAsync(path, new(), [new() { Path = w.Root, Notes = "private", Readme = "large cache" }]);
        var raw = await File.ReadAllTextAsync(path);
        Assert.Contains("private", raw);
        Assert.DoesNotContain("large cache", raw);
        Assert.Equal(1, (await ConfigurationTransfer.ReadAsync(path)).Version);
    }
    [Fact]
    public void ImportPreservesOfflineRootsAndMetadataUntilDetection()
    {
        var path = Path.Combine(Path.GetTempPath(), "not-yet-scanned");
        var s = new HubSettings();
        var data = new ProjectCustomization(path, true, false, 0, false, "Important note", ["tag"], [], "", []);
        ConfigurationTransfer.Apply(new(1, new()
        {
            Roots = [path]
        }, [data]), s, []);
        Assert.Contains(path, s.Roots);
        Assert.Single(s.PendingCustomizations);
        var project = new ProjectRecord { Path = path };
        s.PendingCustomizations[0].ApplyTo(project);
        Assert.True(project.Favorite);
        Assert.Equal("Important note", project.Notes);
    }
}
