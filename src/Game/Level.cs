namespace ATMR.Level;

using Arch.Core;
using ATMR.Components;

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
