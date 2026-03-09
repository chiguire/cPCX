using System.Net;
using cpcx.Config;
using Microsoft.Extensions.Options;

namespace cpcx.Middleware;

public class IpAllowlistMiddleware(RequestDelegate next, IOptions<IpAllowlistConfig> options)
{
    private readonly IpAllowlistConfig _config = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_config.Enabled || context.Request.Path.StartsWithSegments("/AccessDenied"))
        {
            await next(context);
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            context.Response.Redirect("/AccessDenied");
            return;
        }

        // Map IPv4-in-IPv6 (e.g. ::ffff:127.0.0.1) back to IPv4
        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        if (IsAllowed(remoteIp))
        {
            await next(context);
            return;
        }

        context.Response.Redirect("/AccessDenied");
    }

    private bool IsAllowed(IPAddress remoteIp)
    {
        foreach (var entry in _config.AllowedIPs)
        {
            if (MatchesCidrOrIp(remoteIp, entry))
                return true;
        }
        return false;
    }

    private static bool MatchesCidrOrIp(IPAddress remoteIp, string entry)
    {
        int prefixLength;
        IPAddress prefix;

        var slashIndex = entry.IndexOf('/');
        if (slashIndex >= 0)
        {
            if (!IPAddress.TryParse(entry[..slashIndex], out var parsedPrefix))
                return false;
            if (!int.TryParse(entry[(slashIndex + 1)..], out prefixLength))
                return false;
            prefix = parsedPrefix;
        }
        else
        {
            if (!IPAddress.TryParse(entry, out var parsedPrefix))
                return false;
            prefix = parsedPrefix;
            prefixLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
        }

        // Address families must match
        if (remoteIp.AddressFamily != prefix.AddressFamily)
            return false;

        var remoteBytes = remoteIp.GetAddressBytes();
        var prefixBytes = prefix.GetAddressBytes();

        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (remoteBytes[i] != prefixBytes[i])
                return false;
        }

        if (remainingBits > 0 && fullBytes < remoteBytes.Length)
        {
            byte mask = (byte)(0xFF << (8 - remainingBits));
            if ((remoteBytes[fullBytes] & mask) != (prefixBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }
}
