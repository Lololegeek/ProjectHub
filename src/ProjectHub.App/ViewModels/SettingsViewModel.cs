using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjectHub.Core.Models;
using ProjectHub.Core.Providers;
namespace ProjectHub.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] public partial HubSettings Draft { get; set; } = Clone(App.Engine.Settings);
    [ObservableProperty] public partial bool HasChanges { get; set; }
    [ObservableProperty] public partial bool IsSaving { get; set; }
    public ObservableCollection<string> Roots { get; } = [];
    public ObservableCollection<InstalledTool> Tools { get; } = [];
    public ObservableCollection<string> Editors { get; } = [];
    public SettingsViewModel() => Reload();
    public static HubSettings Clone(HubSettings value) => JsonSerializer.Deserialize<HubSettings>(JsonSerializer.Serialize(value))!;
    public void Reload()
    {
        Draft=Clone(App.Engine.Settings);
        Roots.Clear(); foreach(var root in Draft.Roots) Roots.Add(root);
        RefreshTools(App.Tools);
        HasChanges=false;
    }
    public void RefreshTools(IEnumerable<InstalledTool> tools)
    {
        Tools.Clear(); foreach(var tool in tools) Tools.Add(tool);
        Editors.Clear(); Editors.Add("Automatique"); foreach(var tool in Tools) Editors.Add(tool.Name);
        if(Draft.PreferredEditor.Length>0 && !Editors.Contains(Draft.PreferredEditor)) Editors.Add(Draft.PreferredEditor);
    }
    public async Task SaveAsync()
    {
        IsSaving=true;
        try
        {
            Draft.Roots=Roots.ToList();
            await App.Engine.UpdateSettingsAsync(Clone(Draft));
            App.Tools=await Task.Run(()=>new ToolProvider().Detect(App.Engine.Settings));
            RefreshTools(App.Tools);
            App.Main.ApplySettings();
            App.Main.ViewModel.VisibleProjects.Clear(); App.Main.ViewModel.Refresh();
            HasChanges=false;
        }
        finally { IsSaving=false; }
    }
}
