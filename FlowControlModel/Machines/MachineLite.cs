using Godot;
using FlowControlModel.Inventories;

namespace FlowControlModel.Machines;

/// <summary>
/// Represents a machine type. Contains all intrinsic properties of a machine type.
/// </summary>
public class MachineLite
{
    /// <summary> Machine type's unique identifier. </summary>
    public readonly StringName Kind;

    /// <summary> An integer vector representing the dimensions (width and height) of the machine type. </summary>
    public readonly Vector2I Dimensions;
    
    /// <summary> A structure representing count of slots in machine's inventory. </summary>
    public readonly InventoryDimensions InventoryDimensions;

    /// <param name="kind"> Machine type's unique identifier. </param>
    /// <param name="dimensions"> An integer vector representing the
    /// dimensions (width and height) of the machine type. </param>
    /// <param name="invDims"> Dimensions of the machine's inventory. </param>
    public MachineLite(StringName kind, Vector2I dimensions, InventoryDimensions? invDims = null)
    {
        Kind = kind;
        Dimensions = dimensions;
        InventoryDimensions = invDims ?? InventoryDimensions.Empty;
    }
}
