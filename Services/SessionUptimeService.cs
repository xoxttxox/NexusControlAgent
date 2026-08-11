using System.Diagnostics;

namespace NexusControl.Agent.Services;

internal sealed class SessionUptimeService
{
    private readonly DateTimeOffset _sessionStartedAt = FindSessionStart();

    public long GetUptimeSeconds()
    {
        var elapsed = DateTimeOffset.Now - _sessionStartedAt;
        return Math.Max(0, (long)elapsed.TotalSeconds);
    }

    private static DateTimeOffset FindSessionStart()
    {
        var fallback = new DateTimeOffset(
            Process.GetCurrentProcess().StartTime);
        if (!OperatingSystem.IsWindows())
        {
            return fallback;
        }

        var currentSessionId = Process.GetCurrentProcess().SessionId;
        var candidates = new List<DateTimeOffset>();
        foreach (var explorer in Process.GetProcessesByName("explorer"))
        {
            using (explorer)
            {
                try
                {
                    if (explorer.SessionId == currentSessionId)
                    {
                        candidates.Add(new DateTimeOffset(explorer.StartTime));
                    }
                }
                catch
                {
                    // A process may exit while its start time is being read.
                }
            }
        }

        return candidates.Count > 0
            ? candidates.Min()
            : fallback;
    }
}
