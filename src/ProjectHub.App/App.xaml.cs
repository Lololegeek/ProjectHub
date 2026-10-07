using Microsoft.UI.Xaml;
using ProjectHub.Core.Services.Projects;
using ProjectHub.Core.Services.Launching;
using ProjectHub.Core.Providers;
using ProjectHub.App.Services;
namespace ProjectHub.App;

public partial class App : Application
{
    public static HubEngine Engine { get; private set; } = null!;
    public static TaskRunner Runner { get; } = new();
    public static LauncherService Launcher { get; } = new();
    public static List<InstalledTool> Tools { get; set; } = [];
    public static LocalizationService Language { get; } = new();
    public static MainWindow Main { get; private set; } = null!;
    public App()
    {
        InitializeComponent();
        var data = Environment.GetEnvironmentVariable("PROJECTHUB_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjectHub");
        Engine = new(data);
        UnhandledException += (_, e) => { Engine.Log.Write("UI", e.Exception.ToString()); if (Main != null) { Main.ViewModel.Status = "Erreur : " + e.Message; e.Handled = true; } };
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Main = new MainWindow();
        Main.Activate();
    }
}
