namespace ATMR.Helpers;

using System;
using ATMR.Game;

public static class Keybinds
{
    public static string GetActionWithKey(ConsoleKey key)
    {
        return GameState.preset switch
        {
            "hjkl" => key switch
            {
                ConsoleKey.H => "6",
                ConsoleKey.J => "4",
                ConsoleKey.K => "8",
                ConsoleKey.L => "2",

                ConsoleKey.Y => "7",
                ConsoleKey.U => "1",
                ConsoleKey.B => "5",
                ConsoleKey.N => "3",
                _ => "",
            },
            "numpad" => key switch
            {
                ConsoleKey.D6 => "6",
                ConsoleKey.D4 => "4",
                ConsoleKey.D8 => "8",
                ConsoleKey.D2 => "2",

                ConsoleKey.D7 => "7",
                ConsoleKey.D1 => "1",
                ConsoleKey.D5 => "5",
                ConsoleKey.D3 => "3",
                _ => "",
            },
            _ => throw new InvalidOperationException($"Buh? '{GameState.preset}'."),
        };
    }

    public static ConsoleKey ActionInfoToConsoleKey(string actionInfo)
    {
        // Map action info (direction numbers) back to ConsoleKeys based on preset
        return GameState.preset switch
        {
            "hjkl" => actionInfo switch
            {
                "6" => ConsoleKey.H,
                "4" => ConsoleKey.J,
                "8" => ConsoleKey.K,
                "2" => ConsoleKey.L,

                "7" => ConsoleKey.Y,
                "1" => ConsoleKey.U,
                "5" => ConsoleKey.B,
                "3" => ConsoleKey.N,

                _ => ConsoleKey.NoName,
            },
            "numpad" => actionInfo switch
            {
                "6" => ConsoleKey.D6,
                "4" => ConsoleKey.D4,
                "8" => ConsoleKey.D8,
                "2" => ConsoleKey.D2,

                "7" => ConsoleKey.D7,
                "1" => ConsoleKey.D1,
                "5" => ConsoleKey.D5,
                "3" => ConsoleKey.D3,

                _ => ConsoleKey.NoName,
            },
            _ => ConsoleKey.NoName,
        };
    }
}
