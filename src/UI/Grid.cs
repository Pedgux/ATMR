namespace ATMR.UI;

using Arch.Core;
using ATMR.Components;
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
        _gridWidth = 2000;
        _gridHeight = 2000;

        /*
        _gridWidth = 75;
        _gridHeight = 31;
        */

        _grid = new string[_gridWidth * _gridHeight];
        for (int i = 0; i < _grid.Length; i++)
        {
            _grid[i] = ".";
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
            var query = new QueryDescription().WithAll<Camera, Position>();

            GameState.Level0.World.Query(
                in query,
                (Entity entity, ref Camera camera, ref Position position) =>
                {
                    // clamp to 0
                    int top = Math.Max(position.Y - camera.firstHeightHalf, 0);
                    int bottom = Math.Max(position.Y + camera.secondHeightHalf, 0);
                    int left = Math.Max(position.X - camera.firstWidthHalf, 0);
                    int right = Math.Max(position.X + camera.secondWidthHalf, 0);

                    for (int i = top; i < bottom; i++)
                    {
                        for (int j = left; j < right; j++)
                        {
                            int idx = i * GameState.CameraWidth + j;
                            gridString += _grid[idx];
                        }
                        gridString += "\n";
                    }
                }
            );

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
