namespace ATMR.Game;

using Arch.Core;
using ATMR.Components;

/// <summary>
/// Holds a world with it's entities and a level identifier
/// </summary>
public class Level
{
    public World World;
    public int LevelNumber { get; private set; }

    public Level(int levelNumber)
    {
        World = World.Create();
        LevelNumber = levelNumber;
    }
}
