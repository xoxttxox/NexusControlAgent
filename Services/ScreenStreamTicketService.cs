using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NexusControl.Agent.Services;

internal sealed class ScreenStreamTicketService
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, ScreenStreamGrant> _tickets =
        new(StringComparer.Ordinal);

    public ScreenStreamTicket Create(
        string deviceId,
        int displayId,
        int targetFps,
        string? profile)
    {
        CleanupExpired();

        var normalizedFps = Math.Clamp(targetFps, 15, 60);
        var normalizedProfile = profile?.Trim().ToLowerInvariant() switch
        {
            "quality" => "quality",
            "balanced" => "balanced",
            _ => "performance",
        };
        var expiresAt = DateTimeOffset.UtcNow.Add(TicketLifetime);
        string ticket;
        do
        {
            ticket = Base64Url(RandomNumberGenerator.GetBytes(32));
        }
        while (!_tickets.TryAdd(
            ticket,
            new ScreenStreamGrant(
                deviceId,
                displayId,
                normalizedFps,
                normalizedProfile,
                expiresAt)));

        return new ScreenStreamTicket(ticket, expiresAt);
    }

    public bool TryConsume(string ticket, out ScreenStreamGrant? grant)
    {
        grant = null;
        if (string.IsNullOrWhiteSpace(ticket)
            || !_tickets.TryRemove(ticket, out var stored)
            || stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        grant = stored;
        return true;
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _tickets)
        {
            if (item.Value.ExpiresAt <= now)
            {
                _tickets.TryRemove(item.Key, out _);
            }
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

internal sealed record ScreenStreamTicket(
    string Value,
    DateTimeOffset ExpiresAt);

internal sealed record ScreenStreamGrant(
    string DeviceId,
    int DisplayId,
    int TargetFps,
    string Profile,
    DateTimeOffset ExpiresAt);
