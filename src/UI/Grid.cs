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
        _gridWidth = 100;
        _gridHeight = 100;

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
                    int top = Math.Clamp(position.Y - camera.firstHeightHalf, 0, _gridHeight);
                    int bottom = Math.Clamp(position.Y + camera.secondHeightHalf, 0, _gridHeight);
                    int left = Math.Clamp(position.X - camera.firstWidthHalf, 0, _gridWidth);
                    int right = Math.Clamp(position.X + camera.secondWidthHalf, 0, _gridWidth);

                    //ööh fix
                    if (top == 0)
                    {
                        bottom += (position.Y - camera.firstHeightHalf) * -1;
                    }
                    if (bottom == _gridHeight)
                    {
                        top -= position.Y + camera.secondHeightHalf - _gridHeight;
                    }

                    if (left == 0)
                    {
                        right += (position.X - camera.firstWidthHalf) * -1;
                    }
                    if (right == _gridWidth)
                    {
                        left -= position.X + camera.secondWidthHalf - _gridWidth;
                    }

                    for (int i = top; i < bottom; i++)
                    {
                        GameState.MessageWindow.Write($"{top} ja sit bottom {bottom}");
                        for (int j = left; j < right; j++)
                        {
                            GameState.MessageWindow.Write($"{left} ja sit right {right}");
                            int idx = i * _gridWidth + j;
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
