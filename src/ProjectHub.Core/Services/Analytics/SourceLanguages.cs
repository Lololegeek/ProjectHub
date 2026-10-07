namespace ProjectHub.Core.Services.Analytics;

public static class SourceLanguages
{
    public static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "C#",
        [".fs"] = "F#",
        [".gd"] = "GDScript",
        [".rs"] = "Rust",
        [".ts"] = "TypeScript",
        [".tsx"] = "TypeScript",
        [".js"] = "JavaScript",
        [".jsx"] = "JavaScript",
        [".vue"] = "Vue",
        [".svelte"] = "Svelte",
        [".py"] = "Python",
        [".java"] = "Java",
        [".kt"] = "Kotlin",
        [".cpp"] = "C++",
        [".cc"] = "C++",
        [".c"] = "C",
        [".h"] = "C/C++",
        [".hpp"] = "C++",
        [".lua"] = "Luau",
        [".luau"] = "Luau",
        [".json"] = "JSON",
        [".toml"] = "TOML",
        [".yaml"] = "YAML",
        [".yml"] = "YAML",
        [".md"] = "Markdown",
        [".xaml"] = "XAML",
        [".xml"] = "XML",
        [".html"] = "HTML",
        [".css"] = "CSS",
        [".scss"] = "SCSS",
        [".ps1"] = "PowerShell",
        [".sh"] = "Shell",
        [".sql"] = "SQL",
        [".go"] = "Go",
        [".swift"] = "Swift",
        [".dart"] = "Dart",
        [".rb"] = "Ruby",
        [".php"] = "PHP"
    };
}
