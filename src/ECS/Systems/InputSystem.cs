using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class InputSystem
{
    // eli siis itse inputtien toiminnot.
    public static async Task Run(World world, Dictionary<int, ConsoleKeyInfo> inputs)
    {
        Entity player = GameState.Player1;

        var query = new QueryDescription().WithAll<Player>();
        world.Query(
            in query,
            (Entity entity, ref Player player) =>
            {
                foreach (var kvp in inputs)
                {
                    ref var velocity = ref world.Get<Velocity>(entity);
                    if (player.ID == kvp.Key)
                    {
                        switch (kvp.Value.Key)
                        {
                            case ConsoleKey.UpArrow:
                                velocity.Y -= 1;
                                break;
                            case ConsoleKey.DownArrow:
                                velocity.Y += 1;
                                break;
                            case ConsoleKey.LeftArrow:
                                velocity.X -= 1;
                                break;
                            case ConsoleKey.RightArrow:
                                velocity.X += 1;
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        );
    }
}
