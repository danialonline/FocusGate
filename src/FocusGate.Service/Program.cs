using FocusGate.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;
using System.Text;

Host.CreateDefaultBuilder(args).UseWindowsService(options => options.ServiceName = "FocusGate")
    .ConfigureServices(services => services.AddHostedService<Enforcer>()).Build().Run();

sealed class Enforcer : BackgroundService
{
    private readonly GoogleSync google = new();
    private readonly string hosts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
    private const string Begin = "# BEGIN FOCUSGATE MANAGED";
    private const string End = "# END FOCUSGATE MANAGED";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = AppSettings.Load();
                var snapshot = settings.Rules.Any(r => r.Enabled && r.Mode != RuleMode.Permanent)
                    ? await google.ReadAsync(settings, stoppingToken)
                    : new GoogleSnapshot(false, false, true, "No Google rules");
                var rules = settings.Rules.Where(r => RuleEvaluator.IsBlocked(r, snapshot)).ToList();
                UpdateHosts(rules.SelectMany(r => r.Domains));
                var names = rules.SelectMany(r => r.Executables).Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                names.Remove("explorer.exe"); names.Remove("winlogon.exe"); names.Remove("lsass.exe");
                names.Remove("services.exe"); names.Remove("svchost.exe"); names.Remove("FocusGate.Service.exe");
                names.Remove("FocusGate.Desktop.exe");
                foreach (var process in Process.GetProcesses())
                {
                    try { if (names.Contains(process.ProcessName + ".exe")) process.Kill(); }
                    catch (Exception) { /* protected/system processes cannot be terminated */ }
                    finally { process.Dispose(); }
                }
            }
            catch (Exception ex) { try { File.AppendAllText(Path.Combine(AppSettings.Folder, "service.log"), $"{DateTimeOffset.Now}: {ex}\n"); } catch { } }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    private void UpdateHosts(IEnumerable<string> domains)
    {
        var text = File.ReadAllText(hosts);
        int start = text.IndexOf(Begin, StringComparison.Ordinal);
        if (start >= 0) { int finish = text.IndexOf(End, start, StringComparison.Ordinal); if (finish >= 0) text = text.Remove(start, finish + End.Length - start); }
        var valid = domains.Select(d => d.Trim().ToLowerInvariant()).Where(d => d.Length <= 253 && d.Length > 0 && d.Split('.').All(label => label.Length > 0 && label.Length <= 63 && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))).Distinct();
        var block = new StringBuilder().AppendLine(Begin);
        foreach (var d in valid) { block.AppendLine($"0.0.0.0 {d}"); block.AppendLine($"0.0.0.0 www.{d}"); }
        block.AppendLine(End);
        var desired = text.TrimEnd() + Environment.NewLine + block;
        if (desired != File.ReadAllText(hosts)) File.WriteAllText(hosts, desired);
    }
}
