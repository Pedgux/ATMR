namespace ATMR.Helpers;

using System;
using ATMR.Game;

public static class InputHelper
{
    public static string GetActionInfoWithKey(ConsoleKey key)
    {
        return GameState.preset switch
        {
            "hjkl" => key switch
            {
                ConsoleKey.H => "4",
                ConsoleKey.J => "2",
                ConsoleKey.K => "8",
                ConsoleKey.L => "6",

                ConsoleKey.Y => "7",
                ConsoleKey.U => "9",
                ConsoleKey.B => "1",
                ConsoleKey.N => "3",
                _ => "",
            },
            "numpad" => key switch
            {
                ConsoleKey.D4 => "4",
                ConsoleKey.D2 => "2",
                ConsoleKey.D6 => "6",
                ConsoleKey.D8 => "8",

                ConsoleKey.D5 => "5",

                ConsoleKey.D7 => "7",
                ConsoleKey.D9 => "9",
                ConsoleKey.D1 => "1",
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
                "4" => ConsoleKey.H,
                "2" => ConsoleKey.J,
                "8" => ConsoleKey.K,
                "6" => ConsoleKey.L,

                "7" => ConsoleKey.Y,
                "9" => ConsoleKey.U,
                "1" => ConsoleKey.B,
                "3" => ConsoleKey.N,

                _ => ConsoleKey.NoName,
            },
            "numpad" => actionInfo switch
            {
                "4" => ConsoleKey.D4,
                "2" => ConsoleKey.D2,
                "6" => ConsoleKey.D6,
                "8" => ConsoleKey.D8,

                "5" => ConsoleKey.D5,

                "7" => ConsoleKey.D7,
                "9" => ConsoleKey.D9,
                "1" => ConsoleKey.D1,
                "3" => ConsoleKey.D3,

                _ => ConsoleKey.NoName,
            },
            _ => ConsoleKey.NoName,
        };
    }

    public static char KeyCharFromConsoleKey(ConsoleKey key)
    {
        // Convert A-Z and 0-9 ConsoleKey values to their char representation.
        // For non-alphanumeric keys, return NUL (no meaningful char).
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
}
