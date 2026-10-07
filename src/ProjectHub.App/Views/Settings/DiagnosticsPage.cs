using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.Controls;
using ProjectHub.Core.Models;
using Windows.Storage.Pickers;
namespace ProjectHub.App.Views.Settings;

public sealed class DiagnosticsPage : UserControl
{
    public DiagnosticsPage()
    {
        var panel = PageElements.Column();
        Content = PageElements.Scroll(panel);
        panel.Children.Add(PageElements.Heading("Diagnostics"));
        var engine = App.Engine;
        var db = new FileInfo(engine.Repository.DatabasePath);
        panel.Children.Add(PageElements.Text($"ProjectHub 1.0.0 · .NET {Environment.Version}\nBase de données : {db.FullName}\nCache : {ProjectRecord.FormatSize(db.Exists ? db.Length : 0)}\nProjets : {engine.Projects.Count}\nSurveillances : {engine.Watcher.Count}\nScan en cours : {App.Main.ViewModel.IsScanning}\nDernier scan : {engine.LastScanDuration.TotalSeconds:0.0}s\nErreurs de scan : {engine.ScanErrors.Count}\nLogs : {engine.Log.DirectoryPath}"));
        panel.Children.Add(PageElements.Button("Exporter les logs", async () => { var picker = new FileSavePicker { SuggestedFileName = "projecthub-logs" }; picker.FileTypeChoices.Add("Log", new List<string> { ".log" }); WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Main)); var file = await picker.PickSaveFileAsync(); if (file != null) { var source = Path.Combine(engine.Log.DirectoryPath, "projecthub.log"); if (File.Exists(source)) File.Copy(source, file.Path, true); } }));
        foreach (var error in engine.ScanErrors.Take(200))
            panel.Children.Add(PageElements.Text(error));
    }
}
