using System.Dynamic;

namespace ATMR.Components;

using Arch.Core;
using ATMR.Game;

public record struct Position(int X, int Y);

// MarkupEntry is for styles/colors. Gets added to the symbol as it's formatting.
// For example [red]. Closing [/] come will automatically.
public record struct Glyph(char Symbol, string MarkupEntry = "[white]");

public record struct Velocity(int X, int Y);

public record struct Teleport(int X, int Y);

public record struct Player(int ID);

public record struct Solid();

// Marks an entity as following another entity's Position.
// Each tick, a system copies the target's Position onto this entity.
public record struct FollowsEntity(Entity Target);

public record struct Camera(int X, int Y)
{
    public int firstWidthHalf { get; set; }
    public int secondWidthHalf { get; set; }
    public int firstHeightHalf { get; set; }
    public int secondHeightHalf { get; set; }

    public Camera(int x, int y, int Width, int Height)
        : this(x, y)
    {
        X = x;
        Y = y;

        // calculate halfs
        firstWidthHalf = (int)Math.Floor(Width / 2.0);
        secondWidthHalf = Width - firstWidthHalf;

        firstHeightHalf = (int)Math.Floor(Height / 2.0);
        secondHeightHalf = Height - firstHeightHalf;

        /*
        // Assign camera corners
        Left = x - firstWidthHalf;
        Top = y - firstHeightHalf;
        Right = x + secondWidthHalf;
        Bottom = y + secondHeightHalf;
        */

        GameState.MessageWindow.Write($"Left {firstWidthHalf}, Right {secondWidthHalf}");
        GameState.MessageWindow.Write($"Top {firstHeightHalf}, Bottom {secondHeightHalf}");
        GameState.MessageWindow.Write($"Width oikee {Width}, Height oikee {Height}");
    }
}
