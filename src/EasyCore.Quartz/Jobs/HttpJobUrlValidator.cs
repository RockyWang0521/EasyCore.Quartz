using System.Net;
using System.Net.Sockets;

namespace EasyCore.Quartz.Jobs;

/// <summary>
/// Validates HTTP job target URLs to reduce SSRF risk.
/// </summary>
public static class HttpJobUrlValidator
{
    /// <summary>
    /// Returns null when <paramref name="url"/> is allowed; otherwise an error message.
    /// </summary>
    public static string? Validate(string? url, EasyCoreQuartzOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(url))
        {
            return "HTTP job URL is required.";
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "HTTP job URL is not a valid absolute URI.";
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return $"HTTP job URL scheme '{uri.Scheme}' is not allowed. Use http or https.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return "HTTP job URL must not contain user info (username/password).";
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return "HTTP job URL host is required.";
        }

        if (!options.HttpJobBlockPrivateNetworks)
        {
            return null;
        }

        if (IsAllowedHost(uri.Host, options.HttpJobAllowedHosts))
        {
            return null;
        }

        if (IPAddress.TryParse(uri.Host, out var literalIp))
        {
            return IsBlockedAddress(literalIp)
                ? $"HTTP job URL host '{uri.Host}' targets a blocked address (loopback/private/link-local/metadata)."
                : null;
        }

        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.Host);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return $"HTTP job URL host '{uri.Host}' could not be resolved: {ex.Message}";
        }

        if (addresses.Length == 0)
        {
            return $"HTTP job URL host '{uri.Host}' could not be resolved.";
        }

        if (addresses.Any(IsBlockedAddress))
        {
            return $"HTTP job URL host '{uri.Host}' resolves to a blocked address (loopback/private/link-local/metadata).";
        }

        return null;
    }

    private static bool IsAllowedHost(string host, IList<string> allowedHosts)
    {
        if (allowedHosts.Count == 0)
        {
            return false;
        }

        return allowedHosts.Any(h =>
            !string.IsNullOrWhiteSpace(h) &&
            string.Equals(h.Trim(), host, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsBlockedAddress(IPAddress address)
    {
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
            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 169.254.0.0/16 (link-local + cloud metadata 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            // 0.0.0.0/8
            if (bytes[0] == 0)
            {
                return true;
            }

            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Unique local fc00::/7
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xfe) == 0xfc)
            {
                return true;
            }

            // Link-local fe80::/10
            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
            {
                return true;
            }

            if (address.Equals(IPAddress.IPv6Any))
            {
                return true;
            }
        }

        return false;
    }
}
