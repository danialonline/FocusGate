using FocusGate.Core;
using System.Windows;
namespace FocusGate.Desktop;
public partial class MainWindow : Window
{
    private readonly GoogleSync google = new();
    private AppSettings settings = AppSettings.Load();
    public MainWindow() { InitializeComponent(); OAuthPath.Text = settings.OAuthClientSecretsPath ?? ""; Refresh(); }
    private void Refresh() { Rules.ItemsSource = null; Rules.ItemsSource = settings.Rules.Select(r => $"{r.Name} — {r.Mode} — {r.Domains.Count} domains, {r.Executables.Count} apps").ToList(); }
    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        settings.OAuthClientSecretsPath = OAuthPath.Text.Trim(); settings.Save();
        Status.Text = "Connecting...";
        var snapshot = await google.ReadAsync(settings, CancellationToken.None);
        Status.Text = snapshot.Healthy ? $"Connected. Tasks incomplete: {snapshot.TasksIncomplete}; event active: {snapshot.CalendarActive}" : "Connection failed: " + snapshot.Message;
    }
    private async Task<bool> IsLocked()
    {
        if (settings.StrictUntilUtc > DateTimeOffset.UtcNow) return true;
        if (!settings.Rules.Any(r => r.Enabled)) return false;
        var snapshot = await google.ReadAsync(settings, CancellationToken.None);
        return settings.Rules.Any(r => RuleEvaluator.IsBlocked(r, snapshot));
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (await IsLocked()) { MessageBox.Show("An active rule prevents edits. See administrator recovery instructions."); return; }
        settings.OAuthClientSecretsPath = OAuthPath.Text.Trim();
        settings.Rules.Add(new BlockRule { Name = RuleName.Text, Mode = (RuleMode)Mode.SelectedIndex,
            Domains = Domains.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList(),
            Executables = Executables.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() });
        settings.Save(); Refresh(); Status.Text = "Rule saved. Service applies it within 5 seconds.";
    }
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (await IsLocked()) { MessageBox.Show("Rule is active; editing locked."); return; }
        if (Rules.SelectedIndex is int i && i >= 0 && i < settings.Rules.Count) { settings.Rules.RemoveAt(i); settings.Save(); Refresh(); }
    }
}
