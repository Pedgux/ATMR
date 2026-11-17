namespace ATMR.Helpers;

using System.Net;
using System.Net.Sockets;

public static class IpPortEncoder
{
    // Blob layout (12 bytes, ports big-endian):
    // [0..3] public IP  [4..5] public port  [6..9] LAN IP  [10..11] LAN port
    public static string Encode(IPEndPoint pub, IPEndPoint lan)
    {
        byte[] bytes = new byte[12];
        WriteEndpoint(bytes, 0, pub);
        WriteEndpoint(bytes, 6, lan);
        return Convert.ToBase64String(bytes);
    }

    public static (IPEndPoint pub, IPEndPoint lan) Decode(string blob)
    {
        byte[] bytes = Convert.FromBase64String(blob);

        // Old 6-byte blobs (public endpoint only) still decode; LAN falls back to public.
        if (bytes.Length == 6)
        {
            var only = ReadEndpoint(bytes, 0);
            return (only, only);
        }

        if (bytes.Length != 12)
            throw new ArgumentException("Invalid blob");

        return (ReadEndpoint(bytes, 0), ReadEndpoint(bytes, 6));
    }

    /// <summary>
    /// Picks the local IPv4 address the OS would use to reach the internet.
    /// Connecting a UDP socket sends no packet, it only selects the route.
    /// </summary>
    public static IPAddress GetLanIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)s.LocalEndPoint!).Address;
        }
        catch (SocketException)
        {
            return IPAddress.Loopback; // no network at all
        }
    }

    private static void WriteEndpoint(byte[] buf, int offset, IPEndPoint ep)
    {
        byte[] ip = ep.Address.GetAddressBytes();
        if (ip.Length != 4)
            throw new ArgumentException("Only IPv4 is supported");

        Array.Copy(ip, 0, buf, offset, 4);
        buf[offset + 4] = (byte)(ep.Port >> 8);
        buf[offset + 5] = (byte)(ep.Port & 0xFF);
    }

    private static IPEndPoint ReadEndpoint(byte[] buf, int offset)
    {
        var ip = new IPAddress(new ReadOnlySpan<byte>(buf, offset, 4));
        int port = (buf[offset + 4] << 8) | buf[offset + 5];
        return new IPEndPoint(ip, port);
    }
}