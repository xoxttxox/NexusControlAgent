using System.Text.Json;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Services;

/// <summary>
/// Stores only whether the local welcome flow has been completed. The chosen
/// language remains owned by
/// <see cref="NexusControl.Agent.Localization.LocalizationService"/>.
/// </summary>
internal sealed class FirstRunService
{
    private const int CurrentSchemaVersion = 1;
    private readonly Lock _gate = new();

    public bool IsCompleted()
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(NexusPaths.FirstRunStatePath))
                {
                    return false;
                }

                var state = JsonSerializer.Deserialize<FirstRunState>(
                    File.ReadAllText(NexusPaths.FirstRunStatePath));
                return state is
                {
                    Completed: true,
                    SchemaVersion: CurrentSchemaVersion,
                };
            }
            catch
            {
                return false;
            }
        }
    }

    public bool MarkCompleted()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(NexusPaths.UserDataDirectory);
                var temporaryPath = $"{NexusPaths.FirstRunStatePath}.tmp";
                var state = new FirstRunState(
                    CurrentSchemaVersion,
                    Completed: true,
                    CompletedAtUtc: DateTimeOffset.UtcNow);
                File.WriteAllText(
                    temporaryPath,
                    JsonSerializer.Serialize(
                        state,
                        new JsonSerializerOptions { WriteIndented = true }));
                File.Move(
                    temporaryPath,
                    NexusPaths.FirstRunStatePath,
                    overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private sealed record FirstRunState(
        int SchemaVersion,
        bool Completed,
        DateTimeOffset CompletedAtUtc);
}
