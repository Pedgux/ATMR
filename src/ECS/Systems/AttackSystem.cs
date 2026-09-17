using Arch.Core;
using Arch.Core.Extensions;
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

                if (
                    GameState.Level0.Spatial.TryGetEntity(targetX, targetY, out Entity targetEntity)
                )
                {
                    ref var health = ref targetEntity.TryGetRef<Health>(out bool hasHealth);
                    if (hasHealth)
                        health.Amount -= 1;
                }
            }
        );
        world.Remove<AttackIntent>(in query);
    }
}
