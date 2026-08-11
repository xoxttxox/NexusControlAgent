namespace NexusControl.Agent.Configuration;

internal sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public int Port { get; set; } = 5188;

    public int PairingCodeLifetimeMinutes { get; set; } = 10;

    public int MaximumPairingAttempts { get; set; } = 8;

    public int TelemetryIntervalMilliseconds { get; set; } = 2000;

    public int CommandWindowSeconds { get; set; } = 10;

    public int MaximumCommandsPerWindow { get; set; } = 250;

    public int AllowedClockSkewMinutes { get; set; } = 2;

    public int MaximumMessageSizeBytes { get; set; } = 65_536;

    public int PushMonitoringIntervalSeconds { get; set; } = 15;

    public double PushTemperatureThresholdCelsius { get; set; } = 85;

    public double PushTemperatureResetCelsius { get; set; } = 80;
}
