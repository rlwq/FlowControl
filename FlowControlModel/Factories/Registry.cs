using System.Collections.Generic;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Factories;

/// <summary>
/// Registry of all machine, ground, entity and item types.
/// A pure type catalog: it answers "what kinds exist and what are their intrinsic properties".
/// It is used to build new game objects which will be placed in the world later.
/// </summary>
public partial class Registry
{
    private readonly Dictionary<string, MachineLite> _machineLites = [];
    private readonly Dictionary<string, MachineLogic> _machineLogics = [];
    private readonly Dictionary<string, Vec2I[]> _machineLogicObserverOffsets = [];

    private readonly Dictionary<string, GroundLite> _groundLites = [];
    private readonly Dictionary<string, EntityLite> _entityLites = [];
    private readonly Dictionary<string, EntityLogic> _entityLogics = [];

    private readonly Dictionary<string, ItemLite> _itemLites = [];

    /// <summary> Retrieves a <see cref="GroundLite"/> by its <paramref name="kind"/> name. </summary>
    public GroundLite GetGroundLite(string kind) => _groundLites[kind];

    /// <summary> Retrieves a <see cref="MachineLite"/> by its <paramref name="kind"/> name. </summary>
    public MachineLite GetMachineLite(string kind) => _machineLites[kind];

    /// <summary>
    /// Retrieves a <see cref="MachineLogic"/> prototype. It must be copied and not used directly.
    /// </summary>
    internal MachineLogic GetMachineLogic(string kind) => _machineLogics[kind];

    /// <summary> <c>True</c> if the machine logic can interact with other machines. </summary>
    internal bool IsMachineInteractive(string kind) => _machineLogicObserverOffsets.ContainsKey(kind);

    /// <summary>
    /// If the machine is interactive, it retrieves offsets to the cells it interacts with.
    /// The offsets are relative to an unrotated machine; rotate them with
    /// <see cref="RotationM.RotateOffset"/> when the machine is rotated.
    /// </summary>
    internal IEnumerable<Vec2I> GetMachineObserverOffsets(string kind) => _machineLogicObserverOffsets[kind];

    /// <summary> Retrieves a <see cref="EntityLite"/> by its <paramref name="kind"/> name. </summary>
    public EntityLite GetEntityLite(string kind) => _entityLites[kind];

    /// <summary>
    /// Retrieves an <see cref="EntityLogic"/> prototype, or <c>null</c> for kinds without
    /// autonomous behavior. It must be copied and not used directly.
    /// </summary>
    internal EntityLogic? FindEntityLogic(string kind) => _entityLogics.GetValueOrDefault(kind);

    /// <summary> Retrieves specified item kind's <see cref="ItemLite"/>. </summary>
    public ItemLite GetItemLite(string kind) => _itemLites[kind];
}
