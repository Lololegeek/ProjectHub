namespace ProjectHub.Core.Infrastructure;

public sealed class HubLog
{
    private readonly object gate = new();
    public string DirectoryPath
    {
        get;
    }
    public HubLog(string path)
    {
        DirectoryPath = path;
        Directory.CreateDirectory(path);
    }
    public void Write(string category, string message)
    {
        lock (gate)
        {
            try
            {
                var file = Path.Combine(DirectoryPath, "projecthub.log");
                if (File.Exists(file) && new FileInfo(file).Length > 2_000_000)
                {
                    for (int i = 3; i >= 1; i--)
                    {
                        var old = file + "." + i;
                        if (File.Exists(old))
                            File.Move(old, file + "." + (i + 1), true);
                    }
                    File.Move(file, file + ".1", true);
                    if (File.Exists(file + ".4"))
                        File.Delete(file + ".4");
                }
                File.AppendAllText(file, $"{DateTimeOffset.Now:O} [{category}] {message.ReplaceLineEndings(" ")}\n");
            }
            catch (IOException) { }
        }
    }
}
