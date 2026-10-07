using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.ViewModels;
using ProjectHub.Core.Providers;
using Windows.Storage.Pickers;
namespace ProjectHub.App.Views.Settings;

public sealed partial class SettingsPage : UserControl
{
    public SettingsViewModel ViewModel { get; } = new();
    private bool loading=true;
    public SettingsPage()
    {
        InitializeComponent(); DataContext=ViewModel; Populate();
        Loaded+=(_,_)=> { loading=false; ViewModel.HasChanges=false; UpdateState(); };
        ViewModel.PropertyChanged+=(_,e)=> { if(e.PropertyName==nameof(ViewModel.HasChanges)) UpdateState(); };
        SizeChanged+=(_,e)=>Columns.Orientation=e.NewSize.Width<620?Orientation.Vertical:Orientation.Horizontal;
    }
    private void Populate()
    {
        bool previous=loading; loading=true;
        var s=ViewModel.Draft;
        Exclusions.Text=string.Join('\n',s.Exclusions); Depth.Value=s.MaxDepth; Concurrency.Value=s.Concurrency; Budget.Value=s.AnalysisBudgetSeconds; GitInterval.Value=s.GitRefreshSeconds;
        Hotkey.Text=((char)s.HotkeyKey).ToString();
        LanguageSelector.SelectedIndex=s.Language=="en"?1:0;
        Theme.SelectedIndex=s.Theme switch {"Light"=>1,"System"=>2,_=>0};
        PreferredEditor.SelectedItem=s.PreferredEditor.Length>0?s.PreferredEditor:"Automatique";
        StackColumn.IsChecked=s.VisibleColumns.Contains("Stack"); GitColumn.IsChecked=s.VisibleColumns.Contains("Git"); SizeColumn.IsChecked=s.VisibleColumns.Contains("Size"); ActivityColumn.IsChecked=s.VisibleColumns.Contains("Activity"); PathColumn.IsChecked=s.VisibleColumns.Contains("Path");
        UpdateEmptyStates(); loading=previous; UpdateState();
    }
    private void UpdateState()
    {
        if(SaveState==null) return;
        SaveState.Text=ViewModel.HasChanges?"Modifications non enregistrées.":"Tous les paramètres sont enregistrés.";
    }
    private void UpdateEmptyStates()
    {
        EmptyRoots.Visibility=ViewModel.Roots.Count==0?Visibility.Visible:Visibility.Collapsed;
        EmptyTools.Visibility=ViewModel.Tools.Count==0?Visibility.Visible:Visibility.Collapsed;
    }
    private void Changed(object sender,RoutedEventArgs e) { if(!loading) ViewModel.HasChanges=true; }
    private void TextChanged(object sender,TextChangedEventArgs e) { if(!loading) ViewModel.HasChanges=true; }
    private void ChoiceChanged(object sender,SelectionChangedEventArgs e) { if(!loading) ViewModel.HasChanges=true; }
    private void NumberChanged(NumberBox sender,NumberBoxValueChangedEventArgs e) { if(!loading) ViewModel.HasChanges=true; }
    private void Notify(string message,InfoBarSeverity severity=InfoBarSeverity.Success)
    {
        Feedback.Message=message; Feedback.Severity=severity; Feedback.IsOpen=true;
    }
    private async void Save_Click(object sender,RoutedEventArgs e)
    {
        if(ViewModel.IsSaving) return;
        if(ViewModel.Draft.HotkeyEnabled && (Hotkey.Text.Length!=1 || !char.IsAsciiLetterOrDigit(Hotkey.Text[0]))) { Notify("Choisissez une lettre ou un chiffre pour le raccourci.",InfoBarSeverity.Error); Hotkey.Focus(FocusState.Programmatic); return; }
        SaveButton.IsEnabled=false; DiscardButton.IsEnabled=false;
        try
        {
            var s=ViewModel.Draft;
            s.Exclusions=Exclusions.Text.Split(['\n','\r'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            s.MaxDepth=Number(Depth,12); s.Concurrency=Number(Concurrency,3); s.AnalysisBudgetSeconds=Number(Budget,120); s.GitRefreshSeconds=Number(GitInterval,120);
            s.Theme=((ComboBoxItem)Theme.SelectedItem).Tag.ToString()!;
            s.Language=((ComboBoxItem)LanguageSelector.SelectedItem).Tag.ToString()!;
            s.PreferredEditor=PreferredEditor.SelectedItem is string editor && editor!="Automatique"?editor:"";
            if(Hotkey.Text.Length==1) s.HotkeyKey=char.ToUpperInvariant(Hotkey.Text[0]);
            s.VisibleColumns=[];
            foreach(var (key,control) in new[] {("Stack",StackColumn),("Git",GitColumn),("Size",SizeColumn),("Activity",ActivityColumn),("Path",PathColumn)}) if(control.IsChecked==true) s.VisibleColumns.Add(key);
            await ViewModel.SaveAsync(); App.Language.SetLanguage(s.Language); Notify("Paramètres enregistrés."); App.Main.ApplyLocalization();
        }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
        finally { SaveButton.IsEnabled=DiscardButton.IsEnabled=ViewModel.HasChanges; }
    }
    private static int Number(NumberBox box,int fallback)=>double.IsNaN(box.Value)?fallback:(int)Math.Clamp(box.Value,box.Minimum,box.Maximum);
    private void Discard_Click(object sender,RoutedEventArgs e) { loading=true; ViewModel.Reload(); Populate(); loading=false; Feedback.IsOpen=false; }
    private async void AddRoot_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var picker=new FolderPicker(); picker.FileTypeFilter.Add("*"); WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(App.Main));
            var folder=await picker.PickSingleFolderAsync(); if(folder==null) return;
            if(!ViewModel.Roots.Contains(folder.Path,StringComparer.OrdinalIgnoreCase)) { ViewModel.Roots.Add(folder.Path); ViewModel.HasChanges=true; UpdateEmptyStates(); }
        }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
    private void RemoveRoot_Click(object sender,RoutedEventArgs e) { ViewModel.Roots.Remove((string)((Button)sender).Tag); ViewModel.HasChanges=true; UpdateEmptyStates(); }
    private async void RefreshTools_Click(object sender,RoutedEventArgs e)
    {
        try { SetTools(await Task.Run(()=>new ToolProvider().Detect(ViewModel.Draft))); }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
    private async void BrowseTool_Click(object sender,RoutedEventArgs e)
    {
        try { var picker=new FileOpenPicker(); picker.FileTypeFilter.Add(".exe"); WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(App.Main)); var file=await picker.PickSingleFileAsync(); if(file!=null) ToolPath.Text=file.Path; }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
    private async void ConfigureTool_Click(object sender,RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(ToolName.Text) || !File.Exists(ToolPath.Text)) { Notify("Indiquez un nom et un exécutable existant.",InfoBarSeverity.Error); return; }
        ViewModel.Draft.ToolOverrides[ToolName.Text.Trim()]=ToolPath.Text.Trim(); ViewModel.HasChanges=true;
        try { SetTools(await Task.Run(()=>new ToolProvider().Detect(ViewModel.Draft))); Notify("Outil ajouté. Enregistrez pour l’utiliser dans la bibliothèque.",InfoBarSeverity.Informational); }
        catch(Exception ex) { Notify(ex.Message,InfoBarSeverity.Error); }
    }
    private void SetTools(IEnumerable<InstalledTool> tools)
    {
        string selected=PreferredEditor.SelectedItem as string ?? "Automatique";
        bool previous=loading; loading=true;
        ViewModel.RefreshTools(tools); PreferredEditor.SelectedItem=ViewModel.Editors.Contains(selected)?selected:"Automatique";
        loading=previous; UpdateEmptyStates();
    }
}
