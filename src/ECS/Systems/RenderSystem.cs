using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using Spectre.Console;

namespace ATMR.Systems;

public static class RenderSystem
{
    public static async Task Run(World world)
    {
        var nonPlayerQuery = new QueryDescription().WithAll<Position, Glyph>().WithNone<Player>();
        var playerQuery = new QueryDescription().WithAll<Position, Glyph, Player>();

        world.Query(
            in nonPlayerQuery,
            (Entity entity, ref Position position, ref Glyph glyph) =>
            {
                // guh write to grid later when it exists
                string renderable = $"{glyph.MarkupEntry}{glyph.Symbol}[/]";

                GameState.GridWindow.SetGridCell(position.X, position.Y, renderable);
            }
        );

        // piirrä pelaajat lopuksi
        world.Query(
            in playerQuery,
            (Entity entity, ref Position position, ref Glyph glyph, ref Player player) =>
            {
                string highlightedPlayer = $"[black on yellow bold]{glyph.Symbol}[/]";
                GameState.GridWindow.SetGridCell(position.X, position.Y, highlightedPlayer);
            }
        );

        // Refresh the grid immediately so rendering stays in sync with game state.
        GameState.GridWindow.RefreshPanel();
    }
}
