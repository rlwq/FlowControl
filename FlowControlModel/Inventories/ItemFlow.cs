using FlowControlModel.Factories;
using FlowControlModel.World;

namespace FlowControlModel.Inventories;

/// <summary>
/// Shared item-movement primitives used by the capability APIs.
/// Items are moved, never created: whatever the target rejects returns to the source.
/// </summary>
internal static class ItemFlow
{
    /// <summary>
    /// Moves up to <paramref name="count"/> items between two inventories: of one kind,
    /// or of any kind when <paramref name="itemLite"/> is <c>null</c>.
    /// Returns the number actually moved.
    /// </summary>
    public static int Move(Inventory from, Inventory to, int count, ItemLite? itemLite)
    {
        var stack = itemLite == null
            ? from.Extract(count)
            : from.ExtractItem(new ItemStack(count, itemLite));
        if (stack.IsEmpty)
            return 0;

        var leftover = to.InsertItem(stack);
        if (!leftover.IsEmpty)
            from.InsertItem(leftover);
        return stack.Count - leftover.Count;
    }

    /// <summary>
    /// Picks up ground items within <paramref name="radius"/> of a point into an inventory:
    /// of one kind, or any. Whatever does not fit stays on the ground.
    /// Returns the number of items collected.
    /// </summary>
    public static int PickUp(
        ChunkManager chunkManager, GameObjectFactory factory,
        Inventory into, Vec2 center, float radius, string? itemKind)
    {
        var collected = 0;
        foreach (var item in chunkManager.FindGroundItemsNear(center, radius))
        {
            if (itemKind != null && item.Stack.Lite.Kind != itemKind)
                continue;

            var leftover = into.InsertItem(item.Stack);
            collected += item.Stack.Count - leftover.Count;
            chunkManager.RemoveGroundItem(item);
            if (!leftover.IsEmpty)
                chunkManager.PlaceGroundItem(factory.CreateGroundItem(leftover, item.Coord));
        }
        return collected;
    }

    /// <summary>
    /// Extracts up to <paramref name="count"/> items of a kind from an inventory and drops
    /// them on the ground at the specified point. Returns the number actually dropped.
    /// </summary>
    public static int Drop(
        ChunkManager chunkManager, GameObjectFactory factory,
        Inventory from, ItemLite itemLite, int count, Vec2 coord)
    {
        var stack = from.ExtractItem(new ItemStack(count, itemLite));
        if (stack.IsEmpty)
            return 0;

        chunkManager.PlaceGroundItem(factory.CreateGroundItem(stack, coord));
        return stack.Count;
    }
}
