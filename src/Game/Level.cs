namespace ATMR.Level;

using Arch.Core;
using ATMR.Components;

/// <summary>
/// Holds a world with it's entities and a level identifier
/// </summary>
public class Level
{
    private World _world;
    public int LevelNumber { get; private set; }

    public Level(int levelNumber)
    {
        _world = World.Create();
        LevelNumber = levelNumber;

        var dwarf = _world.Create(new Position(0, 0));
    }
}
