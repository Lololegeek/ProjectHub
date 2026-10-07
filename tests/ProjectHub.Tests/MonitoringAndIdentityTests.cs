using ProjectHub.Core.Services.Projects;
using Xunit;
namespace ProjectHub.Tests;

public sealed class MonitoringAndIdentityTests
{
    [Fact]
    public async Task MovedProjectKeepsItsIdentityNotesAndFavorite()
    {
        using var w = new TestWorkspace();
        w.File("source/old/App/Cargo.toml", "[package]\nname='real-application'\nversion='1.0.0'\n");
        using var engine = new HubEngine(Path.Combine(w.Root, "db"));
        await engine.InitializeAsync();
        engine.Settings.WatchEnabled = false;
        engine.Settings.Roots = [Path.Combine(w.Root, "source")];
        await engine.ScanAsync(null, default);
        var old = Assert.Single(engine.Projects);
        old.Notes = "Retain";
        old.Favorite = true;
        await engine.SaveProjectAsync(old);
        Directory.CreateDirectory(Path.Combine(w.Root, "source/new"));
        Directory.Move(old.Path, Path.Combine(w.Root, "source/new/App"));
        await engine.ScanAsync(null, default);
        var moved = Assert.Single(engine.Projects);
        Assert.Equal(old.Id, moved.Id);
        Assert.Equal("Retain", moved.Notes);
        Assert.True(moved.Favorite);
        Assert.EndsWith(Path.Combine("new", "App"), moved.Path);
        Assert.Single(await engine.Repository.LoadProjectsAsync());
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
    [Fact]
    public async Task WatcherDiscoversNewProjectAndUpdatesManifestStack()
    {
        using var w = new TestWorkspace();
        var root = Path.Combine(w.Root, "source");
        Directory.CreateDirectory(root);
        using var engine = new HubEngine(Path.Combine(w.Root, "db"));
        await engine.InitializeAsync();
        engine.Settings.Roots = [root];
        await engine.SaveSettingsAsync();
        w.File("source/App/package.json", "{\"name\":\"app\",\"dependencies\":{\"react\":\"19\"}}");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (engine.Projects.Count == 0 || !engine.Projects[0].Technologies.Contains("React"))
            await Task.Delay(100, timeout.Token);
        w.File("source/App/package.json", "{\"name\":\"app\",\"dependencies\":{\"vue\":\"3\"}}");
        while (!engine.Projects[0].Technologies.Contains("Vue"))
            await Task.Delay(100, timeout.Token);
        Assert.DoesNotContain("React", engine.Projects[0].Technologies);
    }
}
