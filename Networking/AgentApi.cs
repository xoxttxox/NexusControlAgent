using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Models;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;

namespace NexusControl.Agent.Networking;

/// <summary>
/// Registriert Middleware, HTTP-Endpunkte und WebSocket-Routen des Agents.
/// </summary>
internal static class AgentApi
{
    public static void MapEndpoints(
        WebApplication app,
        PairingService pairing,
        TelemetryService telemetry,
        ActivityLogService activityLog,
        AgentOptions agentOptions)
    {
        app.Use(async (context, next) =>
        {
            if (!NetworkUtilities.IsPrivateOrLoopback(context.Connection.RemoteIpAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message =
                        "Nexus Agent akzeptiert nur Geräte aus dem privaten Netzwerk oder dem eigenen Tailscale-Netz.",
                });
                return;
            }

            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Headers"] =
                "Content-Type, Authorization, X-Nexus-Device-Id";
            context.Response.Headers["Access-Control-Allow-Methods"] =
                "GET, POST, DELETE, OPTIONS";
            context.Response.Headers["Access-Control-Expose-Headers"] =
                "X-Nexus-Screen-Width, X-Nexus-Screen-Height, X-Nexus-Screen-Captured-At";
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            await next();
        });

        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(15),
        });

        app.MapGet("/", () => Results.Ok(new
        {
            name = "Nexus Control Agent",
            version = TelemetryService.AgentVersion,
            state = "ready",
        }));

        app.MapGet("/api/status", () => Results.Ok(new
        {
            wakeOnLan = NetworkUtilities.GetWakeOnLanInfo(),
            name = Environment.MachineName,
            version = TelemetryService.AgentVersion,
            addresses = NetworkUtilities
                .GetReachableIPv4Addresses()
                .Select(address => $"{address}:{agentOptions.Port}"),
            localAddresses = NetworkUtilities.GetPrivateIPv4Addresses(),
            tailscaleAddresses = NetworkUtilities.GetTailscaleIPv4Addresses(),
            remoteAccessReady =
                NetworkUtilities.GetTailscaleIPv4Addresses().Count > 0,
        }));

        app.MapPost("/api/pair", (PairRequest request, HttpContext context) =>
        {
            if (!pairing.TryPair(
                    request.Code,
                    request.DeviceName,
                    request.Platform,
                    out var credentials,
                    out var error)
                || credentials is null)
            {
                return Results.Json(
                    new { message = error },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var socketScheme = context.Request.IsHttps ? "wss" : "ws";
            var webSocketUrl = $"{socketScheme}://{context.Request.Host}/ws";
            return Results.Ok(new PairResponse(
                credentials.DeviceId,
                credentials.SessionToken,
                webSocketUrl,
                telemetry.Capture()));
        });

        app.MapGet(
            "/api/diagnostics",
            async Task<IResult> (
                HttpContext context,
                DeviceStore devices,
                ScreenCaptureService screenCapture,
                HardwareMonitorService hardwareMonitor,
                SessionUptimeService uptime) =>
            {
                if (!TryAuthorize(context, devices, out var currentDeviceId))
                {
                    return Results.Unauthorized();
                }

                var remoteAddress = context.Connection.RemoteIpAddress;
                var throughTailscale =
                    NetworkUtilities.IsTailscaleAddress(remoteAddress);
                var tailscaleAddress = NetworkUtilities
                    .GetTailscaleIPv4Addresses()
                    .FirstOrDefault();
                var wakeOnLan = NetworkUtilities.GetWakeOnLanInfo();
                var sensors = hardwareMonitor.Diagnose();
                return Results.Ok(new
                {
                    computerName = Environment.MachineName,
                    agentVersion = TelemetryService.AgentVersion,
                    serverTime = DateTimeOffset.UtcNow,
                    uptimeSeconds = uptime.GetUptimeSeconds(),
                    localIPAddress = NetworkUtilities.GetPreferredIPv4Address(),
                    tailscaleIPAddress = tailscaleAddress,
                    remoteAccessReady = tailscaleAddress is not null,
                    remoteIPAddress =
                        remoteAddress?.ToString() ?? "Unbekannt",
                    connectionMode = throughTailscale
                        ? context.Request.IsHttps ? "tailscaleTls" : "tailscale"
                        : context.Request.IsHttps ? "localTls" : "local",
                    tlsEnabled = context.Request.IsHttps,
                    trustedDeviceCount = devices.Count,
                    wakeOnLan = new
                    {
                        wakeOnLan.Available,
                        wakeOnLan.MacAddress,
                        wakeOnLan.BroadcastAddress,
                        wakeOnLan.Port,
                        wakeOnLan.Message,
                    },
                    sensors = new
                    {
                        hardwareMonitor = sensors.MonitorAvailable,
                        cpuTemperature = sensors.CpuTemperatureAvailable,
                        gpuTemperature = sensors.GpuTemperatureAvailable,
                        sensors.Message,
                    },
                    services = new
                    {
                        webSocket = true,
                        telemetry = true,
                        screenCapture = ScreenCaptureIsReady(screenCapture),
                        fileTransfer = true,
                        wakeOnLan = wakeOnLan.Available,
                        pushNotifications =
                            devices.HasPushSubscription(currentDeviceId),
                        cpuTemperature = sensors.CpuTemperatureAvailable,
                        gpuTemperature = sensors.GpuTemperatureAvailable,
                    },
                });
            });

        app.MapGet(
            "/api/trusted-devices",
            (
                HttpContext context,
                DeviceStore devices) =>
            {
                if (!TryAuthorize(context, devices, out var currentDeviceId))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new
                {
                    devices = devices.ListDevices(currentDeviceId),
                });
            });

        app.MapDelete(
            "/api/trusted-devices/{deviceId}",
            (
                string deviceId,
                HttpContext context,
                DeviceStore devices) =>
            {
                if (!TryAuthorize(context, devices, out var currentDeviceId))
                {
                    return Results.Unauthorized();
                }
                var requestingIdentity = devices.GetAuditIdentity(
                    currentDeviceId);
                if (!devices.RemoveDevice(deviceId))
                {
                    activityLog.Record(
                        requestingIdentity.DeviceName,
                        requestingIdentity.Platform,
                        "device.permission.remove",
                        ActivityLogResult.Failed);
                    return Results.NotFound(new
                    {
                        message = "Die Gerätefreigabe wurde nicht gefunden.",
                    });
                }

                activityLog.Record(
                    requestingIdentity.DeviceName,
                    requestingIdentity.Platform,
                    "device.permission.removed",
                    ActivityLogResult.Success);

                return Results.Ok(new
                {
                    revoked = true,
                    revokedCurrent = string.Equals(
                        currentDeviceId,
                        deviceId,
                        StringComparison.Ordinal),
                });
            });

        app.MapPost(
            "/api/push-subscription",
            async Task<IResult> (
                PushSubscriptionRequest request,
                HttpContext context,
                DeviceStore devices,
                PushNotificationService pushNotifications) =>
            {
                if (!TryAuthorize(context, devices, out var currentDeviceId))
                {
                    return Results.Unauthorized();
                }

                var token = request.ExpoPushToken?.Trim() ?? "";
                if (!IsExpoPushToken(token))
                {
                    return Results.BadRequest(new
                    {
                        message =
                            "Das Expo-Push-Token ist ungültig. Nutze einen iOS-Development-Build mit eingerichteten Push-Zugangsdaten.",
                    });
                }
                var alreadyRegistered = devices.IsPushTokenRegistered(
                    currentDeviceId,
                    token);
                if (!devices.SetPushToken(currentDeviceId, token))
                {
                    return Results.NotFound(new
                    {
                        message = "Die Gerätefreigabe wurde nicht gefunden.",
                    });
                }
                if (!alreadyRegistered)
                {
                    try
                    {
                        await pushNotifications.SendRegistrationTestAsync(
                            token,
                            context.RequestAborted);
                    }
                    catch (Exception error)
                    {
                        devices.DisablePushNotifications(currentDeviceId);
                        return Results.Json(
                            new
                            {
                                message =
                                    $"Push-Test fehlgeschlagen: {error.Message} Prüfe EAS-Push-Zugangsdaten und Internetzugang des PCs.",
                            },
                            statusCode: StatusCodes.Status502BadGateway);
                    }
                }

                return Results.Ok(new
                {
                    registered = true,
                    verified = true,
                    monitoring = new
                    {
                        cpuTemperature = true,
                        gpuTemperature = true,
                        thresholdCelsius =
                            agentOptions.PushTemperatureThresholdCelsius,
                    },
                });
            });

        app.MapDelete(
            "/api/push-subscription",
            (
                HttpContext context,
                DeviceStore devices) =>
            {
                if (!TryAuthorize(context, devices, out var currentDeviceId))
                {
                    return Results.Unauthorized();
                }

                if (!devices.DisablePushNotifications(currentDeviceId))
                {
                    return Results.NotFound(new
                    {
                        message = "Die Gerätefreigabe wurde nicht gefunden.",
                    });
                }

                return Results.Ok(new { registered = false });
            });

        app.MapGet(
            "/api/screen/displays",
            (
                HttpContext context,
                DeviceStore devices,
                ScreenCaptureService screenCapture) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Screen,
                    out _);
                if (authorizationError is not null)
                {
                    return authorizationError;
                }

                try
                {
                    return Results.Ok(
                        screenCapture.GetDisplays().Select(display => new
                        {
                            display.Id,
                            display.Name,
                            display.Width,
                            display.Height,
                            display.IsPrimary,
                        }));
                }
                catch (Exception error)
                {
                    return Results.Problem(
                        title: "Bildschirme konnten nicht ermittelt werden",
                        detail: error.Message,
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            });

        app.MapPost(
            "/api/screen/session",
            (
                ScreenStreamSessionRequest request,
                HttpContext context,
                DeviceStore devices,
                ScreenCaptureService screenCapture,
                ScreenStreamTicketService tickets) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Screen,
                    out var currentDeviceId,
                    activityLog,
                    "command.screen.start");
                if (authorizationError is not null)
                {
                    return authorizationError;
                }

                var displays = screenCapture.GetDisplays();
                if (!displays.Any(display => display.Id == request.DisplayId))
                {
                    RecordActivity(
                        devices,
                        activityLog,
                        currentDeviceId,
                        "command.screen.start",
                        ActivityLogResult.Failed);
                    return Results.BadRequest(new
                    {
                        message = "Der ausgewählte Bildschirm ist nicht verfügbar.",
                    });
                }

                var targetFps = request.TargetFps <= 0 ? 60 : request.TargetFps;
                var ticket = tickets.Create(
                    currentDeviceId,
                    request.DisplayId,
                    targetFps,
                    request.Profile);
                var socketScheme = context.Request.IsHttps ? "wss" : "ws";
                var socketUrl =
                    $"{socketScheme}://{context.Request.Host}/ws/screen" +
                    $"?ticket={Uri.EscapeDataString(ticket.Value)}";

                RecordActivity(
                    devices,
                    activityLog,
                    currentDeviceId,
                    "command.screen.start",
                    ActivityLogResult.Success);

                return Results.Ok(new
                {
                    webSocketUrl = socketUrl,
                    ticket.ExpiresAt,
                    targetFps = Math.Clamp(targetFps, 15, 60),
                    profile = ScreenStreamProfile.Resolve(request.Profile).Name,
                });
            });

        app.MapGet(
            "/api/screen/frame",
            (
                HttpContext context,
                DeviceStore devices,
                ScreenCaptureService screenCapture) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Screen,
                    out _);
                if (authorizationError is not null)
                {
                    return authorizationError;
                }

                try
                {
                    var smooth = string.Equals(
                        context.Request.Query["mode"].ToString(),
                        "smooth",
                        StringComparison.OrdinalIgnoreCase);
                    var displayIndex = int.TryParse(
                        context.Request.Query["display"].ToString(),
                        out var requestedDisplay)
                        ? requestedDisplay
                        : 0;
                    var frame = screenCapture.CaptureJpeg(smooth, displayIndex);
                    context.Response.Headers.CacheControl =
                        "no-store, no-cache, must-revalidate";
                    context.Response.Headers["X-Nexus-Screen-Width"] =
                        frame.Width.ToString();
                    context.Response.Headers["X-Nexus-Screen-Height"] =
                        frame.Height.ToString();
                    context.Response.Headers["X-Nexus-Screen-Captured-At"] =
                        frame.CapturedAt.ToString("O");
                    return Results.Bytes(frame.Bytes, "image/jpeg");
                }
                catch (Exception error)
                {
                    return Results.Problem(
                        title: "Bildschirmübertragung nicht verfügbar",
                        detail:
                            $"{error.Message} Starte den Agent im angemeldeten Windows-Benutzerkonto. Sperrbildschirm und UAC-Dialoge können nicht übertragen werden.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            });

        app.MapGet(
            "/api/files",
            (
                HttpContext context,
                DeviceStore devices,
                FileTransferService files) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Files,
                    out _);
                if (authorizationError is not null)
                {
                    return authorizationError;
                }

                return Results.Ok(new
                {
                    directory = files.SharedDirectory,
                    files = files.ListFiles(),
                });
            });

        app.MapPost(
            "/api/files/upload",
            async (
                HttpContext context,
                DeviceStore devices,
                FileTransferService files) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Files,
                    out var currentDeviceId,
                    activityLog,
                    "file.upload");
                if (authorizationError is not null)
                {
                    return authorizationError;
                }
                if (!context.Request.HasFormContentType)
                {
                    RecordActivity(
                        devices,
                        activityLog,
                        currentDeviceId,
                        "file.upload",
                        ActivityLogResult.Failed);
                    return Results.BadRequest(new
                    {
                        message = "Es wurde keine Datei übermittelt.",
                    });
                }

                try
                {
                    var form = await context.Request.ReadFormAsync(
                        context.RequestAborted);
                    var upload = form.Files.GetFile("file");
                    if (upload is null)
                    {
                        RecordActivity(
                            devices,
                            activityLog,
                            currentDeviceId,
                            "file.upload",
                            ActivityLogResult.Failed);
                        return Results.BadRequest(new
                        {
                            message = "Das Upload-Feld „file“ fehlt.",
                        });
                    }

                    var requestedName = context.Request.Query["name"].ToString();
                    await using var input = upload.OpenReadStream();
                    var saved = await files.SaveAsync(
                        input,
                        string.IsNullOrWhiteSpace(requestedName)
                            ? upload.FileName
                            : requestedName,
                        upload.Length,
                        context.RequestAborted);
                    RecordActivity(
                        devices,
                        activityLog,
                        currentDeviceId,
                        "file.upload",
                        ActivityLogResult.Success);
                    return Results.Ok(saved);
                }
                catch (Exception error) when (
                    error is InvalidOperationException or IOException)
                {
                    RecordActivity(
                        devices,
                        activityLog,
                        currentDeviceId,
                        "file.upload",
                        ActivityLogResult.Failed);
                    return Results.BadRequest(new { message = error.Message });
                }
            });

        app.MapGet(
            "/api/files/download",
            (
                HttpContext context,
                DeviceStore devices,
                FileTransferService files) =>
            {
                var authorizationError = GetAuthorizationError(
                    context,
                    devices,
                    DevicePermission.Files,
                    out var currentDeviceId,
                    activityLog,
                    "file.download");
                if (authorizationError is not null)
                {
                    return authorizationError;
                }

                var resolved = files.Resolve(context.Request.Query["name"].ToString());
                if (resolved is null)
                {
                    RecordActivity(
                        devices,
                        activityLog,
                        currentDeviceId,
                        "file.download",
                        ActivityLogResult.Failed);
                    return Results.NotFound(new
                    {
                        message = "Die Datei wurde im Nexus-Ordner nicht gefunden.",
                    });
                }

                RecordActivity(
                    devices,
                    activityLog,
                    currentDeviceId,
                    "file.download",
                    ActivityLogResult.Success);

                return Results.File(
                    resolved.Value.Path,
                    resolved.Value.ContentType,
                    resolved.Value.Name,
                    enableRangeProcessing: true);
            });

        app.Map("/ws/screen", async context =>
        {
            var handler = context.RequestServices
                .GetRequiredService<ScreenStreamWebSocketHandler>();
            await handler.HandleAsync(context);
        });

        app.Map("/ws", async context =>
        {
            var handler = context.RequestServices
                .GetRequiredService<AgentWebSocketHandler>();
            await handler.HandleAsync(context);
        });

        static bool TryAuthorize(
            HttpContext context,
            DeviceStore devices,
            out string deviceId)
        {
            deviceId = context.Request.Headers["X-Nexus-Device-Id"].ToString();
            var authorization = context.Request.Headers["Authorization"].ToString();
            const string bearerPrefix = "Bearer ";
            if (!authorization.StartsWith(
                    bearerPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var token = authorization[bearerPrefix.Length..].Trim();
            return devices.IsAuthorized(deviceId, token);
        }

        static IResult? GetAuthorizationError(
            HttpContext context,
            DeviceStore devices,
            DevicePermission requiredPermission,
            out string deviceId,
            ActivityLogService? activityLog = null,
            string? action = null)
        {
            if (!TryAuthorize(context, devices, out deviceId))
            {
                return Results.Unauthorized();
            }
            if (!devices.HasPermission(deviceId, requiredPermission))
            {
                if (activityLog is not null && !string.IsNullOrWhiteSpace(action))
                {
                    RecordActivity(
                        devices,
                        activityLog,
                        deviceId,
                        action,
                        ActivityLogResult.Rejected);
                }
                return Results.Json(
                    new
                    {
                        message =
                            "Diese Funktion ist für das gekoppelte Gerät nicht freigegeben.",
                        errorCode = "permission_denied",
                    },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return null;
        }

        static void RecordActivity(
            DeviceStore devices,
            ActivityLogService activityLog,
            string deviceId,
            string action,
            ActivityLogResult result)
        {
            var identity = devices.GetAuditIdentity(deviceId);
            activityLog.Record(
                identity.DeviceName,
                identity.Platform,
                action,
                result);
        }

        static bool ScreenCaptureIsReady(ScreenCaptureService screenCapture)
        {
            try
            {
                _ = screenCapture.CaptureJpeg();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool IsExpoPushToken(string token) =>
            token.Length is > 20 and <= 256
            && token.EndsWith(']')
            && (
                token.StartsWith("ExpoPushToken[", StringComparison.Ordinal)
                || token.StartsWith(
                    "ExponentPushToken[",
                    StringComparison.Ordinal));
    }
}
