using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NexusControl.Agent.Services;

namespace NexusControl.Agent.Networking;

internal sealed class ScreenStreamWebSocketHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ScreenStreamTicketService _tickets;
    private readonly ScreenCaptureService _screenCapture;

    public ScreenStreamWebSocketHandler(
        ScreenStreamTicketService tickets,
        ScreenCaptureService screenCapture)
    {
        _tickets = tickets;
        _screenCapture = screenCapture;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var ticket = context.Request.Query["ticket"].ToString();
        if (!_tickets.TryConsume(ticket, out var grant) || grant is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var requestedProtocols = context.WebSockets.WebSocketRequestedProtocols;
        var protocol = requestedProtocols.Contains(
            "nexus-screen-v1",
            StringComparer.Ordinal)
            ? "nexus-screen-v1"
            : null;
        using var socket = await context.WebSockets.AcceptWebSocketAsync(protocol);

        try
        {
            var displays = _screenCapture.GetDisplays();
            var display = displays.FirstOrDefault(item => item.Id == grant.DisplayId);
            if (display is null)
            {
                await SendJsonAsync(
                    socket,
                    new
                    {
                        type = "error",
                        message = "Der ausgewählte Bildschirm ist nicht mehr verfügbar.",
                    },
                    context.RequestAborted);
                await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData);
                return;
            }

            var profile = ScreenStreamProfile.Resolve(grant.Profile);
            await SendJsonAsync(
                socket,
                new
                {
                    type = "ready",
                    displayId = display.Id,
                    display.Name,
                    sourceWidth = display.Width,
                    sourceHeight = display.Height,
                    targetFps = grant.TargetFps,
                    profile = profile.Name,
                },
                context.RequestAborted);

            await StreamFramesAsync(
                socket,
                grant,
                display,
                profile,
                context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Closing the app view cancels the stream immediately.
        }
        catch (WebSocketException)
        {
            // The iPhone closed the stream without a WebSocket close frame.
        }
        catch (Exception error)
        {
            if (socket.State == WebSocketState.Open)
            {
                await SendJsonAsync(
                    socket,
                    new
                    {
                        type = "error",
                        message = $"Bildschirmstream wurde beendet: {error.Message}",
                    },
                    CancellationToken.None);
            }
        }
        finally
        {
            await CloseAsync(socket, WebSocketCloseStatus.NormalClosure);
        }
    }

    private async Task StreamFramesAsync(
        WebSocket socket,
        ScreenStreamGrant grant,
        ScreenDisplay display,
        ScreenStreamProfile initialProfile,
        CancellationToken cancellationToken)
    {
        var frameIntervalTicks = Math.Max(
            1L,
            Stopwatch.Frequency / grant.TargetFps);
        var nextFrameDeadline = Stopwatch.GetTimestamp();
        var profile = initialProfile;
        var statsStarted = Stopwatch.GetTimestamp();
        var frames = 0;
        long bytes = 0;

        while (
            socket.State == WebSocketState.Open
            && !cancellationToken.IsCancellationRequested)
        {
            var frame = _screenCapture.CaptureStreamJpeg(display, profile);
            await socket.SendAsync(
                frame.Bytes.AsMemory(),
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
            frames += 1;
            bytes += frame.Bytes.Length;

            var statsElapsed = Stopwatch.GetElapsedTime(statsStarted);
            if (statsElapsed >= TimeSpan.FromSeconds(1))
            {
                var sourceFps = frames / statsElapsed.TotalSeconds;
                if (sourceFps < grant.TargetFps * 0.88)
                {
                    profile = profile.Degrade();
                }

                await SendJsonAsync(
                    socket,
                    new
                    {
                        type = "stats",
                        sourceFps = Math.Round(
                            sourceFps,
                            1),
                        megabitsPerSecond = Math.Round(
                            bytes * 8d / statsElapsed.TotalSeconds / 1_000_000d,
                            1),
                        frame.Width,
                        frame.Height,
                        profile = profile.Name,
                    },
                    cancellationToken);
                frames = 0;
                bytes = 0;
                statsStarted = Stopwatch.GetTimestamp();
            }

            nextFrameDeadline += frameIntervalTicks;
            var now = Stopwatch.GetTimestamp();
            var remainingTicks = nextFrameDeadline - now;
            if (remainingTicks > 0)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        remainingTicks / (double)Stopwatch.Frequency),
                    cancellationToken);
            }
            else
            {
                nextFrameDeadline = now;
                await Task.Yield();
            }
        }
    }

    private static async Task SendJsonAsync(
        WebSocket socket,
        object payload,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(payload, JsonOptions));
        await socket.SendAsync(
            bytes.AsMemory(),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private static async Task CloseAsync(
        WebSocket socket,
        WebSocketCloseStatus status)
    {
        if (socket.State is not (
            WebSocketState.Open or WebSocketState.CloseReceived))
        {
            return;
        }

        try
        {
            await socket.CloseAsync(
                status,
                "Nexus Bildschirmstream beendet.",
                CancellationToken.None);
        }
        catch (WebSocketException)
        {
            // The client already disappeared.
        }
    }
}
