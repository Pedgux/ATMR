using Arch.Core;
using ATMR.Components;
using ATMR.Game;

namespace ATMR.Systems;

/// <summary>
/// Moves entities either by a velocity or a teleport.
/// </summary>
public static class MovementSystem
{
    public static MovementOutcome Run(World world)
    {
        int moves = 0;
        int blockedMoves = 0;

        var movables = new QueryDescription().WithAll<Position, Velocity>();
        var teleportables = new QueryDescription().WithAll<Position, Teleport>();
        GameState.SolidOccupancy.EnsureInitialized(world, GameState.GridWindow.GridWidth);

        world.Query(
            in movables,
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                if (vel.X == 0 && vel.Y == 0)
                    return;

                moves++;

                int nextX = pos.X + vel.X;
                int nextY = pos.Y + vel.Y;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canMove = isSolid
                    ? GameState.SolidOccupancy.TryMoveSolid(entity, pos.X, pos.Y, nextX, nextY)
                    : !GameState.SolidOccupancy.IsOccupied(nextX, nextY);

                if (!canMove)
                {
                    blockedMoves++;
                    vel.X = 0;
                    vel.Y = 0;
                    return;
                }

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                // move the entity with velocity
                pos.X = nextX;
                pos.Y = nextY;
                // reset velocity
                vel.X = 0;
                vel.Y = 0;
            }
        );

        world.Query(
            in teleportables,
            (Entity entity, ref Position pos, ref Teleport tp) =>
            {
                if (tp.X == 0 && tp.Y == 0)
                    return;

                moves++;

                bool isSolid = world.Has<Position, Solid>(entity);
                bool canTeleport = isSolid
                    ? GameState.SolidOccupancy.TryMoveSolid(entity, pos.X, pos.Y, tp.X, tp.Y)
                    : !GameState.SolidOccupancy.IsOccupied(tp.X, tp.Y);

                if (!canTeleport)
                {
                    blockedMoves++;
                    tp.X = 0;
                    tp.Y = 0;
                    return;
                }

                // replace the last cell the entity was in, so no duplicates appear
                GameState.GridWindow.RestoreBaseTile(pos.X, pos.Y);

                // teleport em
                pos.X = tp.X;
                pos.Y = tp.Y;

                tp.X = 0;
                tp.Y = 0;
            }
        );

        return new MovementOutcome(moves, blockedMoves);
    }
}

public readonly record struct MovementOutcome(int Moves, int BlockedMoves)
{
    public bool HasActionableMoves => Moves > 0;

    public bool AllActionableMovesBlocked => HasActionableMoves && BlockedMoves == Moves;
}
