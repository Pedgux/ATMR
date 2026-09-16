using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

public static class DestroySystem
{
    public static void Run(World world)
    {
        var deletables = new QueryDescription().WithAll<Destroy, Position>();
        world.Query(
            in deletables,
            (Entity entity, ref Position pos) =>
            {
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);
                GameState.Level0.Spatial.RemoveOccupancy(pos.X, pos.Y);
            }
        );
        world.Destroy(deletables);
    }
}
