using System;
using System.Collections.Generic;
using Arch.Core;
using ATMR.Components;
using ATMR.Helpers;

namespace ATMR.Systems;

public static class ItemSystem
{
    private static readonly QueryDescription ItemsAtPosQuery = new QueryDescription().WithAll<
        Item,
        Position
    >();

    public static void ExecutePickup(
        World world,
        Entity playerEntity,
        int amount = -1,
        int itemIndex = 0
    )
    {
        // Ensure the player has an inventory and position
        if (!world.Has<Inventory>(playerEntity) || !world.Has<Position>(playerEntity))
        {
            return;
        }

        ref var inventory = ref world.Get<Inventory>(playerEntity);
        var playerPos = world.Get<Position>(playerEntity);

        // etsitään tavara
        var itemsAtPos = GetItemsAt(world, playerPos);

        if (itemsAtPos.Count == 0 || itemIndex >= itemsAtPos.Count)
        {
            Log.Write("Nothing to pick up here.");
            return;
        }

        Entity selectedEntity = itemsAtPos[itemIndex];

        // handlaa eri tavalla jos stackable vai ei
        if (world.Has<Stackable>(selectedEntity))
        {
            HandleStackablePickup(world, selectedEntity, ref inventory, amount);
        }
        else
        {
            HandleStandardPickup(world, selectedEntity, ref inventory);
        }
    }

    private static void HandleStandardPickup(
        World world,
        Entity itemEntity,
        ref Inventory inventory
    )
    {
        if (inventory.Items.Count >= inventory.Capacity)
        {
            Log.Write("Inventory is full!");
            return;
        }

        // Directly store non-stackable item
        inventory.Items.Add(itemEntity);
        world.Remove<Position, Glyph>(itemEntity);
        Log.Write($"Picked up {world.Get<Item>(itemEntity).Name}.");
    }

    private static void HandleStackablePickup(
        World world,
        Entity floorEntity,
        ref Inventory inventory,
        int requestedAmount
    )
    {
        ref var itemInfo = ref world.Get<Item>(floorEntity);
        ref var floorStack = ref world.Get<Stackable>(floorEntity);

        // Determine amount to pick up (-1 means "all")
        int amountToPick =
            (requestedAmount > 0) ? Math.Min(requestedAmount, floorStack.Count) : floorStack.Count;
        Log.Write($"Picked up {amountToPick}x {itemInfo.Name}.");

        floorStack.Count -= amountToPick;
        bool merged = false;

        // Step 1: Attempt to merge entirely into an existing stack
        foreach (var invEntity in inventory.Items)
        {
            if (world.Has<Stackable>(invEntity) && world.Has<Item>(invEntity))
            {
                var invItemInfo = world.Get<Item>(invEntity);
                if (invItemInfo.Name == itemInfo.Name)
                {
                    ref var invStack = ref world.Get<Stackable>(invEntity);
                    invStack.Count += amountToPick;
                    merged = true;
                    break;
                }
            }
        }

        // Step 2: If no existing stack was found, create a new slot for it
        if (!merged)
        {
            if (inventory.Items.Count < inventory.Capacity)
            {
                if (floorStack.Count > 0)
                {
                    // Leaves part of stack on the floor, duplicate entity to inventory
                    var newInvEntity = world.Create(
                        new Item(itemInfo.Name, itemInfo.Description),
                        new Stackable(amountToPick)
                    );
                    inventory.Items.Add(newInvEntity);
                }
                else
                {
                    // Moves the entire exact floor entity directly into inventory
                    world.Get<Stackable>(floorEntity).Count = amountToPick;
                    inventory.Items.Add(floorEntity);
                    world.Remove<Position, Glyph>(floorEntity);
                }
            }
            else
            {
                // Not enough room -> return to floor
                Log.Write("Inventory is full!");
                floorStack.Count += amountToPick;
            }
        }

        // Step 3: Cleanup empty stacks off the floor
        if (
            floorStack.Count <= 0
            && world.Has<Position>(floorEntity)
            && !world.Has<Destroy>(floorEntity)
        )
        {
            world.Add(floorEntity, new Destroy());
        }
    }

    public static List<Entity> GetItemsAt(World world, Position targetPos)
    {
        var itemsAtPos = new List<Entity>();

        world.Query(
            in ItemsAtPosQuery,
            (Entity itemEntity, ref Position itemPos) =>
            {
                if (itemPos.X == targetPos.X && itemPos.Y == targetPos.Y)
                {
                    itemsAtPos.Add(itemEntity);
                }
            }
        );

        return itemsAtPos;
    }

    public static void ExecuteDrop(World world, Entity playerEntity, int itemIndex)
    {
        // Placeholder for dropping items
        Log.Write($"Dropping item at index {itemIndex} not yet implemented.");
    }
}
