using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

public static class RenderSystem
{
    public static void Run(World world)
    {
        var query = new QueryDescription().WithAll<Position, Glyph>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref Glyph glyph) =>
            {
                string renderable =
                    $"{glyph.MarkupEntry}{glyph.Symbol}[/] [white]({position.X}, {position.Y})";
                GameState.MessageWindow.Write($"{renderable}[/]");
            }
        );
    }
}
