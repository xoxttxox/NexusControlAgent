using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusControl.Agent.Application;
using NexusControl.Agent.Configuration;
using NexusControl.Agent.Localization;
using NexusControl.Agent.Networking;
using NexusControl.Agent.Pairing;
using NexusControl.Agent.Services;
using NexusControl.Agent.UI;
using NexusControl.Agent.Windows;

namespace NexusControl.Agent;

internal static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        // Muss vor jedem möglichen WinForms-Fenster gesetzt werden, also auch
        // vor Hinweisen für eine bereits laufende Instanz oder Startfehlern.
        // Die DPI-Einstellung gehört bei modernem WinForms nicht ins Manifest.
        System.Windows.Forms.Application.SetHighDpiMode(
            System.Windows.Forms.HighDpiMode.PerMonitorV2);
        LocalizationService.Initialize();

        var autoStartCommand = args.FirstOrDefault(argument =>
            argument.StartsWith(
                "--configure-autostart=",
                StringComparison.OrdinalIgnoreCase));
        if (autoStartCommand is not null)
        {
            var mode = autoStartCommand.Split('=', 2).ElementAtOrDefault(1);
            var autoStartService = new AutoStartService();
            var uninstalling = string.Equals(
                mode,
                "uninstall",
                StringComparison.OrdinalIgnoreCase);
            var result = uninstalling
                ? autoStartService.RemoveForUninstall()
                : autoStartService.SetEnabled(
                    string.Equals(mode, "enable", StringComparison.OrdinalIgnoreCase));

            Environment.ExitCode = result.Succeeded ? 0 : 1;
            return;
        }

        // The MSI only records the selected autostart option. Creating the
        // highest-privilege interactive task here avoids turning a Task Scheduler
        // problem into a failed/rolled-back MSI installation.
        _ = new AutoStartService().ApplyInstallerPreference();

        var startInTray = args.Any(argument =>
            string.Equals(argument, "--tray", StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                argument,
                "--minimized",
                StringComparison.OrdinalIgnoreCase));

        using var singleInstance = new Mutex(
            initiallyOwned: true,
            name: @"Local\NexusControlAgent.Desktop",
            createdNew: out var isFirstInstance);
        if (!isFirstInstance)
        {
            // A scheduled tray start must stay completely silent. A manual launch still
            // explains where the already-running agent can be found.
            if (!startInTray)
            {
                NexusDialog.ShowStandalone(
                    LocalizationService.Text("Program.AlreadyRunning"),
                    "Nexus Control Agent",
                    NexusDialogKind.Information);
            }

            return;
        }

        var hostArguments = args
            .Where(argument =>
                !string.Equals(argument, "--tray", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    argument,
                    "--minimized",
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var builder = WebApplication.CreateBuilder(hostArguments);
        var configuredOptions =
            builder.Configuration
                .GetSection(AgentOptions.SectionName)
                .Get<AgentOptions>()
            ?? new AgentOptions();

        builder.WebHost.UseUrls($"http://0.0.0.0:{configuredOptions.Port}");
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize =
                FileTransferService.MaximumFileSizeBytes + 2 * 1024 * 1024;
        });
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit =
                FileTransferService.MaximumFileSizeBytes + 2 * 1024 * 1024;
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });
        builder.Services
            .AddOptions<AgentOptions>()
            .Bind(builder.Configuration.GetSection(AgentOptions.SectionName))
            .Validate(
                options => options.Port is > 0 and <= 65535,
                LocalizationService.Text("Program.Validation.Port"))
            .Validate(
                options => options.PairingCodeLifetimeMinutes is >= 1 and <= 60,
                LocalizationService.Text("Program.Validation.PairingLifetime"))
            .Validate(
                options => options.MaximumPairingAttempts is >= 1 and <= 20,
                LocalizationService.Text("Program.Validation.PairingAttempts"))
            .Validate(
                options =>
                    options.TelemetryIntervalMilliseconds is >= 250 and <= 60_000,
                LocalizationService.Text("Program.Validation.TelemetryInterval"))
            .Validate(
                options =>
                    options.CommandWindowSeconds > 0
                    && options.MaximumCommandsPerWindow > 0,
                LocalizationService.Text("Program.Validation.CommandLimit"))
            .Validate(
                options => options.AllowedClockSkewMinutes is >= 1 and <= 10,
                LocalizationService.Text("Program.Validation.ClockSkew"))
            .Validate(
                options =>
                    options.MaximumMessageSizeBytes is >= 4096 and <= 1_048_576,
                LocalizationService.Text("Program.Validation.MessageSize"))
            .Validate(
                options =>
                    options.PushMonitoringIntervalSeconds is >= 5 and <= 300,
                LocalizationService.Text("Program.Validation.PushInterval"))
            .Validate(
                options =>
                    options.PushTemperatureThresholdCelsius is >= 50 and <= 120
                    && options.PushTemperatureResetCelsius is >= 40 and <= 115
                    && options.PushTemperatureResetCelsius
                        < options.PushTemperatureThresholdCelsius,
                LocalizationService.Text("Program.Validation.Temperature"))
            .ValidateOnStart();
        builder.Services.AddSingleton<ActivityLogService>();
        builder.Services.AddSingleton<DeviceStore>();
        builder.Services.AddSingleton<PairingService>();
        builder.Services.AddSingleton<HardwareMonitorService>();
        builder.Services.AddSingleton<WindowsAudioService>();
        builder.Services.AddSingleton<SessionUptimeService>();
        builder.Services.AddSingleton<WindowsMediaSessionService>();
        builder.Services.AddHostedService<WindowsMediaSessionService>(serviceProvider =>
            serviceProvider.GetRequiredService<WindowsMediaSessionService>());
        builder.Services.AddSingleton<TelemetryService>();
        builder.Services.AddSingleton<ScreenCaptureService>();
        builder.Services.AddSingleton<ScreenStreamTicketService>();
        builder.Services.AddSingleton<FileTransferService>();
        builder.Services.AddSingleton<WindowsController>();
        builder.Services.AddSingleton<AgentWebSocketHandler>();
        builder.Services.AddSingleton<ScreenStreamWebSocketHandler>();
        builder.Services.AddHttpClient("ExpoPush", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "NexusControlAgent/0.11.5");
        });
        builder.Services.AddSingleton<PushNotificationService>();
        builder.Services.AddHostedService<PushNotificationService>(serviceProvider =>
            serviceProvider.GetRequiredService<PushNotificationService>());
        await using var app = builder.Build();

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var pairing = app.Services.GetRequiredService<PairingService>();
        var telemetry = app.Services.GetRequiredService<TelemetryService>();
        var activityLog = app.Services.GetRequiredService<ActivityLogService>();
        var agentOptions = app.Services
            .GetRequiredService<IOptions<AgentOptions>>()
            .Value;
        var addresses = NetworkUtilities.GetReachableIPv4Addresses();
        pairing.Configure(addresses);

        AgentApi.MapEndpoints(
            app,
            pairing,
            telemetry,
            activityLog,
            agentOptions);

        try
        {
            await app.StartAsync();
        }
        catch (Exception error)
        {
            NexusDialog.ShowStandalone(
                LocalizationService.Format(
                    "Program.ServerStartFailed",
                    Environment.NewLine,
                    error.Message),
                "Nexus Control Agent",
                NexusDialogKind.Error);
            return;
        }

        var desktopExited = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        AgentApplicationContext? desktopContext = null;
        var deviceStore = app.Services.GetRequiredService<DeviceStore>();
        var desktopThread = new Thread(() =>
        {
            try
            {
                using var context = new AgentApplicationContext(
                    pairing,
                    deviceStore,
                    activityLog,
                    agentOptions,
                    startInTray);
                desktopContext = context;
                System.Windows.Forms.Application.Run(context);
                desktopExited.TrySetResult(true);
            }
            catch (Exception error)
            {
                desktopExited.TrySetException(error);
            }
        })
        {
            IsBackground = false,
            Name = "NexusControlAgent.UI",
        };
        desktopThread.SetApartmentState(ApartmentState.STA);

        using var stoppingRegistration =
            app.Lifetime.ApplicationStopping.Register(
                () => desktopContext?.RequestExit());
        desktopThread.Start();

        try
        {
            await desktopExited.Task;
        }
        catch (Exception error)
        {
            NexusDialog.ShowStandalone(
                LocalizationService.Format(
                    "Program.DesktopUnexpectedExit",
                    Environment.NewLine,
                    error.Message),
                "Nexus Control Agent",
                NexusDialogKind.Error);
        }
        finally
        {
            using var shutdownTimeout =
                new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await app.StopAsync(shutdownTimeout.Token);
        }
    }
}
