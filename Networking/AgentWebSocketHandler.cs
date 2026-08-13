using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;
using NexusControl.Agent.Windows;

namespace NexusControl.Agent.Networking;

internal sealed class AgentWebSocketHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly DeviceStore _deviceStore;
    private readonly ActivityLogService _activityLog;
    private readonly TelemetryService _telemetry;
    private readonly WindowsMediaSessionService _mediaSessions;
    private readonly ScreenCaptureService _screenCapture;
    private readonly WindowsController _windows;
    private readonly AgentOptions _options;

    public AgentWebSocketHandler(
        DeviceStore deviceStore,
        ActivityLogService activityLog,
        TelemetryService telemetry,
        WindowsMediaSessionService mediaSessions,
        ScreenCaptureService screenCapture,
        WindowsController windows,
        IOptions<AgentOptions> options)
    {
        _deviceStore = deviceStore;
        _activityLog = activityLog;
        _telemetry = telemetry;
        _mediaSessions = mediaSessions;
        _screenCapture = screenCapture;
        _windows = windows;
        _options = options.Value;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var requestedProtocols = context.WebSockets.WebSocketRequestedProtocols;
        var protocol = requestedProtocols.Contains(
            "nexus-control-v1",
            StringComparer.Ordinal)
            ? "nexus-control-v1"
            : null;
        using var socket = await context.WebSockets.AcceptWebSocketAsync(protocol);
        using var sendLock = new SemaphoreSlim(1, 1);
        using var sessionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        DeviceAuditIdentity? sessionIdentity = null;
        var sessionAuthenticated = false;

        try
        {
            var authentication = await ReceiveEnvelopeAsync(
                socket,
                sessionCancellation.Token);
            if (authentication is null
                || authentication.Type != "session.authenticate"
                || !TimestampIsValid(authentication.Timestamp))
            {
                var rejectedIdentity = _deviceStore.GetAuditIdentity(
                    authentication?.DeviceId);
                _activityLog.Record(
                    rejectedIdentity.DeviceName,
                    rejectedIdentity.Platform,
                    "connection.attempt",
                    ActivityLogResult.Rejected);
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "session.rejected",
                    authentication?.DeviceId ?? "",
                    new { message = "Ungültige Anmeldung." },
                    sessionCancellation.Token);
                await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation);
                return;
            }

            var authPayload =
                authentication.Payload.Deserialize<AuthenticationPayload>(JsonOptions);
            if (authPayload is null
                || !_deviceStore.IsAuthorized(
                    authentication.DeviceId,
                    authPayload.SessionToken))
            {
                var rejectedIdentity = _deviceStore.GetAuditIdentity(
                    authentication.DeviceId);
                _activityLog.Record(
                    rejectedIdentity.DeviceName,
                    rejectedIdentity.Platform,
                    "connection.attempt",
                    ActivityLogResult.Rejected);
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "session.rejected",
                    authentication.DeviceId,
                    new { message = "Gerätefreigabe wurde abgelehnt." },
                    sessionCancellation.Token);
                await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation);
                return;
            }

            sessionIdentity = _deviceStore.GetAuditIdentity(
                authentication.DeviceId);
            sessionAuthenticated = true;
            RecordActivity(
                sessionIdentity,
                "connection.established",
                ActivityLogResult.Success);

            await SendEnvelopeAsync(
                socket,
                sendLock,
                "session.authenticated",
                authentication.DeviceId,
                new { accepted = true },
                sessionCancellation.Token);

            var telemetryTask = SendTelemetryLoopAsync(
                socket,
                sendLock,
                authentication.DeviceId,
                authPayload.SessionToken,
                sessionCancellation.Token);
            var mediaTask = SendMediaLoopAsync(
                socket,
                sendLock,
                authentication.DeviceId,
                sessionCancellation.Token);
            var heartbeatTask = SendHeartbeatLoopAsync(
                socket,
                sendLock,
                authentication.DeviceId,
                sessionCancellation.Token);
            var screenState = new ScreenStreamState();
            var screenTask = SendScreenLoopAsync(
                socket,
                sendLock,
                authentication.DeviceId,
                screenState,
                sessionCancellation.Token);
            var recentCommands = new Queue<DateTimeOffset>();

            while (
                socket.State == WebSocketState.Open
                && !sessionCancellation.IsCancellationRequested)
            {
                var envelope = await ReceiveEnvelopeAsync(
                    socket,
                    sessionCancellation.Token);
                if (envelope is null)
                {
                    break;
                }

                if (envelope.Type != "command.request"
                    || envelope.DeviceId != authentication.DeviceId
                    || !TimestampIsValid(envelope.Timestamp))
                {
                    continue;
                }

                var command =
                    envelope.Payload.Deserialize<CommandRequest>(JsonOptions);
                if (command is null || string.IsNullOrWhiteSpace(command.Command))
                {
                    RecordActivity(
                        sessionIdentity,
                        "Ungültiger Befehl",
                        ActivityLogResult.Rejected);
                    await SendCommandResultAsync(
                        socket,
                        sendLock,
                        authentication.DeviceId,
                        envelope.MessageId,
                        false,
                        "failed",
                        "Ungültiger Befehl.",
                        "invalid_command",
                        sessionCancellation.Token);
                    continue;
                }

                var requiredPermission =
                    DevicePermissionPolicy.ForCommand(command.Command);
                if (!_deviceStore.HasPermission(
                        authentication.DeviceId,
                        requiredPermission))
                {
                    RecordCommand(
                        sessionIdentity,
                        command.Command,
                        ActivityLogResult.Rejected);
                    await SendCommandResultAsync(
                        socket,
                        sendLock,
                        authentication.DeviceId,
                        envelope.MessageId,
                        false,
                        "rejected",
                        "Dieser Befehl ist für das gekoppelte Gerät nicht freigegeben.",
                        "permission_denied",
                        sessionCancellation.Token);
                    continue;
                }

                var isRealtimePointerCommand =
                    command.ExpectResult is false
                    && command.Command is
                        "input.pointerMove" or "input.pointerScroll";
                if (!isRealtimePointerCommand)
                {
                    var now = DateTimeOffset.UtcNow;
                    while (recentCommands.TryPeek(out var oldest)
                        && now - oldest > TimeSpan.FromSeconds(
                            _options.CommandWindowSeconds))
                    {
                        recentCommands.Dequeue();
                    }

                    if (recentCommands.Count >= _options.MaximumCommandsPerWindow)
                    {
                        RecordCommand(
                            sessionIdentity,
                            command.Command,
                            ActivityLogResult.Rejected);
                        await SendCommandResultAsync(
                            socket,
                            sendLock,
                            authentication.DeviceId,
                            envelope.MessageId,
                            false,
                            "failed",
                            "Zu viele Befehle. Bitte kurz warten.",
                            "rate_limited",
                            sessionCancellation.Token);
                        continue;
                    }
                    recentCommands.Enqueue(now);
                }

                await ExecuteCommandAsync(
                    socket,
                    sendLock,
                    authentication.DeviceId,
                    envelope.MessageId,
                    command,
                    screenState,
                    sessionIdentity,
                    sessionCancellation.Token);
            }

            sessionCancellation.Cancel();
            await IgnoreCancellationAsync(Task.WhenAll(
                telemetryTask,
                mediaTask,
                heartbeatTask,
                screenTask));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown or disconnected phone.
        }
        catch (WebSocketException)
        {
            // The client disappeared without a close frame.
        }
        finally
        {
            sessionCancellation.Cancel();
            await CloseAsync(socket, WebSocketCloseStatus.NormalClosure);
            if (sessionAuthenticated && sessionIdentity is not null)
            {
                RecordActivity(
                    sessionIdentity,
                    "connection.disconnected",
                    ActivityLogResult.Information);
            }
        }
    }

    private async Task ExecuteCommandAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        string requestMessageId,
        CommandRequest command,
        ScreenStreamState screenState,
        DeviceAuditIdentity? sessionIdentity,
        CancellationToken cancellationToken)
    {
        var expectResult = command.ExpectResult is not false;
        try
        {
            if (command.Command == "screen.start")
            {
                _screenCapture.CaptureJpeg();
                screenState.IsEnabled = true;
                await SendCommandResultAsync(
                    socket,
                    sendLock,
                    deviceId,
                    requestMessageId,
                    true,
                    "completed",
                    "Bildschirmübertragung wurde gestartet.",
                    null,
                    cancellationToken);
                RecordCommand(
                    sessionIdentity,
                    command.Command,
                    ActivityLogResult.Success);
                return;
            }

            if (command.Command == "screen.stop")
            {
                screenState.IsEnabled = false;
                await SendCommandResultAsync(
                    socket,
                    sendLock,
                    deviceId,
                    requestMessageId,
                    true,
                    "completed",
                    "Bildschirmübertragung wurde beendet.",
                    null,
                    cancellationToken);
                RecordCommand(
                    sessionIdentity,
                    command.Command,
                    ActivityLogResult.Success);
                return;
            }

            if (_windows.IsPowerCommand(command.Command))
            {
                await SendCommandResultAsync(
                    socket,
                    sendLock,
                    deviceId,
                    requestMessageId,
                    true,
                    "completed",
                    "Befehl wurde von Windows angenommen.",
                    null,
                    cancellationToken);
                RecordCommand(
                    sessionIdentity,
                    command.Command,
                    ActivityLogResult.Success);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(650);
                    try
                    {
                        if (_deviceStore.HasPermission(
                                deviceId,
                                DevicePermission.Power))
                        {
                            _windows.Execute(command.Command, command.Parameters);
                        }
                    }
                    catch (Exception error)
                    {
                        RecordCommand(
                            sessionIdentity,
                            command.Command,
                            ActivityLogResult.Failed);
                        System.Diagnostics.Debug.WriteLine(
                            $"Power command failed: {error.Message}");
                    }
                });
                return;
            }

            var message = _windows.Execute(command.Command, command.Parameters);
            RecordCommand(
                sessionIdentity,
                command.Command,
                ActivityLogResult.Success);
            if (expectResult)
            {
                await SendCommandResultAsync(
                    socket,
                    sendLock,
                    deviceId,
                    requestMessageId,
                    true,
                    "completed",
                    message,
                    null,
                    cancellationToken);
            }
        }
        catch (Exception error)
        {
            RecordCommand(
                sessionIdentity,
                command.Command,
                ActivityLogResult.Failed);
            if (expectResult)
            {
                await SendCommandResultAsync(
                    socket,
                    sendLock,
                    deviceId,
                    requestMessageId,
                    false,
                    "failed",
                    error.Message,
                    "command_failed",
                    cancellationToken);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Transient command failed: {error.Message}");
            }
        }
    }

    private void RecordCommand(
        DeviceAuditIdentity? identity,
        string command,
        ActivityLogResult result)
    {
        if (!ActivityLogService.ShouldRecordCommand(command))
        {
            return;
        }

        RecordActivity(
            identity,
            ActivityLogService.CommandAction(command),
            result);
    }

    private void RecordActivity(
        DeviceAuditIdentity? identity,
        string action,
        ActivityLogResult result)
    {
        identity ??= DeviceAuditIdentity.Unknown;
        _activityLog.Record(
            identity.DeviceName,
            identity.Platform,
            action,
            result);
    }

    private async Task SendScreenLoopAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        ScreenStreamState state,
        CancellationToken cancellationToken)
    {
        while (
            socket.State == WebSocketState.Open
            && !cancellationToken.IsCancellationRequested)
        {
            if (!state.IsEnabled)
            {
                await Task.Delay(150, cancellationToken);
                continue;
            }

            if (!_deviceStore.HasPermission(
                    deviceId,
                    DevicePermission.Screen))
            {
                state.IsEnabled = false;
                RecordActivity(
                    _deviceStore.GetAuditIdentity(deviceId),
                    "command.screen.stop",
                    ActivityLogResult.Rejected);
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "screen.error",
                    deviceId,
                    new
                    {
                        message =
                            "Die Bildschirmübertragung wurde für dieses Gerät deaktiviert.",
                    },
                    cancellationToken);
                continue;
            }

            try
            {
                var frame = _screenCapture.Capture();
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "screen.frame",
                    deviceId,
                    frame,
                    cancellationToken);
                await Task.Delay(700, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                state.IsEnabled = false;
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "screen.error",
                    deviceId,
                    new
                    {
                        message =
                            $"Bildschirm konnte nicht gelesen werden: {error.Message}",
                    },
                    cancellationToken);
            }
        }
    }

    private async Task SendTelemetryLoopAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        while (
            socket.State == WebSocketState.Open
            && !cancellationToken.IsCancellationRequested)
        {
            if (!_deviceStore.IsAuthorized(deviceId, sessionToken))
            {
                RecordActivity(
                    _deviceStore.GetAuditIdentity(deviceId),
                    "connection.revoked",
                    ActivityLogResult.Rejected);
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "session.rejected",
                    deviceId,
                    new { message = "Gerätefreigabe wurde widerrufen." },
                    cancellationToken);
                await CloseAsync(
                    socket,
                    WebSocketCloseStatus.PolicyViolation);
                return;
            }

            try
            {
                var snapshot = _telemetry.Capture();
                if (!_deviceStore.HasPermission(
                        deviceId,
                        DevicePermission.Processes))
                {
                    snapshot = snapshot with
                    {
                        Processes = Array.Empty<ProcessSnapshot>(),
                    };
                }
                if (!_deviceStore.HasPermission(
                        deviceId,
                        DevicePermission.Media))
                {
                    snapshot = snapshot with
                    {
                        Audio = snapshot.Audio with
                        {
                            VolumePercent = 0,
                            IsMuted = false,
                            Available = false,
                        },
                        MediaSessions = Array.Empty<MediaSessionSnapshot>(),
                    };
                }
                await SendEnvelopeAsync(
                    socket,
                    sendLock,
                    "device.snapshot",
                    deviceId,
                    snapshot,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Telemetry capture failed: {error.Message}");
            }
            await Task.Delay(
                TimeSpan.FromMilliseconds(_options.TelemetryIntervalMilliseconds),
                cancellationToken);
        }
    }

    private static async Task SendHeartbeatLoopAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        CancellationToken cancellationToken)
    {
        while (
            socket.State == WebSocketState.Open
            && !cancellationToken.IsCancellationRequested)
        {
            await SendEnvelopeAsync(
                socket,
                sendLock,
                "session.heartbeat",
                deviceId,
                new { sentAt = DateTimeOffset.UtcNow },
                cancellationToken);
            await Task.Delay(
                TimeSpan.FromMilliseconds(1_500),
                cancellationToken);
        }
    }

    private async Task SendMediaLoopAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        CancellationToken cancellationToken)
    {
        while (
            socket.State == WebSocketState.Open
            && !cancellationToken.IsCancellationRequested)
        {
            await SendEnvelopeAsync(
                socket,
                sendLock,
                "media.sessions",
                deviceId,
                new MediaSessionsUpdate(
                    DateTimeOffset.UtcNow,
                    _deviceStore.HasPermission(
                        deviceId,
                        DevicePermission.Media)
                        ? _mediaSessions.GetSnapshot()
                        : Array.Empty<MediaSessionSnapshot>()),
                cancellationToken);
            await Task.Delay(
                TimeSpan.FromMilliseconds(400),
                cancellationToken);
        }
    }

    private static Task SendCommandResultAsync(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string deviceId,
        string requestMessageId,
        bool accepted,
        string status,
        string? message,
        string? errorCode,
        CancellationToken cancellationToken) =>
        SendEnvelopeAsync(
            socket,
            sendLock,
            "command.result",
            deviceId,
            new CommandResult(
                requestMessageId,
                accepted,
                status,
                message,
                errorCode),
            cancellationToken);

    private async Task<ProtocolEnvelope?> ReceiveEnvelopeAsync(
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }
            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new WebSocketException("Only text messages are supported.");
            }

            stream.Write(buffer, 0, result.Count);
            if (stream.Length > _options.MaximumMessageSizeBytes)
            {
                throw new WebSocketException("Message is too large.");
            }
            if (result.EndOfMessage)
            {
                break;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<ProtocolEnvelope>(
                stream.ToArray(),
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task SendEnvelopeAsync<TPayload>(
        WebSocket socket,
        SemaphoreSlim sendLock,
        string type,
        string deviceId,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var envelope = new
        {
            version = 1,
            type,
            messageId = $"agent-{Guid.NewGuid():N}",
            deviceId,
            timestamp = DateTimeOffset.UtcNow,
            payload,
        };
        var bytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(envelope, JsonOptions));

        await sendLock.WaitAsync(cancellationToken);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }
        }
        finally
        {
            sendLock.Release();
        }
    }

    private bool TimestampIsValid(DateTimeOffset timestamp) =>
        Math.Abs((DateTimeOffset.UtcNow - timestamp).TotalMinutes)
        <= _options.AllowedClockSkewMinutes;

    private static async Task CloseAsync(
        WebSocket socket,
        WebSocketCloseStatus status)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
        {
            return;
        }

        try
        {
            await socket.CloseAsync(status, "Nexus Agent", CancellationToken.None);
        }
        catch
        {
            // Nothing else to do when a peer already disappeared.
        }
    }

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when the WebSocket closes.
        }
    }

    private sealed class ScreenStreamState
    {
        private int _enabled;

        public bool IsEnabled
        {
            get => Volatile.Read(ref _enabled) == 1;
            set => Volatile.Write(ref _enabled, value ? 1 : 0);
        }
    }
}
