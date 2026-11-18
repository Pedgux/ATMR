namespace ATMR.ECS;

using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

public struct Wall
{
    public char Symbol;
    public int hp;
}

public class SpatialGrid
{
    private readonly int _width,
        _height;
    private Entity[] _occupancy;
    public Wall[] WallOccupancy;

    // maybe item holding here too? Dictionary? GetItemsAt(x, y)?

    public Wall[] CaptureWalls() => (Wall[])WallOccupancy.Clone();

    public void ApplyWalls(Wall[] walls)
    {
        if (walls.Length != WallOccupancy.Length)
            return;
        Array.Copy(walls, WallOccupancy, walls.Length);
    }

    public SpatialGrid(int width, int height)
    {
        _width = width;
        _height = height;
        _occupancy = new Entity[width * height];
        WallOccupancy = new Wall[width * height];
    }

    public void Rebuild(World world)
    {
        Array.Clear(_occupancy);
        var entities = new QueryDescription().WithAll<Position, Solid>().WithNone<Destroy>();
        world.Query(
            in entities,
            (Entity entity, ref Position pos) =>
            {
                // out of bounds loggin'
                if (!InBounds(pos.X, pos.Y))
                {
                    Log.Write(
                        $"[red]Solid entity {entity} out of bounds at ({pos.X},{pos.Y}), skipping[/]"
                    );
                    return;
                }
                _occupancy[GetIndex(pos.X, pos.Y)] = entity;
            }
        );
    }

    public bool TryGetEntity(int x, int y, out Entity entity)
    {
        entity = default;

        if (!InBounds(x, y))
            return false;

        entity = _occupancy[GetIndex(x, y)];
        return entity != default;
    }

    public void PlaceWall(int x, int y, Wall wall)
    {
        if (!InBounds(x, y))
        {
            return;
        }

        WallOccupancy[GetIndex(x, y)] = wall;
    }

    public bool DamageWall(int x, int y, int damage = 1)
    {
        if (!InBounds(x, y) || damage <= 0)
        {
            return false;
        }

        int index = GetIndex(x, y);
        ref Wall wall = ref WallOccupancy[index];
        if (wall.Symbol == default)
        {
            return false;
        }

        wall.hp -= damage;
        if (wall.hp <= 0)
        {
            GameState.GridWindow.RestoreBaseTile(x, y);
            wall = default;
        }

        return true;
    }

    /// <summary>
    /// Moves entities in the occupancy grid.
    /// </summary>
    /// <param name="entity">The entity that gets moved</param>
    /// <param name="fromPos">Moving from here</param>
    /// <param name="toPos">Moving to here</param>
    /// <returns>If the operation succeeded or not</returns>
    public bool TryMoveOccupancy(Entity entity, (int x, int y) fromPos, (int x, int y) toPos)
    {
        // TODO: Unit tests for all weird edge cases

        // are the positions valid?
        if (!InBounds(fromPos.x, fromPos.y) || !InBounds(toPos.x, toPos.y))
            return false;

        int fromIndex = GetIndex(fromPos.x, fromPos.y);
        int toIndex = GetIndex(toPos.x, toPos.y);

        if (fromIndex == toIndex && _occupancy[fromIndex] == _occupancy[toIndex])
            return true; // can move into the same spot (wait)

        // pre-emptive checks for if the move is allowed or valid.
        if (_occupancy[fromIndex] != entity)
            return false; // caller's fromPos doesn't match what occupancy actually has

        if (WallOccupancy[toIndex].Symbol != default)
            return false;

        if (_occupancy[toIndex] != default)
            return false; // toPos has something, can't move

        // perform the swap
        _occupancy[toIndex] = entity;
        _occupancy[fromIndex] = default;
        return true;
    }

    public bool IsBlocked(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return true;
        }
        if (_occupancy[GetIndex(x, y)] != default)
        {
            Log.Write("blokattu");
        }
        return _occupancy[GetIndex(x, y)] != default;
    }

    private int GetIndex(int x, int y)
    {
        return y * _width + x;
    }

    public (int, int) GetPos(int index)
    {
        return (index % _width, index / _width);
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    public void RemoveOccupancy(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return;
        }
        _occupancy[GetIndex(x, y)] = default;
    }
}
