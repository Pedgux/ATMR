using System;
using System.Collections.Concurrent;
using System.Data;

public static class Input
{
    public static readonly ConcurrentQueue<ConsoleKey> InputQueue = new();

    // Call this from your game loop to poll and enqueue key presses
    public static void Poll()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            InputQueue.Enqueue(key.Key); // store it for the game loop
        }
    }
}
