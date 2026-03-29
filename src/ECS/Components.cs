namespace ATMR.Components;

using Arch.Core;
using ATMR.Game;
using ATMR.Helpers;

public record struct Position(int X, int Y);

// MarkupEntry is for styles/colors. Gets added to the symbol as it's formatting.
// For example [red]. Closing [/] come will automatically.
public record struct Glyph(char Symbol, string MarkupEntry = "[white]");

public record struct Velocity(int X, int Y);

public record struct Teleport(int X, int Y);

// stores the RNG state for deterministic multiplayer rollbacks
public record struct RngState(uint State);

public record struct Player(int Id);

public record struct Health(int Amount, int MaxAmount);

//tag
public record struct Solid();

//tag on things that need to be destroyed properly
public record struct Destroy();

//makes an entity follow another assuming both have position
public record struct FollowsEntity(Entity Target);

public record struct Camera()
{
    public int FirstWidthHalf { get; set; }
    public int SecondWidthHalf { get; set; }
    public int FirstHeightHalf { get; set; }
    public int SecondHeightHalf { get; set; }

    public Camera(int width, int height)
        : this()
    {
        // calculate halfs. First is rounded half, other is the rest
        FirstWidthHalf = (int)Math.Floor(width / 2.0);
        SecondWidthHalf = width - FirstWidthHalf;

        FirstHeightHalf = (int)Math.Floor(height / 2.0);
        SecondHeightHalf = height - FirstHeightHalf;

        Log.Write($"Left {FirstWidthHalf}, Right {SecondWidthHalf}");
        Log.Write($"Top {FirstHeightHalf}, Bottom {SecondHeightHalf}");
        Log.Write($"Width oikee {width}, Height oikee {height}");
    }
}

// --- item ja inv ---
public record struct Item(string Name, string Description);

public record struct Stackable(int Count);

// inv = list of entities
public record struct Inventory(List<Entity> Items, int Capacity);
