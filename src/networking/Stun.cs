namespace ATMR.Networking;

using System.Linq;
using System.Net;
using System.Net.Sockets;

public static class Stun
{
    /// <summary>
    /// black magic
    /// </summary>
    public static async Task<(string ip, ushort port)> GetPublicIPAsync(
        string host = "stun.l.google.com",
        int port = 19302
    )
    {
        var addrs = await Dns.GetHostAddressesAsync(host);
        var serverAddress =
            addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs[0];
        var server = new IPEndPoint(serverAddress, port);

        UdpTransport.Udp.Client.ReceiveTimeout = 5000;

        byte[] request = BuildRequest();
        await UdpTransport.Udp.SendAsync(request, request.Length, server);

        var response = await UdpTransport.Udp.ReceiveAsync();
        var endpoint = Parse(response.Buffer);

        return (endpoint.Address.ToString(), (ushort)endpoint.Port);
    }

    private static byte[] BuildRequest()
    {
        byte[] req = new byte[20];
        req[0] = 0x00;
        req[1] = 0x01; // Binding Request
        req[4] = 0x21;
        req[5] = 0x12;
        req[6] = 0xA4;
        req[7] = 0x42; // Magic cookie
        Random.Shared.NextBytes(req.AsSpan(8, 12)); // Transaction ID
        return req;
    }

    private static IPEndPoint Parse(byte[] data)
    {
        int index = 20;

        while (index < data.Length)
        {
            ushort attr = (ushort)(data[index] << 8 | data[index + 1]);
            ushort len = (ushort)(data[index + 2] << 8 | data[index + 3]);
            index += 4;

            if (attr == 0x0020) // XOR-MAPPED-ADDRESS
            {
                byte family = data[index + 1];
                if (family != 0x01)
                    throw new Exception("IPv6 not supported here.");

                ushort port = (ushort)(data[index + 2] << 8 | data[index + 3]);
                port ^= 0x2112;

                uint ipraw = (uint)(
                    data[index + 4] << 24
                    | data[index + 5] << 16
                    | data[index + 6] << 8
                    | data[index + 7]
                );

                ipraw ^= 0x2112A442;

                byte[] ipBytes =
                {
                    (byte)(ipraw >> 24),
                    (byte)(ipraw >> 16),
                    (byte)(ipraw >> 8),
                    (byte)(ipraw),
                };

                return new IPEndPoint(new IPAddress(ipBytes), port);
            }

            index += len;
        }

        throw new Exception("STUN response missing XOR-MAPPED-ADDRESS");
    }
}
