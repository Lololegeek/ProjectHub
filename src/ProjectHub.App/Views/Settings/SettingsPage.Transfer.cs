using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.ViewModels;
using ProjectHub.Core.Services.Persistence;
using Windows.Storage.Pickers;
namespace ProjectHub.App.Views.Settings;

public sealed partial class SettingsPage
{
    private async void Export_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var picker=new FileSavePicker { SuggestedFileName="projecthub-config" }; picker.FileTypeChoices.Add("JSON",new List<string> {".json"});
            WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(App.Main));
            var file=await picker.PickSaveFileAsync(); if(file==null) return;
            await ConfigurationTransfer.ExportAsync(file.Path,App.Engine.Settings,App.Engine.Projects);
            Notify(ViewModel.HasChanges?"Configuration enregistrée exportée. Les modifications en cours ne sont pas incluses.":"Configuration exportée.");
        }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
    private async void Import_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var picker=new FileOpenPicker(); picker.FileTypeFilter.Add(".json"); WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(App.Main));
            var file=await picker.PickSingleFileAsync(); if(file==null) return;
            var import=await ConfigurationTransfer.ReadAsync(file.Path);
            var dialog=new ContentDialog { XamlRoot=XamlRoot,Title="Importer cette configuration ?",Content=$"{import.Settings.Roots.Count} dossiers et {import.Projects.Count} projets. Les préférences actuelles et les modifications en cours seront remplacées.",PrimaryButtonText="Importer",CloseButtonText="Annuler" };
            if(await dialog.ShowAsync()!=ContentDialogResult.Primary) return;
            var settings=SettingsViewModel.Clone(App.Engine.Settings);
            ConfigurationTransfer.Apply(import,settings,App.Engine.Projects);
            await App.Engine.Repository.SaveProjectsAsync(App.Engine.Projects); await App.Engine.UpdateSettingsAsync(settings);
            App.Tools=await Task.Run(()=>new ProjectHub.Core.Providers.ToolProvider().Detect(settings));
            loading=true; ViewModel.Reload(); Populate(); loading=false;
            App.Main.ApplySettings(); App.Main.ViewModel.VisibleProjects.Clear(); App.Main.ViewModel.Refresh();
            Notify("Configuration importée. Scannez les dossiers pour retrouver les projets.");
        }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
}
