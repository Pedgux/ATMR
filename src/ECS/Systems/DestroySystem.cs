using Arch.Core;
using Arch.Core.Extensions;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

public static class DestroySystem
{
    public static void Run(World world)
    {
        var deletables = new QueryDescription().WithAll<Destroy>();
        world.Destroy(deletables);
    }
}
