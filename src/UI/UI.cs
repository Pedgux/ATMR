namespace ATMR.UI;

using Spectre.Console;

public sealed class UI
{
    private Layout _root = new();

    // Public accessor for other parts of the app to query the layout.
    public Layout RootLayout => _root;

    public UI()
    {
        Initialize();
        UiState.RootUI = this;
    }

    public void Initialize()
    {
        // Create the UI
        _root = new Layout("Root").SplitColumns(
            new Layout("Left")
                .Ratio(3)
                .Size(UiState.leftSize)
                .SplitRows(
                    new Layout("Game").MinimumSize(20).Ratio(4),
                    new Layout("Stats").MinimumSize(5).Ratio(1)
                ),
            new Layout("Right")
                .Ratio(2)
                .Size(UiState.rightSize)
                .SplitRows(new Layout("Messages").Size(7), new Layout("Inventory").Ratio(10))
        );

        /*
        // Stats have all players? Potential idea
        int playerCount = 2;
        var statsTable = new Table { Expand = true };
        for (int i = 1; i <= playerCount; i++)
            statsTable.AddColumn($"p{i}");

        _root["Stats"].Update(statsTable);
        */
    }
}
