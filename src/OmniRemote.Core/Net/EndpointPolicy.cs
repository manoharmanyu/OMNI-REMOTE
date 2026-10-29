using System.Net;
using System.Net.Sockets;

namespace OmniRemote.Core.Net;

/// <summary>
/// Restricts where network adapters may send traffic.
/// Local integrations must target private/link-local LAN addresses (prevents the app being used to reach
/// arbitrary internet hosts with device credentials). HTTPS is required for anything that is not on the LAN.
/// </summary>
public static class EndpointPolicy
{
    public static bool TryParseLanHost(string? host, out IPAddress address)
    {
        address = IPAddress.None;
        if (string.IsNullOrWhiteSpace(host) || !IPAddress.TryParse(host.Trim(), out var parsed)) return false;
        if (!IsPrivate(parsed)) return false;
        address = parsed;
        return true;
    }

    public static bool IsPrivate(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return false; // never talk to services on the phone itself
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254);
        }
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return ip.IsIPv6LinkLocal || ip.IsIPv6UniqueLocal;
        return false;
    }

    /// <summary>
    /// Validates a base URL for an HTTP integration. Accepts https:// anywhere, or http:// only to a private LAN IP.
    /// Rejects credentials in the URL, query strings and fragments.
    /// </summary>
    public static bool TryValidateBaseUrl(string? value, out Uri uri, out string? error)
    {
        uri = null!;
        error = null;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed))
        {
            error = "Enter a valid URL.";
            return false;
        }
        if (!string.IsNullOrEmpty(parsed.UserInfo) || !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment))
        {
            error = "The URL must not contain credentials, a query or a fragment.";
            return false;
        }
        if (parsed.Scheme == Uri.UriSchemeHttps)
        {
            uri = parsed;
            return true;
        }
        if (parsed.Scheme == Uri.UriSchemeHttp && IPAddress.TryParse(parsed.IdnHost, out var ip) && IsPrivate(ip))
        {
            uri = parsed;
            return true;
        }
        error = "Use https://, or http:// only for a private LAN IP address.";
        return false;
    }
}
