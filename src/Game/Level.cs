namespace ATMR.Game;

using Arch.Core;
using ATMR.ECS;
using ATMR.Helpers;
using ATMR.UI;

public readonly record struct LevelSnapshot(World World, Wall[] Walls);

/// <summary>
/// Holds a world with it's entities and a level identifier
/// </summary>
public class Level
{
    public World World;
    public int LevelNumber { get; private set; }
    private SpatialGrid? _spatial;

    // lazy initialization to ensure the UI has been created before this!
    public SpatialGrid Spatial
    {
        get
        {
            if (_spatial == null)
            {
                if (GameState.GridWindow == null)
                {
                    Log.Write("[red]AAAAAAAA[/]");
                    throw new InvalidOperationException(
                        "GridWindow must be initialized before accessing Level.Spatial. "
                            + "Ensure UI is initialized before creating levels."
                    );
                }
                _spatial = new SpatialGrid(
                    GameState.GridWindow.GridWidth,
                    GameState.GridWindow.GridHeight
                );
            }
            return _spatial;
        }
    }

    public Level(int levelNumber)
    {
        World = World.Create();
        LevelNumber = levelNumber;
    }

    public LevelSnapshot GetSnapshot()
    {
        return new LevelSnapshot(World.Copy(), Spatial.CaptureWalls());
    }

    public void Restore(LevelSnapshot snapshot)
    {
        World = snapshot.World;
        Spatial.ApplyWalls(snapshot.Walls);
    }
}
