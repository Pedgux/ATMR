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
        if (keyInfo.KeyChar == ',' || keyInfo.KeyChar == 'g')
        {
            return "Pickup";
        }
        if (keyInfo.KeyChar == 'd')
        {
            return "Drop";
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

    // sama kuin parse list, paitsi yhdelle tavaralle vain. katso tarkemmat kommentit alempaa
    public static bool TryParsePickupAction(string actionInfo, out int amount, out int itemIndex)
    {
        amount = 0;
        itemIndex = 0;

        if (string.IsNullOrEmpty(actionInfo) || !actionInfo.StartsWith("Pickup"))
        {
            return false;
        }

        if (actionInfo.StartsWith("PickupList_"))
        {
            return false; // Leave lists for the new method
        }

        string[] parts = actionInfo.Substring(6).Split('_');
        if (
            parts.Length == 2
            && int.TryParse(parts[0], out amount)
            && int.TryParse(parts[1], out itemIndex)
        )
        {
            return true;
        }
        return false;
    }

    // tekee actionInfosta ostoslistan (dictionaryn). "PickupList_(slot):(amount (-1 = all))_(repeat)
    public static bool TryParsePickupListAction(
        string actionInfo,
        out Dictionary<int, int> pickupCart
    )
    {
        pickupCart = new Dictionary<int, int>();

        if (string.IsNullOrEmpty(actionInfo) || !actionInfo.StartsWith("PickupList_"))
        {
            return false;
        }
        // onhan pickup list (kai turhaa literaly)
        string cartData = actionInfo.Substring("PickupList_".Length);
        if (string.IsNullOrEmpty(cartData))
        {
            return false;
        }

        // _ erottelee tavarat
        string[] items = cartData.Split('_');
        foreach (var item in items)
        {
            // montako. per tavara
            string[] parts = item.Split(':');
            if (
                parts.Length == 2
                && int.TryParse(parts[0], out int itemIndex)
                && int.TryParse(parts[1], out int amount)
            )
            {
                pickupCart[itemIndex] = amount;
            }
            else
            {
                return false;
            }
        }

        return pickupCart.Count > 0;
    }
}
