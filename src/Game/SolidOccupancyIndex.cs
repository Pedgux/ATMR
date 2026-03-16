namespace ATMR.Game;

using Arch.Core;
using ATMR.Components;

/// <summary>
/// Tracks solid occupancy per cell and supports constant-time collision checks.
/// </summary>
public sealed class SolidOccupancyIndex
{
    private readonly Dictionary<int, int> _cellCounts = new();
    private readonly Dictionary<Entity, int> _entityCells = new();
    private bool _initialized;
    private int _worldId = -1;
    private int _gridWidth;

    public void EnsureInitialized(World world, int gridWidth)
    {
        if (_initialized && _worldId == world.Id && _gridWidth == gridWidth)
        {
            return;
        }

        _cellCounts.Clear();
        _entityCells.Clear();

        _worldId = world.Id;
        _gridWidth = gridWidth;
        _initialized = true;

        var solids = new QueryDescription().WithAll<Position, Solid>();
        world.Query(
            in solids,
            (Entity entity, ref Position position, ref Solid solid) =>
            {
                RegisterSolid(entity, position.X, position.Y);
            }
        );
    }

    public bool IsOccupied(int x, int y)
    {
        int key = ToCellKey(x, y);
        return _cellCounts.ContainsKey(key);
    }

    public void RegisterSolid(Entity entity, int x, int y)
    {
        int key = ToCellKey(x, y);
        _entityCells[entity] = key;

        if (_cellCounts.TryGetValue(key, out int existing))
        {
            _cellCounts[key] = existing + 1;
            return;
        }

        _cellCounts[key] = 1;
    }

    public void UnregisterSolid(Entity entity)
    {
        if (!_entityCells.TryGetValue(entity, out int key))
        {
            return;
        }

        DecrementCellCount(key);
        _entityCells.Remove(entity);
    }

    public bool TryMoveSolid(Entity entity, int fromX, int fromY, int toX, int toY)
    {
        int fromKey = ToCellKey(fromX, fromY);
        int toKey = ToCellKey(toX, toY);

        if (!_entityCells.TryGetValue(entity, out int trackedFromKey))
        {
            RegisterSolid(entity, fromX, fromY);
            trackedFromKey = fromKey;
        }

        if (trackedFromKey != fromKey)
        {
            DecrementCellCount(trackedFromKey);
            _entityCells[entity] = fromKey;
            if (_cellCounts.TryGetValue(fromKey, out int existingFrom))
            {
                _cellCounts[fromKey] = existingFrom + 1;
            }
            else
            {
                _cellCounts[fromKey] = 1;
            }
        }

        if (fromKey == toKey)
        {
            return true;
        }

        if (IsOccupiedByAnother(entity, toKey))
        {
            return false;
        }

        DecrementCellCount(fromKey);

        if (_cellCounts.TryGetValue(toKey, out int destinationCount))
        {
            _cellCounts[toKey] = destinationCount + 1;
        }
        else
        {
            _cellCounts[toKey] = 1;
        }

        _entityCells[entity] = toKey;
        return true;
    }

    private bool IsOccupiedByAnother(Entity entity, int key)
    {
        if (!_cellCounts.TryGetValue(key, out int count))
        {
            return false;
        }

        if (!_entityCells.TryGetValue(entity, out int ownKey))
        {
            return count > 0;
        }

        if (ownKey != key)
        {
            return count > 0;
        }

        return count > 1;
    }

    private void DecrementCellCount(int key)
    {
        if (!_cellCounts.TryGetValue(key, out int count))
        {
            return;
        }

        if (count <= 1)
        {
            _cellCounts.Remove(key);
            return;
        }

        _cellCounts[key] = count - 1;
    }

    private int ToCellKey(int x, int y)
    {
        return y * _gridWidth + x;
    }
}
