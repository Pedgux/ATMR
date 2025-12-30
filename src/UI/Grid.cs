namespace ATMR.UI;

using ATMR.Game;
using Spectre.Console;

/// <summary>
/// Contains the grid (Game Window), and methods relating it.
/// </summary>
public sealed class Grid
{
    private Panel _gridPanel;
    private readonly Layout _gridWindow;

    // The width of the window, -2 for borders
    private int _gridWidth;

    // how tf do I calculate this
    private int _gridHeight;

    private string[] _grid;

    public Grid()
    {
        _gridWidth = GameState.LeftWidth - 2 - 2;
        _gridHeight = GameState.LeftTop - 2;

        /*
        _gridWidth = 75;
        _gridHeight = 31;
        */

        _grid = new string[_gridWidth * _gridHeight];
        for (int i = 0; i < _grid.Length; i++)
        {
            _grid[i] = " ";
        }
        string gridString = GridToString();

        _gridPanel = new Panel(gridString) { Expand = true };
        _gridWindow = GameState.Ui.RootLayout["Grid"];
        _gridWindow.Update(_gridPanel);

        GameState.MessageWindow.Write($"window size {_gridWindow.Size}");
        GameState.MessageWindow.Write($"grid length {_grid.Length}");
        GameState.MessageWindow.Write(
            $"grid width {GameState.LeftWidth - 2} grid height{GameState.LeftTop - 2}"
        );
    }

    public void RefreshPanel()
    {
        _gridPanel = new Panel(GridToString()) { Expand = true };
        _gridWindow.Update(_gridPanel);
    }

    private string GridToString()
    {
        lock (_grid)
        {
            string gridString = string.Empty;

            for (int i = 0; i < _gridHeight; i++)
            {
                for (int j = 0; j < _gridWidth; j++)
                {
                    int idx = i * _gridWidth + j;
                    gridString += _grid[idx];
                }
                gridString += "\n";
            }
            return gridString;
        }
    }

    public void SetGridCell(int x, int y, string thing)
    {
        if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
        {
            return;
        }

        int idx = y * _gridWidth + x;
        _grid[idx] = thing;
    }
}
