namespace ATMR.Networking;

using System.Net;
using System.Threading.Tasks;
using ATMR.Helpers;

/// <summary>
/// Handles nat punching & gets address from STUN to establish a P2P connection.
/// </summary>
public static class Puncher
{
    public static async Task Punch(IPEndPoint peer)
    {
        Log.Write($"[red]Punching: {peer}[/]");

        // tries for 30s
        for (int i = 0; i < 600; i++)
        {
            await UdpTransport.SendMessage("poke");
            await Task.Delay(50);
        }

        if (!UdpTransport.connected)
        {
            Log.Write("[red] Puncher timed out[/]");
        }
    }
}
