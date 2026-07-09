using FlowControlModel.Inventories;

namespace FlowControlModel.Entities;

/// <summary>
/// Represents an entity type. Contains all intrinsic properties of an entity type.
/// </summary>
/// <param name="kind"> Entity type's unique identifier. </param>
/// <param name="boxSize"> A vector representing the collison box size (width and height) in grid cells of the entity type. </param>
/// <param name="invDims"> Dimensions of the entity's inventory. Empty when not provided. </param>
public class EntityLite(string kind, Vec2 boxSize, InventoryDimensions? invDims = null)
{
    /// <summary> Entity type's unique identifier. </summary>
    public readonly string Kind = kind;

    /// <summary> A vector representing the collison box size (width and height) in grid cells of the entity type. </summary>
    public readonly Vec2 BoxSize = boxSize;

    /// <summary> A structure representing count of slots in the entity's inventory. </summary>
    public readonly InventoryDimensions InventoryDimensions = invDims ?? InventoryDimensions.Empty;
}
