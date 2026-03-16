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
    public int GridWidth;

    // how tf do I calculate this
    public int GridHeight;

    // Contains the original generated terrain so moving entities can restore tiles.
    private string[] _baseGrid;
    private string[] _grid;

    public static DeterministicRng GridRng = new DeterministicRng(
        Hasher.Hash(Program.runSeed + 1111)
    );

    public Grid()
    {
        GameState.MessageWindow.Write($"grid rng: {Program.runSeed + 1111}");
        GridWidth = 100;
        GridHeight = 100;

        /*
        GridWidth = 75;
        GridHeight = 31;
        */

        _baseGrid = new string[GridWidth * GridHeight];
        _grid = new string[GridWidth * GridHeight];
        for (int i = 0; i < _grid.Length; i++)
        {
            if (GridRng.Range(1, 100) < 40)
            {
                if (GridRng.Range(1, 4) == 1)
                {
                    _baseGrid[i] = "[green]#[/]";
                }
                else
                {
                    GameState.Level0.World.Create(
                        new Position(i % GridWidth, i / GridWidth),
                        new Glyph('#', "[red]"),
                        new Solid()
                    );
                }
            }
            else
            {
                _baseGrid[i] = ".";
            }

            _grid[i] = _baseGrid[i];
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
                    int top = Math.Clamp(position.Y - camera.firstHeightHalf, 0, GridHeight);
                    int bottom = Math.Clamp(position.Y + camera.secondHeightHalf, 0, GridHeight);
                    int left = Math.Clamp(position.X - camera.firstWidthHalf, 0, GridWidth);
                    int right = Math.Clamp(position.X + camera.secondWidthHalf, 0, GridWidth);

                    //ööh fix
                    if (top == 0)
                    {
                        bottom += (position.Y - camera.firstHeightHalf) * -1;
                    }
                    if (bottom == GridHeight)
                    {
                        top -= position.Y + camera.secondHeightHalf - GridHeight;
                    }

                    if (left == 0)
                    {
                        right += (position.X - camera.firstWidthHalf) * -1;
                    }
                    if (right == GridWidth)
                    {
                        left -= position.X + camera.secondWidthHalf - GridWidth;
                    }

                    for (int i = top; i < bottom; i++)
                    {
                        //GameState.MessageWindow.Write($"{top} ja sit bottom {bottom}");
                        for (int j = left; j < right; j++)
                        {
                            //GameState.MessageWindow.Write($"{left} ja sit right {right}");
                            int idx = i * GridWidth + j;
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
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
        {
            return;
        }

        int idx = y * GridWidth + x;
        _grid[idx] = thing;
    }

    public void RestoreBaseTile(int x, int y)
    {
        if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
        {
            return;
        }

        int idx = y * GridWidth + x;
        _grid[idx] = _baseGrid[idx];
    }
}
