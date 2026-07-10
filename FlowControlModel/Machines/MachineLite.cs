using FlowControlModel.Inventories;

namespace FlowControlModel.Machines;

/// <summary>
/// Represents a machine type. Contains all intrinsic properties of a machine type.
/// </summary>
public class MachineLite
{
    /// <summary> Machine type's unique identifier. </summary>
    public readonly string Kind;

    /// <summary> An integer vector representing the dimensions (width and height) of the machine type. </summary>
    public readonly Vec2I Dimensions;
    
    /// <summary> A structure representing count of slots in machine's inventory. </summary>
    public readonly InventoryDimensions InventoryDimensions;

    /// <summary>
    /// Whether players may build this machine. Buildable machines require an item of the
    /// same kind (the build cost); non-buildable ones (e.g. the Hub) are placed only
    /// by system commands and return no item on removal.
    /// </summary>
    public readonly bool PlayerBuildable;

    /// <summary> Whether the machine resists removal by players (e.g. the Hub). </summary>
    public readonly bool Indestructible;

    /// <param name="kind"> Machine type's unique identifier. </param>
    /// <param name="dimensions"> An integer vector representing the
    /// dimensions (width and height) of the machine type. </param>
    /// <param name="invDims"> Dimensions of the machine's inventory. </param>
    /// <param name="playerBuildable"> Whether players may build this machine. </param>
    /// <param name="indestructible"> Whether the machine resists removal by players. </param>
    public MachineLite(
        string kind, Vec2I dimensions, InventoryDimensions? invDims = null,
        bool playerBuildable = true, bool indestructible = false)
    {
        Kind = kind;
        Dimensions = dimensions;
        InventoryDimensions = invDims ?? InventoryDimensions.Empty;
        PlayerBuildable = playerBuildable;
        Indestructible = indestructible;
    }
}
