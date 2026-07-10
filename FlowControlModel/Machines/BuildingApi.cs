using System;
using System.Collections.Generic;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Machines;

/// <summary>
/// The capability surface a building acts through: its own inventory, ports to observed
/// neighbor cells, ground items and world queries around itself. One layer for everyone —
/// built-in <see cref="MachineLogic"/>s run on it today, and the future in-game scripting
/// (<c>using</c>) will resolve the very same object.
/// </summary>
/// <remarks>
/// Two rules are enforced inside, so no logic (or player script) can break them:
/// <list type="bullet">
/// <item><b>No conjuring:</b> every item transfer passes through the building's own
/// inventory — ports pull/push, the ground gets drops from it and pick-ups into it.
/// Items are never created or destroyed by the API.</item>
/// <item><b>Bounded reach:</b> world queries and ground interactions are limited to
/// <see cref="MaxRange"/> cells around the building's footprint and throw beyond it.</item>
/// </list>
/// All operations are deterministic: no hidden randomness, no wall-clock.
/// </remarks>
public interface IBuildingApi
{
    /// <summary> How far (in cells from the footprint) the building can query and reach items. </summary>
    public const float MaxRange = 3f;

    /// <summary> Read-only view of the building itself. </summary>
    IMachine Machine { get; }

    /// <summary> Number of ports (observed cells registered with the machine's logic). </summary>
    int PortCount { get; }

    /// <summary> The port with the specified index, in registration order. </summary>
    IBuildingPort Port(int index);

    /// <summary>
    /// Counts items in the building's own inventory: of one kind,
    /// or of any kind when <paramref name="itemKind"/> is <c>null</c>.
    /// </summary>
    int CountItems(string? itemKind = null);

    /// <summary>
    /// Picks up ground items lying within <paramref name="radius"/> cells of the building
    /// (clamped to <see cref="MaxRange"/>) into its own inventory: of one kind, or any.
    /// Returns the number of items collected; whatever does not fit stays on the ground.
    /// </summary>
    int PickUpItems(float radius, string? itemKind = null);

    /// <summary>
    /// Drops up to <paramref name="count"/> items of a kind from the building's own
    /// inventory onto the ground at an offset from the footprint center
    /// (bounded by <see cref="MaxRange"/>). Returns the number actually dropped.
    /// </summary>
    int DropItems(string itemKind, int count, Vec2 offset);

    /// <summary>
    /// The ground kind at <paramref name="offset"/> from the building's origin.
    /// The cell must lie within <see cref="MaxRange"/> of the footprint.
    /// </summary>
    string GroundKindAt(Vec2I offset);

    /// <summary>
    /// Ground items lying within <paramref name="radius"/> cells of the building
    /// (clamped to <see cref="MaxRange"/>), for inspection.
    /// </summary>
    IReadOnlyList<IGroundItem> GroundItemsNear(float radius);
}

/// <summary>
/// A capability to interact with one observed cell: inspect the machine standing there
/// and move items between it and the owning building's inventory.
/// </summary>
public interface IBuildingPort
{
    /// <summary> Whether a machine currently occupies the observed cell. </summary>
    bool HasMachine { get; }

    /// <summary> Read-only view of the machine on the observed cell, or <c>null</c>. </summary>
    IMachine? Machine { get; }

    /// <summary>
    /// Moves up to <paramref name="count"/> items from the port's machine into the owning
    /// building's inventory: of one kind, or of any kind when <paramref name="itemKind"/>
    /// is <c>null</c>. Returns the number actually moved.
    /// </summary>
    int Pull(int count, string? itemKind = null);

    /// <summary>
    /// Moves up to <paramref name="count"/> items from the owning building's inventory
    /// into the port's machine: of one kind, or of any kind. Returns the number actually moved.
    /// </summary>
    int Push(int count, string? itemKind = null);
}

/// <summary> The model's implementation of <see cref="IBuildingApi"/>. </summary>
internal sealed class BuildingApi : IBuildingApi, IDisposable
{
    private readonly Registry _registry;
    private readonly ChunkManager _chunkManager;
    private readonly GameObjectFactory _factory;
    private readonly BuildingPort[] _ports;

    private Machine _machine = null!;

    /// <summary> Read-only view of the building itself. </summary>
    public IMachine Machine => _machine;

    /// <summary> Number of ports (observed cells registered with the machine's logic). </summary>
    public int PortCount => _ports.Length;

    public BuildingApi(
        Registry registry, ChunkManager chunkManager, GameObjectFactory factory,
        CellObserver[] observers)
    {
        _registry = registry;
        _chunkManager = chunkManager;
        _factory = factory;
        _ports = Array.ConvertAll(observers, observer => new BuildingPort(this, observer));
    }

    /// <summary> Attaches the owning machine (called once by the factory). </summary>
    public void Bind(Machine machine) => _machine = machine;

    /// <summary> The port with the specified index, in registration order. </summary>
    public IBuildingPort Port(int index) =>
        index >= 0 && index < _ports.Length
            ? _ports[index]
            : throw new ArgumentOutOfRangeException(
                nameof(index), $"The building has {_ports.Length} ports; there is no port {index}.");

    /// <inheritdoc/>
    public int CountItems(string? itemKind = null) => _machine.Inventory.CountItems(itemKind);

    /// <inheritdoc/>
    public int PickUpItems(float radius, string? itemKind = null)
    {
        radius = Math.Clamp(radius, 0, IBuildingApi.MaxRange);
        return ItemFlow.PickUp(
            _chunkManager, _factory, _machine.Inventory, _machine.Rect.GetCenter(), radius, itemKind);
    }

    /// <inheritdoc/>
    public int DropItems(string itemKind, int count, Vec2 offset)
    {
        RequireInRange(offset.DistanceTo(Vec2.Zero), "drop point");
        return ItemFlow.Drop(
            _chunkManager, _factory, _machine.Inventory,
            _registry.GetItemLite(itemKind), count, _machine.Rect.GetCenter() + offset);
    }

    /// <inheritdoc/>
    public string GroundKindAt(Vec2I offset)
    {
        var cell = _machine.Coord + offset;
        RequireInRange(_machine.Rect.GetCenter().DistanceTo(cell + new Vec2(0.5f, 0.5f)), "cell");
        return _chunkManager.GetGroundLiteAt(cell).Kind;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IGroundItem> GroundItemsNear(float radius)
    {
        radius = Math.Clamp(radius, 0, IBuildingApi.MaxRange);
        return _chunkManager.FindGroundItemsNear(_machine.Rect.GetCenter(), radius);
    }

    /// <summary> Unsubscribes all port observers. </summary>
    public void Dispose()
    {
        foreach (var port in _ports)
            port.Dispose();
    }

    private static void RequireInRange(float distance, string what)
    {
        // Half the diagonal of a 2x2 footprint plus MaxRange covers every legal case;
        // distances are measured from the footprint center
        if (distance > IBuildingApi.MaxRange + 1.5f)
            throw new InvalidOperationException(
                $"The {what} is out of the building's range ({IBuildingApi.MaxRange} cells).");
    }

    /// <summary> A port over one <see cref="CellObserver"/>. </summary>
    private sealed class BuildingPort(BuildingApi owner, CellObserver observer) : IBuildingPort, IDisposable
    {
        /// <summary> Whether a machine currently occupies the observed cell. </summary>
        public bool HasMachine => !observer.IsEmpty;

        /// <summary> Read-only view of the machine on the observed cell, or <c>null</c>. </summary>
        public IMachine? Machine => observer.Machine;

        /// <inheritdoc/>
        public int Pull(int count, string? itemKind = null) =>
            observer.Machine is { } neighbor
                ? ItemFlow.Move(neighbor.Inventory, owner._machine.Inventory, count, LiteOf(itemKind))
                : 0;

        /// <inheritdoc/>
        public int Push(int count, string? itemKind = null) =>
            observer.Machine is { } neighbor
                ? ItemFlow.Move(owner._machine.Inventory, neighbor.Inventory, count, LiteOf(itemKind))
                : 0;

        public void Dispose() => observer.Dispose();

        private ItemLite? LiteOf(string? itemKind) =>
            itemKind == null ? null : owner._registry.GetItemLite(itemKind);
    }
}
