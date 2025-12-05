using System;
using System.Collections.Concurrent;
using System.Data;

public static class Input
{
    public static async Task StartPolling()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
            }
        }
    }
}
