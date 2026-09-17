using Arch.Core;
using ATMR.Components;
using ATMR.ECS;
using ATMR.Game;

namespace ATMR.Systems;

public static class AttackSystem
{
    public static void Run(World world)
    {
        /*
        if (intents.Count == 0)
        {
            return;
        }

        var digIntents = intents
            .Where(intent => intent.Kind == ActionKind.Dig)
            .GroupBy(intent => intent.PlayerId)
            .ToDictionary(group => group.Key, group => group.Last());

        if (digIntents.Count == 0)
        {
            return;
        }
        */
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
                {
                    // möy
                } 
                // ExecuteDig(world, targetX, targetY);
                //GameState.TimeCounter += 20;
            }
        );
    }
}