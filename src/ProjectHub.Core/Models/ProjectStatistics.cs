namespace ProjectHub.Core.Models;

public sealed class ProjectStatistics
{
    public long TotalBytes
    {
        get; set;
    }
    public long SourceBytes
    {
        get; set;
    }
    public long FileCount
    {
        get; set;
    }
    public bool Truncated
    {
        get; set;
    }
    public DateTimeOffset MeasuredAt
    {
        get; set;
    }
    public long AnalysisMilliseconds
    {
        get; set;
    }
    public List<LanguageStatistics> Languages { get; set; } = [];
    public List<TodoItem> Todos { get; set; } = [];
    public List<StorageEntry> Storage { get; set; } = [];
    public int LargeFiles
    {
        get; set;
    }
    public bool HasTests
    {
        get; set;
    }
    public bool HasLicense
    {
        get; set;
    }
    public long Lines => Languages.Sum(x => x.Code + x.Comments + x.Blank);
    public long RecoverableBytes => Storage.Where(x => x.Regenerable).Sum(x => x.Bytes);
}
public sealed class LanguageStatistics
{
    public string Language { get; set; } = "";
    public long Code
    {
        get; set;
    }
    public long Comments
    {
        get; set;
    }
    public long Blank
    {
        get; set;
    }
}
public sealed record TodoItem(string Kind, string Path, int Line, string Text);
public sealed record StorageEntry(string Name, string Path, long Bytes, bool Regenerable);
