using System.Text.Json;

namespace WebGrabber.Services;

public class JobStateService
{
    private readonly object _sync = new();
    private readonly string _filePath;

    public bool IsRunning { get; private set; }
    public int? LastDiscoveryCount { get; private set; }
    public DateTime? LastDiscoveryTime { get; private set; }
    public string? LastDiscoveryUrl { get; private set; }

    public JobStateService()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "last_discovery.json");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var txt = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(txt)) return;
            var trimmed = txt.TrimStart();
            if (!trimmed.StartsWith("{")) return;

            using var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            if (root.TryGetProperty("PagesFound", out var pf))
            {
                if (pf.ValueKind == JsonValueKind.Number && pf.TryGetInt32(out var n))
                {
                    LastDiscoveryCount = n;
                }
                else if (pf.ValueKind == JsonValueKind.String && int.TryParse(pf.GetString(), out var sn))
                {
                    LastDiscoveryCount = sn;
                }
            }

            if (root.TryGetProperty("StartUrl", out var su) && su.ValueKind == JsonValueKind.String)
            {
                LastDiscoveryUrl = su.GetString();
            }

            if (root.TryGetProperty("Timestamp", out var ts))
            {
                if (ts.ValueKind == JsonValueKind.String)
                {
                    var s = ts.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, out var dt))
                    {
                        LastDiscoveryTime = dt;
                    }
                }
                else if (ts.ValueKind == JsonValueKind.Number && ts.TryGetInt64(out var ticks))
                {
                    try { LastDiscoveryTime = DateTime.FromFileTimeUtc(ticks); } catch { }
                }
            }
        }
        catch
        {
            // ignore malformed or unreadable file
        }
    }

    private void Persist()
    {
        try
        {
            var rec = new LastDiscoveryRecord
            {
                PagesFound = LastDiscoveryCount ?? 0,
                Timestamp = LastDiscoveryTime ?? DateTime.UtcNow,
                StartUrl = LastDiscoveryUrl ?? string.Empty
            };
            var txt = JsonSerializer.Serialize(rec);
            File.WriteAllText(_filePath, txt);
        }
        catch { }
    }

    public bool TryBeginJob()
    {
        lock (_sync)
        {
            if (IsRunning) return false;
            IsRunning = true;
            return true;
        }
    }

    public void EndJob()
    {
        lock (_sync)
        {
            IsRunning = false;
        }
    }

    public void SetLastDiscovery(int pagesFound, string startUrl)
    {
        lock (_sync)
        {
            LastDiscoveryCount = pagesFound;
            LastDiscoveryTime = DateTime.UtcNow;
            LastDiscoveryUrl = startUrl;
            Persist();
        }
    }

    private record LastDiscoveryRecord
    {
        public int PagesFound { get; init; }
        public DateTime Timestamp { get; init; }
        public string StartUrl { get; init; } = string.Empty;
    }
}
