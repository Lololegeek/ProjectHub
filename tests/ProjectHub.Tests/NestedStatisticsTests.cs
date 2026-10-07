using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Analytics;
using Xunit;
namespace ProjectHub.Tests;

public sealed class NestedStatisticsTests
{
    [Fact]
    public async Task IndependentNestedProjectsDoNotDuplicateCodeOrGlobalStorage()
    {
        using var w = new TestWorkspace();
        w.File("parent/main.cs", "class A {}\n");
        w.File("parent/child/main.rs", "fn main() {}\n");
        var parent = Path.Combine(w.Root, "parent");
        var child = Path.Combine(parent, "child");
        var analyzer = new StatisticsAnalyzer();
        var (parentStats, _) = await analyzer.AnalyzeAsync(parent, new(), default, [child]);
        var (childStats, _) = await analyzer.AnalyzeAsync(child, new(), default);
        Assert.Equal(1, parentStats.Lines);
        Assert.Equal(1, childStats.Lines);
        var all = new[] { new ProjectRecord { Path = parent, Statistics = parentStats }, new ProjectRecord { Path = child, Statistics = childStats } };
        Assert.Equal(parentStats.TotalBytes, LibraryAnalytics.TotalBytes(all));
        Assert.Single(LibraryAnalytics.StorageRoots(all));
    }
}
