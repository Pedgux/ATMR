namespace ATMR.UI;

using System.Collections.Generic;
using System.Linq;
using ATMR.Helpers;
using Spectre.Console;

/// <summary>
/// I wonder what.
/// </summary>
public sealed class Stats
{
    private Panel _statsPanel;
    private readonly Layout _statsWindow;

    // Constructor: capture the layout child. Live will be started at program-level.
    public Stats()
    {
        _statsPanel = new Panel("") { Expand = true };
        _statsWindow = UiState.Ui.RootLayout["Stats"];
        _statsWindow.Update(_statsPanel);
    }

    // todo / reminder:
    // Use breakdownchart to represent player health, easily visualizing:
    // incoming damage (poison, fire),
    // different types of health (armor, shields)
}
