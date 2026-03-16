namespace ATMR.Networking;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ATMR.Game;
using ATMR.Helpers;
using ATMR.Input;

/// <summary>
/// Handles all UDP. heh.
/// </summary>
public static class UdpTransport
{
    private static UdpClient? _udp;
    public static UdpClient Udp
    {
        get => _udp ?? throw new InvalidOperationException("UdpTransport not initialized");
        private set => _udp = value;
    }
    private static List<IPEndPoint> peerEndpoints = new();
    public static bool connected;
    public static Stopwatch sw = Stopwatch.StartNew();

    /// <summary>
    /// Initializes fucking everything
    /// </summary>
    /// <param name="lobbyId"> the name of the lobby</param>
    public static async Task Initialize(string lobbyId)
    {
        connected = false;
        _udp = new UdpClient(0);
        Log.Write($"Local UDP bound: {_udp.Client.LocalEndPoint}");

        string ip;
        ushort port;

        if (GameState.LocalMode)
        {
            // Local mode: skip STUN, use localhost with the OS-assigned port
            ip = "127.0.0.1";
            port = (ushort)((IPEndPoint)_udp.Client.LocalEndPoint!).Port;
            Log.Write($"[green]Local mode: using {ip}:{port}[/]");
        }
        else
        {
            (ip, port) = await Stun.GetPublicIPAsync();
            Log.Write($"[green]Address from STUN: {ip}:{port}[/] ");
        }

        await Lobby.Initialize();

        // Keep NAT mapping alive while waiting in the lobby
        var natKeepAliveCts = new CancellationTokenSource();
        _ = Stun.KeepNatAlive(natKeepAliveCts.Token);

        string playerId =
            Lobby.Auth?.LocalId
            ?? throw new InvalidOperationException("Lobby.Auth or LocalId is null");
        Log.Write($"[yellow]playerId is: {playerId}[/]");
        await Lobby.Join(lobbyId, playerId, ip, port);

        List<string> blobs = await Lobby.GetOtherPlayerBlobs(lobbyId, playerId);
        if (blobs.Count == 0)
            throw new InvalidOperationException(
                "Lobby.GetOtherPlayerBlobs returned no blobs for the other players in the lobby"
            );

        // Stop NAT keepalive — punching takes over
        natKeepAliveCts.Cancel();

        // Connect to all peers
        peerEndpoints.Clear();
        foreach (string blob in blobs)
        {
            (string peerIp, ushort peerPort) = IpPortEncoder.Decode(blob);
            var ep = new IPEndPoint(IPAddress.Parse(peerIp), peerPort);
            peerEndpoints.Add(ep);
            _ = Puncher.Punch(ep);
        }

        // Single receive loop handles packets from all peers
        await ReceiveLoop();

        // TODO:
        // after game is over, each user deletes their own node and last user deletes lobby
        // unless firebase auto deletes empty JSON Objects. This was true while testing with manual deletion
    }

    /// <summary>
    /// Handles all incoming packets from any peer
    /// </summary>
    public static async Task ReceiveLoop()
    {
        Log.Write("[blue]Starting Receiveloop![/]");
        while (true)
        {
            try
            {
                var result = await Udp.ReceiveAsync();
                var sender = result.RemoteEndPoint;

                string message = Encoding.UTF8.GetString(result.Buffer, 0, result.Buffer.Length);

                if (message.StartsWith("i"))
                {
                    await Input.ReceiveInput(message);
                    continue;
                }

                // ping/pong using timestamps to calculate RTT
                if (message.StartsWith("ping:", StringComparison.Ordinal))
                {
                    string ts = message.Substring(5).Trim();
                    await SendMessage($"pong:{ts}", sender);
                }

                if (message.StartsWith("pong:", StringComparison.Ordinal))
                {
                    string ts = message.Substring(5).Trim();
                    if (long.TryParse(ts, out long sentTs))
                    {
                        long rtt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sentTs;
                        GameState.PingList.Add(rtt);
                        while (GameState.PingList.Count > 20)
                            GameState.PingList.RemoveAt(0);
                    }
                }

                if (result.Buffer.Length == 1 && result.Buffer[0] == 0x01)
                {
                    Log.Write("[blue]alive[/]");
                }

                if (message == "poke")
                {
                    if (!connected)
                    {
                        Log.Write("[green]Got a connection![/]");
                        connected = true;
                        await KeepAliveLoop();
                        await PingLoop();
                    }
                    continue;
                }
            }
            catch (Exception ex)
            {
                Log.Write($"[red]Receive error: {ex}[/]");
            }
        }
    }

    /// <summary>
    /// Sends a message to a specific peer
    /// </summary>
    public static async Task SendMessage(string message, IPEndPoint target)
    {
        var messageByte = Encoding.UTF8.GetBytes(message);
        try
        {
            await Udp.SendAsync(messageByte, messageByte.Length, target);
        }
        catch (Exception ex)
        {
            Log.Write($"Send error to {target}: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcasts a message to all peers
    /// </summary>
    public static async Task SendMessage(string message)
    {
        foreach (var peer in peerEndpoints)
        {
            await SendMessage(message, peer);
        }
    }

    private static Task KeepAliveLoop()
    {
        byte[] poke = { 0x01 };

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(30000);
                foreach (var peer in peerEndpoints)
                {
                    try
                    {
                        await Udp.SendAsync(poke, poke.Length, peer);
                    }
                    catch (Exception ex)
                    {
                        Log.Write(
                            $"KeepAlive send error to {peer}: {ex.Message}"
                        );
                    }
                }
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends pings to all peers
    /// </summary>
    private static Task PingLoop()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await SendMessage($"ping:{ts}");
                await Task.Delay(1000);
            }
        });

        return Task.CompletedTask;
    }
}
