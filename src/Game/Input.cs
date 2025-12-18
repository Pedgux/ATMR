namespace ATMR.Input;

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;
using ATMR.Networking;
using ATMR.Tick;

public static class Input
{
    // Start the background poller and return the channel reader
    public static ChannelReader<ConsoleKeyInfo> StartPolling(CancellationToken token = default)
    {
        var chan = Channel.CreateUnbounded<ConsoleKeyInfo>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = true }
        );
        _ = Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        await chan.Writer.WriteAsync(key, token);
                    }
                    else
                    {
                        await Task.Delay(10, token);
                    }
                }
                chan.Writer.TryComplete();
            },
            token
        );
        return chan.Reader;
    }

    public static void RecieveInput(string message)
    {
        if (message == "iup")
        {
            GameState.MessageWindow.OffsetUp();
        }
        if (message == "idown")
        {
            GameState.MessageWindow.OffsetDown();
        }
    }

    // Example consumer that maps keys to handlers
    public static async Task RunConsumer(
        ChannelReader<ConsoleKeyInfo> reader,
        CancellationToken token = default
    )
    {
        await foreach (var keyInfo in reader.ReadAllAsync(token))
        {
            if (keyInfo.Key == ConsoleKey.PageUp)
            {
                try
                {
                    GameState.MessageWindow.OffsetUp();
                }
                catch { }

                continue;
            }

            if (keyInfo.Key == ConsoleKey.PageDown)
            {
                try
                {
                    GameState.MessageWindow.OffsetDown();
                }
                catch { }

                continue;
            }

            try
            {
                var level = GameState.Level0;
                var playerId = level.World.Get<Player>(GameState.Player1).ID;
                var inputs = new Dictionary<int, ConsoleKeyInfo> { [playerId] = keyInfo };
                await Tick.CreateAsync(inputs, level);
            }
            catch
            {
                // ignore input until world/player is initialized
            }
        }
    }
}
