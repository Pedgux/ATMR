using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class RenderSystem
{
    public static async Task Run(World world)
    {
        var query = new QueryDescription().WithAll<Position, Glyph>();

        world.Query(
            in query,
            (Entity entity, ref Position position, ref Glyph glyph) =>
            {
                // guh write to grid later when it exists
                string renderable = $"{glyph.MarkupEntry}{glyph.Symbol}[/]";

                GameState.GridWindow.SetGridCell(position.X, position.Y, renderable);
                GameState.MessageWindow.Write($"X: {position.X} Y: {position.Y}");
            }
        );
    }
}
