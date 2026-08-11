using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NexusControl.Agent.Networking;

internal static class NetworkUtilities
{
    private static readonly byte[] TailscaleIPv6Prefix =
        [0xfd, 0x7a, 0x11, 0x5c, 0xa1, 0xe0];

    public static bool IsPrivateOrLoopback(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return IsTailscaleAddress(address)
                || bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return address.IsIPv6LinkLocal || (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }

    public static bool IsTailscaleAddress(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 100 && bytes[1] is >= 64 and <= 127;
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6
            && bytes.AsSpan(0, TailscaleIPv6Prefix.Length)
                .SequenceEqual(TailscaleIPv6Prefix);
    }

    public static IReadOnlyList<string> GetPrivateIPv4Addresses()
    {
        var addresses = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(network =>
                network.OperationalStatus == OperationalStatus.Up
                && network.NetworkInterfaceType != NetworkInterfaceType.Loopback
                && network.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(network =>
                network.GetIPProperties().UnicastAddresses.Select(item => item.Address))
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork
                && IsPrivateOrLoopback(address)
                && !IsTailscaleAddress(address)
                && !IPAddress.IsLoopback(address))
            .Select(address => address.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(AddressPriority)
            .ThenBy(address => address, StringComparer.Ordinal)
            .ToArray();

        return addresses.Length > 0 ? addresses : ["127.0.0.1"];
    }

    public static IReadOnlyList<string> GetTailscaleIPv4Addresses() =>
        NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(network =>
                network.OperationalStatus == OperationalStatus.Up
                && network.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(network =>
                network.GetIPProperties().UnicastAddresses.Select(item => item.Address))
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork
                && IsTailscaleAddress(address))
            .Select(address => address.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(address => address, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<string> GetReachableIPv4Addresses() =>
        GetPrivateIPv4Addresses()
            .Concat(GetTailscaleIPv4Addresses())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static string GetPreferredIPv4Address() =>
        GetPrivateIPv4Addresses()[0];

    public static WakeOnLanNetworkInfo GetWakeOnLanInfo()
    {
        try
        {
            var candidates = new List<WakeOnLanCandidate>();
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (
                    network.OperationalStatus != OperationalStatus.Up
                    || network.NetworkInterfaceType
                        is NetworkInterfaceType.Loopback
                        or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var macBytes = network.GetPhysicalAddress().GetAddressBytes();
                if (
                    macBytes.Length != 6
                    || macBytes.All(value => value == 0))
                {
                    continue;
                }

                var properties = network.GetIPProperties();
                var hasDefaultGateway = properties.GatewayAddresses.Any(
                    gateway =>
                        gateway.Address.AddressFamily
                            == AddressFamily.InterNetwork
                        && !gateway.Address.Equals(IPAddress.Any));
                foreach (var unicast in properties.UnicastAddresses)
                {
                    var address = unicast.Address;
                    if (
                        address.AddressFamily
                            != AddressFamily.InterNetwork
                        || !IsPrivateOrLoopback(address)
                        || IsTailscaleAddress(address)
                        || IPAddress.IsLoopback(address))
                    {
                        continue;
                    }

                    var mask = unicast.IPv4Mask;
                    if (mask is null)
                    {
                        continue;
                    }

                    var broadcast = CalculateBroadcastAddress(address, mask);
                    candidates.Add(new WakeOnLanCandidate(
                        Address: address.ToString(),
                        MacAddress: string.Join(
                            ":",
                            macBytes.Select(value => value.ToString("X2"))),
                        BroadcastAddress: broadcast.ToString(),
                        HasDefaultGateway: hasDefaultGateway,
                        NetworkType: network.NetworkInterfaceType));
                }
            }

            var selected = candidates
                .OrderBy(candidate => candidate.HasDefaultGateway ? 0 : 1)
                .ThenBy(candidate => AddressPriority(candidate.Address))
                .ThenBy(candidate =>
                    candidate.NetworkType
                        == NetworkInterfaceType.Ethernet
                        ? 0
                        : candidate.NetworkType
                            == NetworkInterfaceType.Wireless80211
                            ? 1
                            : 2)
                .FirstOrDefault();

            return selected is null
                ? new WakeOnLanNetworkInfo(
                    false,
                    null,
                    null,
                    9,
                    "Kein aktiver LAN- oder WLAN-Adapter mit MAC-Adresse gefunden.")
                : new WakeOnLanNetworkInfo(
                    true,
                    selected.MacAddress,
                    selected.BroadcastAddress,
                    9,
                    "Magic Packet im lokalen Netzwerk verfügbar.");
        }
        catch (Exception error)
        {
            return new WakeOnLanNetworkInfo(
                false,
                null,
                null,
                9,
                $"Netzwerkadapter konnten nicht gelesen werden: {error.Message}");
        }
    }

    private static IPAddress CalculateBroadcastAddress(
        IPAddress address,
        IPAddress mask)
    {
        var addressBytes = address.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        var broadcastBytes = new byte[addressBytes.Length];
        for (var index = 0; index < addressBytes.Length; index++)
        {
            broadcastBytes[index] = (byte)(
                addressBytes[index] | (maskBytes[index] ^ 255));
        }
        return new IPAddress(broadcastBytes);
    }

    private static int AddressPriority(string address) =>
        address.StartsWith("192.168.", StringComparison.Ordinal) ? 0
        : address.StartsWith("10.", StringComparison.Ordinal) ? 1
        : address.StartsWith("172.", StringComparison.Ordinal) ? 2
        : 3;

    private sealed record WakeOnLanCandidate(
        string Address,
        string MacAddress,
        string BroadcastAddress,
        bool HasDefaultGateway,
        NetworkInterfaceType NetworkType);
}

internal sealed record WakeOnLanNetworkInfo(
    bool Available,
    string? MacAddress,
    string? BroadcastAddress,
    int Port,
    string Message);
