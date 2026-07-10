using FlowControlModel.Inventories;

namespace FlowControlModel.World;

/// <summary> Represents a ground type. Contains all intrinsic properties of a ground type.</summary>
/// <param name="kind">Ground type's unique identifier.</param>
/// <param name="passable">Whether entities can walk on (and machines stand on) this ground.</param>
/// <param name="speedModifier">Movement speed multiplier for entities standing on this ground.</param>
public class GroundLite(string kind, bool passable = true, float speedModifier = 1f)
{
    /// <summary> Ground tile's unique identifier. </summary>
    public readonly string Kind = kind;

    /// <summary> Whether entities can walk on (and machines stand on) this ground (e.g. water is not). </summary>
    public readonly bool Passable = passable;

    /// <summary> Movement speed multiplier for entities standing on this ground (stone paths are faster). </summary>
    public readonly float SpeedModifier = speedModifier;

    /// <summary>
    /// If set, every tile of this ground periodically spawns items on itself (e.g. ore deposits).
    /// Assigned by the registry builder when the ground is registered with a spawner.
    /// </summary>
    public ItemSpawner? Spawner { get; internal set; }
}

/// <summary> Periodic ground-item spawning behavior of a ground type. </summary>
/// <param name="item"> The item type spawned by the tile. </param>
/// <param name="periodTicks"> How many ticks pass between two spawns of one tile. </param>
public class ItemSpawner(ItemLite item, int periodTicks)
{
    /// <summary> The item type spawned by the tile. </summary>
    public readonly ItemLite Item = item;

    /// <summary> How many ticks pass between two spawns of one tile. </summary>
    public readonly int PeriodTicks = periodTicks;
}
