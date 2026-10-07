using ProjectHub.Core.Services.Projects;
using ProjectHub.Core.Services.Scanning;
using Xunit;
namespace ProjectHub.Tests;

public sealed class EngineTests
{
    [Fact]
    public async Task ScannerHandlesFiveHundredProjectsWithoutIndexingGeneratedManifests()
    {
        using var w = new TestWorkspace();
        for (int i = 0; i < 500; i++)
        {
            w.File($"P{i}/Cargo.toml", "[package]\nname='project'");
            w.File($"P{i}/target/fake/Cargo.toml", "[package]");
        }
        var result = await new ProjectScanner(new()).ScanAsync(new()
        {
            Roots = [w.Root]
        }, null, default);
        Assert.Equal(500, result.Projects.Count);
        Assert.Empty(result.Errors);
    }
    [Fact]
    public async Task MissingRootDoesNotDeleteCachedProjects()
    {
        using var w = new TestWorkspace();
        w.File("source/A/Cargo.toml", "[package]");
        using var engine = new HubEngine(Path.Combine(w.Root, "db"));
        await engine.InitializeAsync();
        engine.Settings.WatchEnabled = false;
        engine.Settings.Roots = [Path.Combine(w.Root, "source")];
        await engine.ScanAsync(null, default);
        var project = Assert.Single(engine.Projects);
        project.Notes = "keep";
        await engine.SaveProjectAsync(project);
        Directory.Move(Path.Combine(w.Root, "source"), Path.Combine(w.Root, "disconnected"));
        await engine.ScanAsync(null, default);
        var cached = Assert.Single(await engine.Repository.LoadProjectsAsync());
        Assert.Equal(project.Id, cached.Id);
        Assert.True(cached.Missing);
        Assert.Equal("keep", cached.Notes);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
}
