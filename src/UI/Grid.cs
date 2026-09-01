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

        GridWidth = Math.Max(100, GameState.CameraWidth);
        GridHeight = Math.Max(0, GameState.CameraHeight);
        //GridWidth = 50;
        //GridHeight = 10;

        /*
        GridWidth = 75;
        GridHeight = 31;
        */

        _baseGrid = new string[GridWidth * GridHeight];
        _grid = new string[GridWidth * GridHeight];
        CollisionSystem.Initialize(GridWidth, GridHeight);
        for (int i = 0; i < _grid.Length; i++)
        {
            if (GridRng.Range(1, 100) < 0)
            {
                if (GridRng.Range(1, 4) != 1)
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
                        new Solid(),
                        new Health(3, 3)
                    );
                }
            }
            else
            {
                _baseGrid[i] = ".";

                // 1% chance to spawn an item on empty floor
                if (GridRng.Range(1, 1000) <= 1000)
                {
                    int itemType = GridRng.Range(1, 4);
                    var pos = new Position(i % GridWidth, i / GridWidth);

                    if (itemType == 6)
                    {
                        GameState.Level0.World.Create(
                            pos,
                            new Glyph('$', "[yellow]"),
                            new Item("Gold Coin", "moneh"),
                            new Stackable(GridRng.Range(1, 15))
                        );
                        GameState.Level0.World.Create(
                            pos,
                            new Glyph('$', "[yellow]"),
                            new Item("Silver coin", "wau"),
                            new Stackable(GridRng.Range(1, 15))
                        );
                    }
                    else if (itemType == 6)
                    {
                        GameState.Level0.World.Create(
                            pos,
                            new Glyph('/', "[silver]"),
                            new Item("Iron Sword", "mieks")
                        );
                    }
                    /*
                    else
                    {
                        GameState.Level0.World.Create(
                            pos,
                            new Glyph('*', "[purple]"),
                            new Item("Gem", "gem"),
                            new Stackable(GridRng.Range(5, 10))
                        );
                        GameState.Level0.World.Create(
                            pos,
                            new Glyph('/', "[silver]"),
                            new Item("Iron Sword", "mieks")
                        );
                    }
                    */
                }
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
        try
        {
            RefreshPanelInternal();
        }
        catch (Exception ex)
        {
            Log.Write($"Grid error: {ex.Message}");
        }
    }

    private void RefreshPanelInternal()
    {
        _gridPanel = new Panel(GridToString()) { Expand = true };
        _gridWindow.Update(_gridPanel);
    }

    private string GridToString()
    {
        lock (_grid)
        {
            var sb = new System.Text.StringBuilder();
            var query = new QueryDescription().WithAll<Camera, Position>();

            GameState.Level0.World.Query(
                in query,
                (Entity entity, ref Camera camera, ref Position position) =>
                {
                    int viewHeight = Math.Clamp(
                        camera.FirstHeightHalf + camera.SecondHeightHalf,
                        1,
                        GridHeight
                    );
                    int viewWidth = Math.Clamp(
                        camera.FirstWidthHalf + camera.SecondWidthHalf,
                        1,
                        GridWidth
                    );

                    int top = Math.Clamp(position.Y - camera.FirstHeightHalf, 0, GridHeight - viewHeight);
                    int left = Math.Clamp(position.X - camera.FirstWidthHalf, 0, GridWidth - viewWidth);

                    int bottom = top + viewHeight;
                    int right = left + viewWidth;

                    for (int i = top; i < bottom; i++)
                    {
                        for (int j = left; j < right; j++)
                        {
                            int idx = i * GridWidth + j;
                            sb.Append(_grid[idx]);
                        }
                        sb.AppendLine();
                    }
                }
            );

            return sb.ToString();
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
