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
    private static readonly TimeSpan TickCoalesceWindow = TimeSpan.FromMilliseconds(50);
    private static readonly Channel<(int playerId, ConsoleKeyInfo keyInfo)> InputEvents =
        Channel.CreateUnbounded<(int playerId, ConsoleKeyInfo keyInfo)>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

    private static Task? _tickPump;
    private static readonly object TickPumpLock = new();

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

    private static void EnsureTickPumpStarted(CancellationToken token = default)
    {
        lock (TickPumpLock)
        {
            _tickPump ??= Task.Run(() => TickPump(token), token);
        }
    }

    private static async Task TickPump(CancellationToken token)
    {
        var reader = InputEvents.Reader;

        while (await reader.WaitToReadAsync(token))
        {
            if (!reader.TryRead(out var first))
                continue;

            var inputs = new Dictionary<int, ConsoleKeyInfo> { [first.playerId] = first.keyInfo };
            var deadline = DateTime.UtcNow + TickCoalesceWindow;

            while (true)
            {
                while (reader.TryRead(out var next))
                {
                    inputs[next.playerId] = next.keyInfo;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;

                var waitForMore = reader.WaitToReadAsync(token).AsTask();
                var timeout = Task.Delay(remaining, token);
                var completed = await Task.WhenAny(waitForMore, timeout);
                if (completed == timeout)
                    break;
            }

            try
            {
                await Tick.CreateAsync(inputs, GameState.Level0);
            }
            catch
            {
                // ignore ticks until world/player is initialized
            }
        }
    }

    private static void EnqueueInput(int playerId, ConsoleKeyInfo keyInfo, CancellationToken token)
    {
        EnsureTickPumpStarted(token);
        InputEvents.Writer.TryWrite((playerId, keyInfo));
    }

    private static char KeyCharFromConsoleKey(ConsoleKey key)
    {
        if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
        {
            return (char)('a' + (key - ConsoleKey.A));
        }

        if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
        {
            return (char)('0' + (key - ConsoleKey.D0));
        }

        return '\0';
    }

    public static Task RecieveInput(string message)
    {
        if (message == "iup")
        {
            GameState.MessageWindow.OffsetUp();
            return Task.CompletedTask;
        }

        if (message == "idown")
        {
            GameState.MessageWindow.OffsetDown();
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(message) || message[0] != 'i')
            return Task.CompletedTask;

        // Expect: i{playerId}{ConsoleKey}, e.g. i2UpArrow
        int index = 1;
        while (index < message.Length && char.IsDigit(message[index]))
        {
            index++;
        }

        if (index == 1 || index >= message.Length)
        {
            return Task.CompletedTask;
        }

        if (!int.TryParse(message.AsSpan(1, index - 1), out int playerId))
        {
            return Task.CompletedTask;
        }

        string keyText = message.Substring(index);
        if (!Enum.TryParse<ConsoleKey>(keyText, ignoreCase: false, out var key))
        {
            return Task.CompletedTask;
        }

        var keyInfo = new ConsoleKeyInfo(KeyCharFromConsoleKey(key), key, false, false, false);
        EnqueueInput(playerId, keyInfo, CancellationToken.None);
        return Task.CompletedTask;
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
                if (UdpTransport.connected)
                {
                    var message = $"i{playerId}{keyInfo.Key}";
                    await UdpTransport.SendMessage(message);
                }
                EnqueueInput(playerId, keyInfo, token);
            }
            catch
            {
                // ignore input until world/player is initialized
            }
        }
    }
}
