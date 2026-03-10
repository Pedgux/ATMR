using System.Dynamic;

namespace ATMR.Components;

using Arch.Core;

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
    public int Left { get; set; }
    public int Right { get; set; }
    public int Top { get; set; }
    public int Bottom { get; set; }

    public Camera(int x, int y, int Width, int Height)
        : this(x, y)
    {
        X = x;
        Y = y;

        // calculate halfs
        int firstWidthHalf = (int)Math.Floor(Width / 2.0);
        int secondWidthHalf = Width - firstWidthHalf;

        int firstHeightHalf = (int)Math.Floor(Height / 2.0);
        int secondHeightHalf = Height - firstHeightHalf;

        // Assign camera corners
        Left = x - firstWidthHalf;
        Top = y - firstHeightHalf;
        Right = x + secondWidthHalf;
        Bottom = y + secondHeightHalf;
    }
}
