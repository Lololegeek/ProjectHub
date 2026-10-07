using ProjectHub.Core.Detection;
using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Scanning;
using Xunit;
namespace ProjectHub.Tests;

public sealed class DetectionTests
{
    [Fact]
    public async Task GroupsTauriAndSolutionComponentsAndIgnoresDependencies()
    {
        using var w = new TestWorkspace();
        w.File("Desktop/package.json", "{\"dependencies\":{\"react\":\"19\",\"@tauri-apps/api\":\"2\"}}");
        w.File("Desktop/src-tauri/Cargo.toml", "[package]\nname='backend'\n[dependencies]\ntauri='2'");
        w.File("Desktop/node_modules/fake/package.json", "{}");
        w.File("Desktop/tests/fixtures/fake/package.json", "{\"dependencies\":{\"vue\":\"3\"}}");
        w.File("Dotnet/App.slnx", "<Solution/>");
        w.File("Dotnet/src/App.csproj", "<Project/>");
        w.File("Other/project.godot", "[application]");
        var result = await new ProjectScanner(new()).ScanAsync(new()
        {
            Roots = [w.Root]
        }, null, default);
        Assert.Equal(3, result.Projects.Count);
        var desktop = Assert.Single(result.Projects, x => x.Name == "Desktop");
        Assert.Contains("Tauri", desktop.Technologies);
        Assert.Contains("Rust", desktop.Technologies);
        Assert.Single(desktop.Components);
        Assert.Single(result.Projects.Single(x => x.Name == "Dotnet").Components);
        Assert.DoesNotContain(result.Projects, x => x.Path.Contains("node_modules"));
        Assert.DoesNotContain("Vue", desktop.Technologies);
    }
    [Theory]
    [InlineData("Cargo.toml", "[workspace]", "Rust")]
    [InlineData("pyproject.toml", "[project]\ndependencies=['fastapi']", "FastAPI")]
    [InlineData("build.gradle", "plugins { id 'net.neoforged.moddev' }", "NeoForge")]
    [InlineData("default.project.json", "{}", "Roblox")]
    [InlineData("CMakeLists.txt", "project(foo)", "C++")]
    public void DetectsStacks(string file, string text, string stack)
    {
        using var w = new TestWorkspace();
        var path = w.File(file, text);
        var result = new DetectorRegistry().Detect(new(w.Root, [path]));
        Assert.NotNull(result);
        Assert.Contains(stack, result.Technologies);
    }
    [Fact]
    public void HandlesInvalidNodeManifestAsData()
    {
        using var w = new TestWorkspace();
        var p = w.File("package.json", "{broken");
        var result = new DetectorRegistry().Detect(new(w.Root, [p]));
        Assert.NotNull(result);
        Assert.Contains("Node.js", result.Technologies);
    }
    [Fact]
    public async Task CancellationPreservesCompletedDiscoveries()
    {
        using var w = new TestWorkspace();
        w.File("A/Cargo.toml", "[package]");
        using var ct = new CancellationTokenSource();
        ct.Cancel();
        var result = await new ProjectScanner(new()).ScanAsync(new()
        {
            Roots = [w.Root]
        }, null, ct.Token);
        Assert.True(result.Cancelled);
        Assert.Empty(result.CompletedRoots);
    }
}
