using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
using ProjectHub.Core.Services.Projects;
using Xunit;
namespace ProjectHub.Tests;

public sealed class AnalyticsTests
{
    [Fact]
    public async Task CountsSourceAndGeneratedStorageSeparately()
    {
        using var w = new TestWorkspace();
        w.File("src/main.rs", "// TODO fix\nfn main() {}\n\n");
        w.File("target/generated.rs", "// FIXME ignored\nfn fake() {}\n");
        w.File("LICENSE", "MIT");
        w.File("tests/test.rs", "// test\n");
        var (s, date) = await new StatisticsAnalyzer().AnalyzeAsync(w.Root, new(), default);
        Assert.True(s.TotalBytes > s.SourceBytes);
        Assert.Equal(1, s.Languages.Single(x => x.Language == "Rust").Code);
        Assert.Single(s.Todos);
        Assert.Equal("TODO", s.Todos[0].Kind);
        Assert.True(s.HasLicense);
        Assert.True(s.HasTests);
        Assert.True(s.RecoverableBytes > 0);
        Assert.NotEqual(default, date);
    }
    [Theory]
    [InlineData("language:csharp", true)]
    [InlineData("git:dirty", true)]
    [InlineData("size:>1gb", true)]
    [InlineData("favorite:true modified:<30d", true)]
    [InlineData("tag:game", true)]
    [InlineData("framework:godot", false)]
    [InlineData("readme:false", true)]
    [InlineData("name:no", false)]
    public void EvaluatesSmartFilters(string query, bool expected)
    {
        var p = new ProjectRecord { Name = "Tool", Technologies = ["C#", "WinUI 3"], Tags = ["Game"], Favorite = true, LastActivity = DateTimeOffset.UtcNow, Statistics = new() { TotalBytes = 2L << 30 }, Git = new() { IsRepository = true, ChangedFiles = [new("??", "file")] } };
        Assert.Equal(expected, ProjectQuery.Matches(p, query));
    }
    [Fact]
    public void DuplicateSuggestionsNeedEvidence()
    {
        var a = new ProjectRecord { Name = "Game", Technologies = ["Godot"] };
        var b = new ProjectRecord { Name = "Game-backup", Technologies = ["Godot"] };
        var c = new ProjectRecord { Name = "Unrelated", Technologies = ["Rust"] };
        Assert.Single(DuplicateFinder.Find([a, b, c]));
    }
    [Fact]
    public void HealthReasonsExplainEveryPoint()
    {
        var p = new ProjectRecord { LastActivity = DateTimeOffset.UtcNow, Readme = "readme", Git = new() { IsRepository = true }, Statistics = new() { HasTests = true, HasLicense = true } };
        var health = ProjectHealth.Evaluate(p);
        Assert.Equal(100, health.Score);
        Assert.Equal(8, health.Reasons.Count);
    }
    [Theory]
    [InlineData("ProjectHub")]
    [InlineData("projecthub")]
    [InlineData("WinUI")]
    public void PlainTextSearchMatchesNameAndStack(string query)
    {
        Assert.True(ProjectQuery.Matches(new()
        {
            Name = "ProjectHub",
            Technologies = ["WinUI 3"]
        }, query));
    }
    [Fact]
    public async Task IncrementalSourceCacheInvalidatesChangedFiles()
    {
        using var w = new TestWorkspace();
        var file = w.File("main.rs", "fn main() {}\n");
        var analyzer = new StatisticsAnalyzer();
        var (before, _) = await analyzer.AnalyzeAsync(w.Root, new(), default);
        w.File("main.rs", "// TODO change\nfn main() {}\n\n");
        var (after, _) = await analyzer.AnalyzeAsync(w.Root, new(), default);
        Assert.Equal(1, before.Lines);
        Assert.Equal(3, after.Lines);
        Assert.Single(after.Todos);
    }
}
