using System.Dynamic;

namespace ATMR.Components;

public record struct Position(int X, int Y);

// MarkupEntry is for styles/colors. Gets added to the symbol as it's formatting.
// For example [red]. Closing [/] come will automatically.
public record struct Glyph(char Symbol, string MarkupEntry = "[white]");

public record struct Velocity(int X, int Y);

public record struct Teleport(int X, int Y);

public record struct Player(int ID);

public record struct Solid();

// todo reminder thing:
// Remove position from here, need to make a Camera entity.
// Which would have the Camera, Teleport and Position component.
// Then maybe some modes? Or a reference to a X and Y it follows.
// Support resizing...

public record struct Camera(int X, int Y)
{
    public int LeftTop { get; set; }
    public int LeftBottom { get; set; }
    public int RightTop { get; set; }
    public int RightBottom { get; set; }

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
        LeftTop = x - firstWidthHalf;
        LeftBottom = y - firstHeightHalf;
        RightTop = x + secondWidthHalf;
        RightBottom = y + secondHeightHalf;
    }
}
