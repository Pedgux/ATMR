namespace ATMR.Networking;

using System.Net;
using System.Text;
using System.Threading.Tasks;
using ATMR.Game;

/// <summary>
/// Handles nat punching & gets address from STUN to establish a P2P connection.
/// </summary>
public static class Puncher
{
    public static async Task Punch(IPEndPoint peer)
    {
        GameState.MessageWindow.Write($"[red]Punching: {peer}[/]");
        byte[] poke = Encoding.UTF8.GetBytes("poke");

        // tries for 30s
        for (int i = 0; i < 600; i++)
        {
            if (!UdpTransport.connected)
            {
                await UdpTransport.Udp.SendAsync(poke, poke.Length, peer);
                await Task.Delay(50);
            }
        }
    }
}
