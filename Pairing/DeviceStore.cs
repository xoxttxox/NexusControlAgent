using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexusControl.Agent.Models;
using NexusControl.Agent.Security;

namespace NexusControl.Agent.Pairing;

internal sealed class DeviceStore
{
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ActivitySaveInterval =
        TimeSpan.FromSeconds(30);

    private readonly Lock _gate = new();
    private readonly string _path;
    private readonly Dictionary<string, DateTimeOffset> _lastSavedActivity =
        new(StringComparer.Ordinal);
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
        _document.Devices ??= [];
        _document.Version = 2;
        foreach (var device in _document.Devices)
        {
            device.DeviceName = NormalizeDeviceName(device.DeviceName);
            device.Platform = NormalizePlatform(device.Platform, device.DeviceName);
            _lastSavedActivity[device.DeviceId] = device.LastSeenAt;
        }
    }

    public PairingCredentials AddDevice(
        string? requestedName,
        string? requestedPlatform)
    {
        var deviceId = $"device-{Guid.NewGuid():N}";
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var name = NormalizeDeviceName(requestedName);
        var platform = NormalizePlatform(requestedPlatform, name);
        var now = DateTimeOffset.UtcNow;

        lock (_gate)
        {
            _document.Devices.Add(new StoredDevice
            {
                DeviceId = deviceId,
                DeviceName = name,
                Platform = platform,
                TokenHash = HashToken(token),
                CreatedAt = now,
                LastSeenAt = now,
                RemoteAccessEnabled = true,
                Permissions = DevicePermission.All,
            });
            _lastSavedActivity[deviceId] = now;
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
            if (!stored.RemoteAccessEnabled)
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
            stored.LastSeenAt = now;
            if (
                !_lastSavedActivity.TryGetValue(
                    stored.DeviceId,
                    out var lastSaved)
                || now - lastSaved >= ActivitySaveInterval)
            {
                _lastSavedActivity[stored.DeviceId] = now;
                Save();
            }
            return true;
        }
    }

    public IReadOnlyList<TrustedDeviceInfo> ListDevices(
        string? currentDeviceId = null)
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            return _document.Devices
                .OrderByDescending(device => device.LastSeenAt)
                .Select(device => new TrustedDeviceInfo(
                    device.DeviceId,
                    device.DeviceName,
                    device.Platform,
                    device.CreatedAt,
                    device.LastSeenAt,
                    string.Equals(
                        device.DeviceId,
                        currentDeviceId,
                        StringComparison.Ordinal),
                    device.RemoteAccessEnabled
                        && now - device.LastSeenAt <= OnlineWindow,
                    device.RemoteAccessEnabled,
                    DevicePermissionsSnapshot.FromFlags(
                        device.Permissions & DevicePermission.All)))
                .ToArray();
        }
    }

    public bool UpdateDevice(
        string deviceId,
        string? requestedName,
        bool remoteAccessEnabled,
        DevicePermission permissions)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                device => string.Equals(
                    device.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            if (stored is null)
            {
                return false;
            }

            stored.DeviceName = NormalizeDeviceName(requestedName);
            stored.RemoteAccessEnabled = remoteAccessEnabled;
            stored.Permissions = permissions & DevicePermission.All;
            Save();
            return true;
        }
    }

    public bool HasPermission(
        string deviceId,
        DevicePermission permission)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                device => string.Equals(
                    device.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            return stored?.RemoteAccessEnabled == true
                && (stored.Permissions & permission) == permission;
        }
    }

    public DeviceAuditIdentity GetAuditIdentity(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return DeviceAuditIdentity.Unknown;
        }

        lock (_gate)
        {
            var stored = _document.Devices.FirstOrDefault(
                device => string.Equals(
                    device.DeviceId,
                    deviceId,
                    StringComparison.Ordinal));
            return stored is null
                ? DeviceAuditIdentity.Unknown
                : new DeviceAuditIdentity(
                    stored.DeviceName,
                    stored.Platform);
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

            _lastSavedActivity.Remove(deviceId);
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
                    device.RemoteAccessEnabled
                    && device.PushNotificationsEnabled
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
            return stored?.RemoteAccessEnabled == true
                && stored.PushNotificationsEnabled
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
            return stored?.RemoteAccessEnabled == true
                && stored.PushNotificationsEnabled
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

    private static string NormalizeDeviceName(string? requestedName)
    {
        var value = string.IsNullOrWhiteSpace(requestedName)
            ? "Smartphone"
            : requestedName.Trim();
        return value[..Math.Min(value.Length, 80)];
    }

    private static string NormalizePlatform(
        string? requestedPlatform,
        string deviceName)
    {
        var value = requestedPlatform?.Trim();
        if (
            string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "Mobilgerät",
                StringComparison.OrdinalIgnoreCase))
        {
            value = deviceName.Contains(
                "iPhone",
                StringComparison.OrdinalIgnoreCase)
                || deviceName.Contains(
                    "iPad",
                    StringComparison.OrdinalIgnoreCase)
                ? "iOS"
                : deviceName.Contains(
                    "Android",
                    StringComparison.OrdinalIgnoreCase)
                    || deviceName.Contains(
                        "Galaxy",
                        StringComparison.OrdinalIgnoreCase)
                    || deviceName.Contains(
                        "Pixel",
                        StringComparison.OrdinalIgnoreCase)
                    ? "Android"
                    : "Mobilgerät";
        }

        return value[..Math.Min(value.Length, 40)];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private sealed class DeviceStoreDocument
    {
        public int Version { get; set; } = 2;
        public List<StoredDevice> Devices { get; set; } = [];
    }

    private sealed class StoredDevice
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string TokenHash { get; set; } = "";
        public string? ExpoPushToken { get; set; }
        public bool PushNotificationsEnabled { get; set; }
        public bool RemoteAccessEnabled { get; set; } = true;
        public DevicePermission Permissions { get; set; } =
            DevicePermission.All;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastSeenAt { get; set; }
    }
}

internal sealed record PushNotificationTarget(
    string DeviceId,
    string DeviceName,
    string ExpoPushToken);

internal sealed record DeviceAuditIdentity(
    string DeviceName,
    string Platform)
{
    public static DeviceAuditIdentity Unknown { get; } =
        new("Unbekanntes Gerät", "Unbekannt");
}
