using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ATMR;
using ATMR.Networking;

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

    // Example consumer that maps keys to handlers
    public static async Task RunConsumer(
        ChannelReader<ConsoleKeyInfo> reader,
        CancellationToken token = default
    )
    {
        // Map keys to handlers (delegates)
        var handlers = new Dictionary<ConsoleKey, Func<ConsoleKeyInfo, Task>>
        {
            [ConsoleKey.UpArrow] = async k =>
            { /* handle up */
                UiState.MessageWindow?.OffsetUp();
                UdpTransport.SendMessage("iup");
                await Task.CompletedTask;
            },
            [ConsoleKey.DownArrow] = async k =>
            { /* handle down */
                UiState.MessageWindow?.OffsetDown();
                UdpTransport.SendMessage("idown");
                await Task.CompletedTask;
            },
        };

        await foreach (var keyInfo in reader.ReadAllAsync(token))
        {
            if (handlers.TryGetValue(keyInfo.Key, out var handler))
            {
                try
                {
                    await handler(keyInfo);
                }
                catch
                { /* log/ignore handler errors */
                }
            }
            else
            {
                // fallback: text input, or ignore
                var ch = keyInfo.KeyChar;
                if (!char.IsControl(ch))
                { /* process text char */
                }
            }
        }
    }
}
