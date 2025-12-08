namespace ATMR.Networking;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

/// <summary>
/// Handles nat punching & gets address from STUN to establish a P2P connection.
/// </summary>
public class Puncher
{
    public static async Task Punch(IPEndPoint peer)
    {
        AnsiConsole.MarkupLine($"[red]Punching: {peer}[/]");
        byte[] poke = Encoding.UTF8.GetBytes("poke");

        // tries for 30s
        for (int i = 0; i < 600; i++)
        {
            await UdpTransport.Udp.SendAsync(poke, poke.Length, peer);
            await Task.Delay(50);
        }
    }
}
