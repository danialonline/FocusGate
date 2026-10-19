using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using Google.Apis.Util.Store;
using System.Text.Json;
namespace FocusGate.Core;
public sealed class GoogleSync
{
    public async Task<GoogleSnapshot> ReadAsync(AppSettings settings, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.OAuthClientSecretsPath) || !File.Exists(settings.OAuthClientSecretsPath))
            return new(false, false, false, "Google OAuth credentials not configured");
        try
        {
            await using var stream = File.OpenRead(settings.OAuthClientSecretsPath);
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                new[] { CalendarService.Scope.CalendarReadonly, TasksService.Scope.TasksReadonly },
                "local-user", ct, new FileDataStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusGate", "google-tokens"), true));
            var init = new BaseClientService.Initializer { HttpClientInitializer = credential, ApplicationName = "FocusGate" };
            using var tasks = new TasksService(init);
            using var calendar = new CalendarService(init);
            bool incomplete = false, active = false;
            var lists = await tasks.Tasklists.List().ExecuteAsync(ct);
            foreach (var list in lists.Items ?? new System.Collections.Generic.List<Google.Apis.Tasks.v1.Data.TaskList>())
            {
                var req = tasks.Tasks.List(list.Id); req.ShowCompleted = false; req.ShowHidden = false; req.MaxResults = 100;
                string? page;
                do { var result = await req.ExecuteAsync(ct); incomplete |= (result.Items ?? new System.Collections.Generic.List<Google.Apis.Tasks.v1.Data.Task>()).Any(t => t.Status != "completed"); page = result.NextPageToken; req.PageToken = page; } while (page != null);
            }
            var now = DateTime.UtcNow;
            var events = calendar.Events.List("primary"); events.TimeMinDateTimeOffset = new DateTimeOffset(now.AddMinutes(-1)); events.TimeMaxDateTimeOffset = new DateTimeOffset(now.AddMinutes(1)); events.SingleEvents = true;
            var response = await events.ExecuteAsync(ct);
            active = (response.Items ?? new System.Collections.Generic.List<Google.Apis.Calendar.v3.Data.Event>()).Any(e => e.Status != "cancelled" && e.Transparency != "transparent" && (e.Start?.DateTimeDateTimeOffset?.UtcDateTime ?? DateTime.MaxValue) <= now && (e.End?.DateTimeDateTimeOffset?.UtcDateTime ?? DateTime.MinValue) > now);
            return new(incomplete, active, true, "Google sync OK");
        }
        catch (Exception ex) { return new(false, false, false, ex.Message); }
    }
}
