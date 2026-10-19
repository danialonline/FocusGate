using System.Text.Json;
namespace FocusGate.Core;
public enum RuleMode { Permanent, Tasks, Calendar, Either, Both }
public sealed class BlockRule
{
    public string Name { get; set; } = "Focus";
    public RuleMode Mode { get; set; } = RuleMode.Tasks;
    public List<string> Domains { get; set; } = new();
    public List<string> Executables { get; set; } = new();
    public List<string> TaskListIds { get; set; } = new();
    public List<string> CalendarIds { get; set; } = new() { "primary" };
    public bool Enabled { get; set; } = true;
}
public sealed class AppSettings
{
    public List<BlockRule> Rules { get; set; } = new();
    public DateTimeOffset? StrictUntilUtc { get; set; }
    public string? OAuthClientSecretsPath { get; set; }
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FocusGate");
    public static string PathName => Path.Combine(Folder, "settings.json");
    public static AppSettings Load() { Directory.CreateDirectory(Folder); return File.Exists(PathName) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(PathName), JsonOptions()) ?? new() : new(); }
    public void Save() { Directory.CreateDirectory(Folder); File.WriteAllText(PathName, JsonSerializer.Serialize(this, JsonOptions())); }
    public static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true, PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
}
public record GoogleSnapshot(bool TasksIncomplete, bool CalendarActive, bool Healthy, string Message);
public static class RuleEvaluator
{
    public static bool IsBlocked(BlockRule rule, GoogleSnapshot snapshot) => rule.Enabled && (rule.Mode switch
    {
        RuleMode.Permanent => true,
        RuleMode.Tasks => !snapshot.Healthy || snapshot.TasksIncomplete,
        RuleMode.Calendar => !snapshot.Healthy || snapshot.CalendarActive,
        RuleMode.Either => !snapshot.Healthy || snapshot.TasksIncomplete || snapshot.CalendarActive,
        RuleMode.Both => !snapshot.Healthy || snapshot.TasksIncomplete && snapshot.CalendarActive,
        _ => false
    });
}
