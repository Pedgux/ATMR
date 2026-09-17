using Arch.Core;
using ATMR.Components;
using ATMR.ECS;
using ATMR.Game;

namespace ATMR.Systems;

public static class AttackSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<AttackIntent, Position>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref AttackIntent intent) =>
            {
                int targetX = position.X + intent.X;
                int targetY = position.Y + intent.Y;

                GameState.Level0.Spatial.TryGetEntity(targetX, targetY, out Entity targetEntity);
                if (world.Has<Health>(targetEntity))
                {
                    var health = world.Get<Health>(targetEntity);
                    health.Amount -= 1;
                }
            }
        );
    }
}
