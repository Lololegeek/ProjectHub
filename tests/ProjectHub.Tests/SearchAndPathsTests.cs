using ProjectHub.Core.Models;
using ProjectHub.Core.Services.Projects;
using ProjectHub.Core.Utilities;
using Xunit;
namespace ProjectHub.Tests;

public sealed class SearchAndPathsTests
{
    [Fact]
    public void DriveRootContainsDescendantsButSimilarlyNamedFoldersDoNot()
    {
        Assert.True(SafeFileSystem.Within(@"D:\Code\App", @"D:\"));
        Assert.True(SafeFileSystem.Within(@"D:\Code\App", @"D:\Code"));
        Assert.False(SafeFileSystem.Within(@"D:\CodeBackup\App", @"D:\Code"));
    }
    [Fact]
    public void PathsAndRemoteUrlsRemainSearchableAsOrdinaryText()
    {
        var p = new ProjectRecord { Name = "App", Path = @"D:\Code\App", Git = new() { Remote = "https://github.com/owner/app" } };
        Assert.True(ProjectQuery.Matches(p, @"D:\Code"));
        Assert.True(ProjectQuery.Matches(p, "https://github.com/owner"));
    }
    [Fact]
    public void SearchCacheRefreshesAfterMetadataChanges()
    {
        var p = new ProjectRecord { Name = "Before", Tags = ["old"] };
        Assert.True(ProjectQuery.Matches(p, "Before"));
        p.Name = "After";
        p.Tags = ["new"];
        Assert.True(ProjectQuery.Matches(p, "After"));
        Assert.False(ProjectQuery.Matches(p, "Before"));
        Assert.True(ProjectQuery.Matches(p, "new"));
    }
}
