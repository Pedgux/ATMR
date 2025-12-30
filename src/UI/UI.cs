namespace ATMR.UI;

using ATMR.Game;
using Spectre.Console;

public sealed class UI
{
    private Layout _root = new();

    // Public accessor for other parts of the app to query the layout.
    public Layout RootLayout => _root;

    public UI()
    {
        Initialize();
    }

    public Task Initialize()
    {
        // Create the UI
        _root = new Layout("Root").SplitColumns(
            new Layout("Left")
                .Size(GameState.LeftWidth)
                .Ratio(6)
                .SplitRows(
                    new Layout("Grid") /*.MinimumSize(20)*/
                        .Size(GameState.LeftTop)
                        .Ratio(8),
                    new Layout("Stats") /*.MinimumSize(5)*/
                        .Size(GameState.LeftBottom)
                        .Ratio(2)
                ),
            new Layout("Right")
                .Size(GameState.RightWidth)
                .SplitRows(
                    new Layout("Messages").Size(GameState.RightTop),
                    new Layout("Inventory").Size(GameState.RightBottom)
                )
        );

        /*
        // Stats have all players? Potential idea
        int playerCount = 2;
        var statsTable = new Table { Expand = true };
        for (int i = 1; i <= playerCount; i++)
            statsTable.AddColumn($"p{i}");

        _root["Stats"].Update(statsTable);
        */
        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-assigns the correct sizes to each UI element.
    /// </summary>
    public void Fit()
    {
        // todo reminder thingy:
        // Grid currently breaks with this. Gotta rebuild it or something. Maybe support this in the future when a camera is implemented.
        GameState.RecalculateConsoleSizes();

        _root["Left"].Size = GameState.LeftWidth;
        _root["Grid"].Size = GameState.LeftTop;
        _root["Stats"].Size = GameState.LeftBottom;
        _root["Right"].Size = GameState.RightWidth;
        _root["Messages"].Size = GameState.RightTop;
        _root["Inventory"].Size = GameState.RightBottom;
    }
}
