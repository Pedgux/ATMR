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
                ConsoleKey.NumPad6 => "6",
                ConsoleKey.NumPad4 => "4",
                ConsoleKey.NumPad8 => "8",
                ConsoleKey.NumPad2 => "2",
                ConsoleKey.NumPad7 => "7",
                ConsoleKey.NumPad1 => "1",
                ConsoleKey.NumPad5 => "5",
                ConsoleKey.NumPad3 => "3",
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
                "1" => ConsoleKey.U,
                "2" => ConsoleKey.L,
                "3" => ConsoleKey.N,
                "4" => ConsoleKey.J,
                "5" => ConsoleKey.B,
                "6" => ConsoleKey.H,
                "7" => ConsoleKey.Y,
                "8" => ConsoleKey.K,
                _ => ConsoleKey.NoName,
            },
            "numpad" => actionInfo switch
            {
                "1" => ConsoleKey.NumPad1,
                "2" => ConsoleKey.NumPad2,
                "3" => ConsoleKey.NumPad3,
                "4" => ConsoleKey.NumPad4,
                "5" => ConsoleKey.NumPad5,
                "6" => ConsoleKey.NumPad6,
                "7" => ConsoleKey.NumPad7,
                "8" => ConsoleKey.NumPad8,
                _ => ConsoleKey.NoName,
            },
            _ => ConsoleKey.NoName,
        };
    }
}
