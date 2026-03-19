using System;
using Arch.Core;
using ATMR.Components;
using ATMR.Game;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class CollisionSystem
{
    private static int _width;
    private static int _height;
    private static Entity[] _occupancy = Array.Empty<Entity>();

    public static void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _occupancy = new Entity[width * height];
    }

    public static bool IsInitialized => _occupancy.Length > 0;

    public static bool TryGetOccupant(int x, int y, out Entity occupant)
    {
        occupant = default;
        if (!IsInitialized || !InBounds(x, y))
        {
            return false;
        }

        occupant = _occupancy[ToIndex(x, y)];
        return occupant != default;
    }

    public static bool IsBlocked(int x, int y)
    {
        if (!IsInitialized || !InBounds(x, y))
        {
            return true;
        }

        return _occupancy[ToIndex(x, y)] != default;
    }

    public static bool TryMoveSolid(Entity entity, int fromX, int fromY, int toX, int toY)
    {
        // jos ei ole tehty tai ouf of bounds niin false tietysti
        if (!IsInitialized || !InBounds(fromX, fromY) || !InBounds(toX, toY))
        {
            return false;
        }

        // tästä tuonne indexit
        int fromIndex = ToIndex(fromX, fromY);
        int toIndex = ToIndex(toX, toY);

        // jos liikkuu samaan kohtaan jossa on jo, niin saa. (key 5 wait toimii myös)
        if (fromIndex == toIndex)
        {
            return true;
        }

        // tänne pyritään (CollisionSystem arrayssa)
        var destination = _occupancy[toIndex];
        // jos siihen yrittää päästä joku muu, ei itse pääse
        if (destination != default && destination != entity)
        {
            return false;
        }
        // korjaa edellisen ruudun tyhjäksi
        if (_occupancy[fromIndex] == entity)
        {
            _occupancy[fromIndex] = default;
        }
        // ASETTAA sen
        _occupancy[toIndex] = entity;
        return true;
    }

    private static bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }

    private static int ToIndex(int x, int y)
    {
        return y * _width + x;
    }

    public static void RemoveOccupancy(int x, int y)
    {
        Log.Write("thotaan tuhotaan tuhotaan tuhotaan");
        if (!IsInitialized || !InBounds(x, y))
        {
            return;
        }

        _occupancy[ToIndex(x, y)] = default;
    }

    public static void Run(World world)
    {
        if (!IsInitialized)
        {
            return;
        }

        Array.Fill(_occupancy, default);

        var collidables = new QueryDescription().WithAll<Position, Solid>();

        world.Query(
            in collidables,
            (Entity entity, ref Position position) =>
            {
                if (!InBounds(position.X, position.Y))
                {
                    return;
                }

                int index = ToIndex(position.X, position.Y);
                var current = _occupancy[index];
                if (current == default || entity.Id < current.Id)
                {
                    _occupancy[index] = entity;
                }
            }
        );
    }
}
