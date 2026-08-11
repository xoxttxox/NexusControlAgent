using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexusControl.Agent.Models;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Pairing;

internal sealed class DeviceStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private DeviceStoreDocument _document;

    public DeviceStore()
    {
        NexusPaths.EnsureSecureDataDirectory();
        var legacyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NexusControl");
        var legacyPath = Path.Combine(
            legacyDirectory,
            "trusted-devices.json");
        _path = NexusPaths.TrustedDevicesPath;
        if (!File.Exists(_path) && File.Exists(legacyPath))
        {
            File.Copy(legacyPath, _path, overwrite: false);
        }
        _document = Load();
    }

    public PairingCredentials AddDevice(string? requestedName)
    {
        var deviceId = $"iphone-{Guid.NewGuid():N}";
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var name = string.IsNullOrWhiteSpace(requestedName)
            ? "iPhone"
            : requestedName.Trim()[..Math.Min(requestedName.Trim().Length, 80)];

        lock (_gate)
        {
            _document.Devices.Add(new StoredDevice
            {
                DeviceId = deviceId,
                DeviceName = name,
                TokenHash = HashToken(token),
                CreatedAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
            });
            Save();
        }

        return new PairingCredentials(deviceId, token);
    }

    public bool IsAuthorized(string deviceId, string token)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                item => string.Equals(item.DeviceId, deviceId, StringComparison.Ordinal));
            if (stored is null)
            {
                return false;
            }

            try
            {
                var expected = Convert.FromHexString(stored.TokenHash);
                var supplied = Convert.FromHexString(HashToken(token));
                if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            if (now - stored.LastSeenAt >= TimeSpan.FromSeconds(30))
            {
                stored.LastSeenAt = now;
                Save();
            }
            return true;
        }
    }

    public IReadOnlyList<TrustedDeviceInfo> ListDevices(
        string currentDeviceId)
    {
        lock (_gate)
        {
            return _document.Devices
                .OrderByDescending(device => device.LastSeenAt)
                .Select(device => new TrustedDeviceInfo(
                    device.DeviceId,
                    device.DeviceName,
                    device.CreatedAt,
                    device.LastSeenAt,
                    string.Equals(
                        device.DeviceId,
                        currentDeviceId,
                        StringComparison.Ordinal)))
                .ToArray();
        }
    }

    public bool RemoveDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        lock (_gate)
        {
            var removed = _document.Devices.RemoveAll(
                device => string.Equals(
                    device.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            if (removed == 0)
            {
                return false;
            }

            Save();
            return true;
        }
    }

    public bool SetPushToken(string deviceId, string expoPushToken)
    {
        if (
            string.IsNullOrWhiteSpace(deviceId)
            || string.IsNullOrWhiteSpace(expoPushToken))
        {
            return false;
        }

        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                item => string.Equals(
                    item.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            if (stored is null)
            {
                return false;
            }

            stored.ExpoPushToken = expoPushToken.Trim();
            stored.PushNotificationsEnabled = true;
            Save();
            return true;
        }
    }

    public bool DisablePushNotifications(string deviceId)
    {
        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                item => string.Equals(
                    item.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            if (stored is null)
            {
                return false;
            }

            stored.ExpoPushToken = null;
            stored.PushNotificationsEnabled = false;
            Save();
            return true;
        }
    }

    public IReadOnlyList<PushNotificationTarget> ListPushTargets()
    {
        lock (_gate)
        {
            return _document.Devices
                .Where(device =>
                    device.PushNotificationsEnabled
                    && !string.IsNullOrWhiteSpace(device.ExpoPushToken))
                .Select(device => new PushNotificationTarget(
                    device.DeviceId,
                    device.DeviceName,
                    device.ExpoPushToken!))
                .ToArray();
        }
    }

    public bool HasPushSubscription(string deviceId)
    {
        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                item => string.Equals(
                    item.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            return stored?.PushNotificationsEnabled == true
                && !string.IsNullOrWhiteSpace(stored.ExpoPushToken);
        }
    }

    public bool IsPushTokenRegistered(
        string deviceId,
        string expoPushToken)
    {
        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                item => string.Equals(
                    item.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            return stored?.PushNotificationsEnabled == true
                && string.Equals(
                    stored.ExpoPushToken,
                    expoPushToken,
                    StringComparison.Ordinal);
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _document.Devices.Count;
            }
        }
    }

    private DeviceStoreDocument Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new DeviceStoreDocument();
            }

            return JsonSerializer.Deserialize<DeviceStoreDocument>(
                File.ReadAllText(_path),
                JsonOptions) ?? new DeviceStoreDocument();
        }
        catch
        {
            return new DeviceStoreDocument();
        }
    }

    private void Save()
    {
        var temporaryPath = $"{_path}.tmp";
        File.WriteAllText(
            temporaryPath,
            JsonSerializer.Serialize(_document, JsonOptions),
            Encoding.UTF8);
        File.Move(temporaryPath, _path, true);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private sealed class DeviceStoreDocument
    {
        public int Version { get; set; } = 1;
        public List<StoredDevice> Devices { get; set; } = [];
    }

    private sealed class StoredDevice
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string TokenHash { get; set; } = "";
        public string? ExpoPushToken { get; set; }
        public bool PushNotificationsEnabled { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
    }
}

internal sealed record PushNotificationTarget(
    string DeviceId,
    string DeviceName,
    string ExpoPushToken);
