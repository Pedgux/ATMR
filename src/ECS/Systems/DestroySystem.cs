using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class DestroySystem
{
    public static void Run(World world)
    {
        var deletables = new QueryDescription().WithAll<Destroy, Position>();
        Log.Write($" NO NYT AINAKIN KUOLEE {deletables}");
        world.Query(
            in deletables,
            (Entity entity, ref Position pos) =>
            {
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);
                CollisionSystem.RemoveOccupancy(pos.X, pos.Y);
            }
        );
        world.Destroy(deletables);
    }
}
