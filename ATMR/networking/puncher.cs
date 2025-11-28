namespace ATMR.Networking;

using System.Net.Sockets;
using System.Text;

public static class Puncher
{
    public static async Task RunListener(int port)
    {
        using var udp = new UdpClient(port);
        Console.WriteLine($"Listening on {port}...");

        while (true)
        {
            var result = await udp.ReceiveAsync();
            Console.WriteLine($"[RECV] {Encoding.UTF8.GetString(result.Buffer)}");
        }
    }

    public static async Task Send(string ip, int port, string message)
    {
        using var udp = new UdpClient();
        var data = Encoding.UTF8.GetBytes(message);
        await udp.SendAsync(data, data.Length, ip, port);
        Console.WriteLine($"[SENT] {message}");
    }
}
