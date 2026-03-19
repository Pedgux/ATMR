namespace ATMR.UI;

using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;
using ATMR.Systems;
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
        GridWidth = 100;
        GridHeight = 100;

        /*
        GridWidth = 75;
        GridHeight = 31;
        */

        _baseGrid = new string[GridWidth * GridHeight];
        _grid = new string[GridWidth * GridHeight];
        CollisionSystem.Initialize(GridWidth, GridHeight);
        for (int i = 0; i < _grid.Length; i++)
        {
            if (GridRng.Range(1, 100) < 60)
            {
                if (GridRng.Range(1, 4) == 1)
                {
                    _baseGrid[i] = "[green]#[/]";
                }
                else
                {
                    // Even when a wall entity is spawned here, the terrain under it is floor.
                    // This ensures destroy/move restore can always fall back to '.' cleanly.
                    _baseGrid[i] = ".";
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

        Log.Write($"window size {_gridWindow.Size}");
        Log.Write($"grid length {_grid.Length}");
        Log.Write($"grid width {GameState.LeftWidth - 2} grid height{GameState.LeftTop - 2}");
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
                    int top = Math.Clamp(position.Y - camera.FirstHeightHalf, 0, GridHeight);
                    int bottom = Math.Clamp(position.Y + camera.SecondHeightHalf, 0, GridHeight);
                    int left = Math.Clamp(position.X - camera.FirstWidthHalf, 0, GridWidth);
                    int right = Math.Clamp(position.X + camera.SecondWidthHalf, 0, GridWidth);

                    //ööh fix
                    if (top == 0)
                    {
                        bottom += (position.Y - camera.FirstHeightHalf) * -1;
                    }
                    if (bottom == GridHeight)
                    {
                        top -= position.Y + camera.SecondHeightHalf - GridHeight;
                    }
                    if (left == 0)
                    {
                        right += (position.X - camera.FirstWidthHalf) * -1;
                    }
                    if (right == GridWidth)
                    {
                        left -= position.X + camera.SecondWidthHalf - GridWidth;
                    }

                    for (int i = top; i < bottom; i++)
                    {
                        //Log.Write($"{top} ja sit bottom {bottom}");
                        for (int j = left; j < right; j++)
                        {
                            //Log.Write($"{left} ja sit right {right}");
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
        // Fallback protects against accidental null base tiles, preventing broken render cells.
        _grid[idx] = _baseGrid[idx] ?? ".";
    }
}
