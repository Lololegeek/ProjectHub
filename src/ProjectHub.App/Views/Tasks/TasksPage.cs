using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ProjectHub.App.Controls;
using ProjectHub.Core.Services.Launching;
namespace ProjectHub.App.Views.Tasks;

public sealed class TasksPage : UserControl
{
    private readonly ListView list = new() { DisplayMemberPath = "Name", MaxHeight = 160 };
    private readonly TextBox output = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"), FontSize = 12, MinHeight = 280 };
    private readonly TextBlock status = PageElements.Text("");
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    public TasksPage()
    {
        var panel = PageElements.Column();
        Content = PageElements.Scroll(panel);
        panel.Children.Add(PageElements.Heading("Tâches"));
        panel.Children.Add(PageElements.Text("Commandes lancées depuis ProjectHub · stdout, stderr et durée"));
        panel.Children.Add(list);
        panel.Children.Add(PageElements.Row(status, PageElements.Button("Arrêter", () => { if (list.SelectedItem is RunningTask task) task.Stop(); })));
        panel.Children.Add(output);
        list.SelectionChanged += (_, _) => Refresh();
        timer.Tick += (_, _) => Refresh();
        Loaded += (_, _) => timer.Start();
        Unloaded += (_, _) => timer.Stop();
        Refresh();
    }
    private void Refresh()
    {
        if (list.Items.Count != App.Runner.Tasks.Count)
        {
            var selected = list.SelectedIndex;
            list.ItemsSource = App.Runner.Tasks.ToArray();
            list.SelectedIndex = selected < 0 ? list.Items.Count - 1 : selected;
        }
        if (list.SelectedItem is RunningTask task)
        {
            status.Text = task.Status;
            var text = task.Output;
            if (output.Text != text)
                output.Text = text;
        }
        else
            status.Text = "Aucune commande lancée. Ouvrez un projet, puis l’onglet Lancer.";
    }
}
