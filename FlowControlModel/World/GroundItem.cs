using FlowControlModel.Inventories;

namespace FlowControlModel.World;

/// <summary>
/// A read-only view of an item stack lying on the ground.
/// Ground items are immutable: partial pickup replaces the stack with a new one.
/// </summary>
public interface IGroundItem
{
    /// <summary> Ground item's unique identifier. </summary>
    uint Id { get; }

    /// <summary> Global position of the stack (center). </summary>
    Vec2 Coord { get; }

    /// <summary> The items lying here. </summary>
    ItemStack Stack { get; }
}

/// <summary> An item stack lying on the ground. </summary>
internal class GroundItem(uint id, Vec2 coord, ItemStack stack) : IGroundItem
{
    /// <summary> Ground item's unique identifier. </summary>
    public uint Id { get; } = id;

    /// <summary> Global position of the stack (center). </summary>
    public Vec2 Coord { get; } = coord;

    /// <summary> The items lying here. </summary>
    public ItemStack Stack { get; } = stack;
}
