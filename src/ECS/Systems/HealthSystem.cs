using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;

namespace ATMR.Systems;

public static class HealthSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<Health>().WithNone<Destroy>();

        world.Query(
            in query,
            (Entity entity, ref Health health) =>
            {
                if (health.Amount < 0)
                {
                    entity.Add(new Destroy());
                }
            }
        );
    }
}
