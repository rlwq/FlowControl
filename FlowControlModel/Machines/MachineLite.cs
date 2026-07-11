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

    /// <summary>
    /// Power units this machine draws from its electric network every tick.
    /// Zero means the machine works without electricity.
    /// </summary>
    public readonly float PowerDemand;

    /// <summary> Electric pole parameters, or <c>null</c> for machines that are not poles. </summary>
    public readonly PoleSpec? Pole;

    /// <param name="kind"> Machine type's unique identifier. </param>
    /// <param name="dimensions"> An integer vector representing the
    /// dimensions (width and height) of the machine type. </param>
    /// <param name="invDims"> Dimensions of the machine's inventory. </param>
    /// <param name="playerBuildable"> Whether players may build this machine. </param>
    /// <param name="indestructible"> Whether the machine resists removal by players. </param>
    /// <param name="powerDemand"> Power drawn from the electric network every tick. </param>
    /// <param name="pole"> Electric pole parameters, for pole machines. </param>
    public MachineLite(
        string kind, Vec2I dimensions, InventoryDimensions? invDims = null,
        bool playerBuildable = true, bool indestructible = false,
        float powerDemand = 0f, PoleSpec? pole = null)
    {
        Kind = kind;
        Dimensions = dimensions;
        InventoryDimensions = invDims ?? InventoryDimensions.Empty;
        PlayerBuildable = playerBuildable;
        Indestructible = indestructible;
        PowerDemand = powerDemand;
        Pole = pole;
    }
}

/// <summary> Electric pole parameters of a machine kind. </summary>
/// <param name="WireReach"> Poles whose centers are within this distance connect into one network. </param>
/// <param name="SupplyRadius"> Machines whose footprint lies within this distance of the pole's center are powered by its network. </param>
public sealed record PoleSpec(float WireReach, float SupplyRadius);
