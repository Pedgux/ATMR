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

    // work in progress, idea is to later put strings there like:
    // $"str: {int}", or ping for example $"ping: {int} ms"
    // in a dynamic way, so it's automatically slotted onto the stats panel.
    // Instead of manually putting them in the code, allowing more customization and eases the whole thing.
    // But uhh future me problem, don't even have stats yet. Just ideas.
    // also gotta figure out how to detach the {var} part from the string.
    public static List<string> StatEntries = [];

    // Constructor: capture the layout child. Live will be started at program-level.
    public Stats()
    {
        _statsPanel = new Panel("") { Expand = true };
        _statsWindow = GameState.Ui.RootLayout["Stats"];
        _statsWindow.Update(_statsPanel);
    }

    // todo / reminder:
    // Use breakdownchart to represent player health, easily visualizing:
    // incoming damage (poison, fire),
    // different types of health (armor, shields)

    // update the panel, plz work
    public void RefreshPanel()
    {
        if (GameState.PingList.Count > 10)
        {
            List<long> tempList = GameState.PingList;
            tempList.Sort();
            var panel = new Panel(new Markup($"Median ping: {tempList[10]} ms")) { Expand = true };
            _statsWindow.Update(panel);
        }
    }
}
