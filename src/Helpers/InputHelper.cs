namespace ATMR.Helpers;

using System;
using ATMR.Game;

public static class InputHelper
{
    public static string GetActionInfoWithKey(ConsoleKeyInfo keyInfo)
    {
        var key = keyInfo.Key;
        bool shift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;

        if (key == ConsoleKey.T)
        {
            return "T";
        }

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
            "arrows" => (key, shift) switch
            {
                (ConsoleKey.LeftArrow, false) => "4",
                (ConsoleKey.DownArrow, false) => "2",
                (ConsoleKey.UpArrow, false) => "8",
                (ConsoleKey.RightArrow, false) => "6",

                // Shift+Arrow = diagonal (rotated 45° clockwise)
                (ConsoleKey.LeftArrow, true) => "7", // left  → up-left
                (ConsoleKey.DownArrow, true) => "1", // down  → down-left
                (ConsoleKey.UpArrow, true) => "9", // up    → up-right
                (ConsoleKey.RightArrow, true) => "3", // right → down-right
                _ => "",
            },
            _ => throw new InvalidOperationException($"Buh? '{GameState.preset}'."),
        };
    }

    public static ConsoleKeyInfo ActionInfoToConsoleKeyInfo(string actionInfo)
    {
        // Map action info (direction numbers) back to ConsoleKeyInfo based on preset
        if (actionInfo == "T")
        {
            return new ConsoleKeyInfo('t', ConsoleKey.T, false, false, false);
        }

        ConsoleKey key;
        bool shift = false;

        switch (GameState.preset)
        {
            case "hjkl":
                key = actionInfo switch
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
                };
                break;
            case "numpad":
                key = actionInfo switch
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
                };
                break;
            case "arrows":
                (key, shift) = actionInfo switch
                {
                    "4" => (ConsoleKey.LeftArrow, false),
                    "2" => (ConsoleKey.DownArrow, false),
                    "8" => (ConsoleKey.UpArrow, false),
                    "6" => (ConsoleKey.RightArrow, false),

                    "7" => (ConsoleKey.LeftArrow, true),
                    "1" => (ConsoleKey.DownArrow, true),
                    "9" => (ConsoleKey.UpArrow, true),
                    "3" => (ConsoleKey.RightArrow, true),

                    _ => (ConsoleKey.NoName, false),
                };
                break;
            default:
                key = ConsoleKey.NoName;
                break;
        }

        return new ConsoleKeyInfo(KeyCharFromConsoleKey(key), key, shift, false, false);
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

    public static bool TryGetDirectionActionInfo(ConsoleKeyInfo keyInfo, out string actionInfo)
    {
        // Reuse the normal movement mapping so directional actions follow the active preset.
        actionInfo = GetActionInfoWithKey(keyInfo);
        return IsDirectionalActionInfo(actionInfo);
    }

    public static bool IsDirectionalActionInfo(string actionInfo)
    {
        // Directional actions only accept actual movement directions (no "5" wait).
        return actionInfo is "1" or "2" or "3" or "4" or "6" or "7" or "8" or "9";
    }

    public static bool TryGetDirectionOffset(string actionInfo, out int dx, out int dy)
    {
        // Converts numpad-like direction codes into world offsets.
        (dx, dy) = actionInfo switch
        {
            "4" => (-1, 0),
            "2" => (0, 1),
            "8" => (0, -1),
            "6" => (1, 0),
            "7" => (-1, -1),
            "9" => (1, -1),
            "1" => (-1, 1),
            "3" => (1, 1),
            _ => (0, 0),
        };

        return !(dx == 0 && dy == 0);
    }

    public static ConsoleKeyInfo CreateDirectionalActionKey(char action, string directionActionInfo)
    {
        if (!IsDirectionalActionInfo(directionActionInfo))
        {
            throw new ArgumentException("Direction input must be one of 1,2,3,4,6,7,8,9");
        }

        // Encode resolved directional actions as synthetic ConsoleKeyInfo values so they can
        // travel through the same input/tick pipeline as normal movement inputs.
        // We use Key=F plus Alt=true as an internal marker for directional action packets.
        return new ConsoleKeyInfo(
            directionActionInfo[0],
            ConsoleKey.F,
            false,
            action == 'D',
            false
        );
    }

    public static bool TryParseDirectionalAction(
        ConsoleKeyInfo keyInfo,
        out char action,
        out string directionActionInfo
    )
    {
        directionActionInfo = string.Empty;
        action = '\0';

        // Only synthetic directional actions should match this parser.
        // Real keyboard F presses are handled earlier in Input.RunConsumer.
        if (keyInfo.Key != ConsoleKey.F)
        {
            return false;
        }

        if (keyInfo.KeyChar is < '1' or > '9')
        {
            return false;
        }

        directionActionInfo = keyInfo.KeyChar.ToString();
        if (!IsDirectionalActionInfo(directionActionInfo))
        {
            return false;
        }

        // Alt=true marks the synthetic "resolved dig action" payload.
        action = (keyInfo.Modifiers & ConsoleModifiers.Alt) != 0 ? 'D' : '\0';
        return action != '\0';
    }
}
