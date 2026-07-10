using System;
using System.Collections.Generic;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.Machines;
using FlowControlModel.World;

namespace FlowControlModel.Entities;

/// <summary>
/// The capability surface an entity acts through: movement requests, its own inventory,
/// ground items and machines around itself. The entity counterpart of
/// <see cref="IBuildingApi"/> — future scripted entities (drones) resolve the same object.
/// </summary>
/// <remarks>
/// The same two rules as for buildings are enforced inside:
/// <list type="bullet">
/// <item><b>No conjuring:</b> every item transfer passes through the entity's own
/// inventory; items are never created or destroyed by the API.</item>
/// <item><b>Bounded reach:</b> world queries and item interactions are limited to
/// <see cref="MaxRange"/> cells around the entity and throw beyond it.</item>
/// </list>
/// Movement is a request: the simulation applies it through the regular
/// collision-aware path (blocked moves slide along free axes).
/// </remarks>
public interface IEntityApi
{
    /// <summary> How far (in cells) the entity can query and reach items and machines. </summary>
    public const float MaxRange = 3f;

    /// <summary> Read-only view of the entity itself. </summary>
    IEntity Entity { get; }

    /// <summary>
    /// Requests a movement by <paramref name="delta"/> this tick. The simulation applies
    /// it with collision checks after the logic's tick; the last request wins.
    /// </summary>
    void Move(Vec2 delta);

    /// <summary>
    /// Counts items in the entity's own inventory: of one kind,
    /// or of any kind when <paramref name="itemKind"/> is <c>null</c>.
    /// </summary>
    int CountItems(string? itemKind = null);

    /// <summary>
    /// Picks up ground items lying within <paramref name="radius"/> cells of the entity
    /// (clamped to <see cref="MaxRange"/>) into its own inventory: of one kind, or any.
    /// Returns the number of items collected; whatever does not fit stays on the ground.
    /// </summary>
    int PickUpItems(float radius, string? itemKind = null);

    /// <summary>
    /// Drops up to <paramref name="count"/> items of a kind from the entity's own
    /// inventory onto the ground at an offset from the entity
    /// (bounded by <see cref="MaxRange"/>). Returns the number actually dropped.
    /// </summary>
    int DropItems(string itemKind, int count, Vec2 offset);

    /// <summary>
    /// Ground items lying within <paramref name="radius"/> cells of the entity
    /// (clamped to <see cref="MaxRange"/>), for inspection.
    /// </summary>
    IReadOnlyList<IGroundItem> GroundItemsNear(float radius);

    /// <summary>
    /// Machines whose footprint lies within <paramref name="radius"/> cells of the entity
    /// (clamped to <see cref="MaxRange"/>), for inspection.
    /// </summary>
    IReadOnlyList<IMachine> MachinesNear(float radius);

    /// <summary>
    /// Moves up to <paramref name="count"/> items from the machine's inventory into the
    /// entity's own: of one kind, or of any kind. The machine must be within
    /// <see cref="MaxRange"/>. Returns the number actually moved.
    /// </summary>
    int PullFrom(uint machineId, int count, string? itemKind = null);

    /// <summary>
    /// Moves up to <paramref name="count"/> items from the entity's own inventory into
    /// the machine's: of one kind, or of any kind. The machine must be within
    /// <see cref="MaxRange"/>. Returns the number actually moved.
    /// </summary>
    int PushTo(uint machineId, int count, string? itemKind = null);

    /// <summary>
    /// The ground kind at <paramref name="offset"/> cells from the entity's cell.
    /// The cell must lie within <see cref="MaxRange"/>.
    /// </summary>
    string GroundKindAt(Vec2I offset);
}

/// <summary> The model's implementation of <see cref="IEntityApi"/>. </summary>
internal sealed class EntityApi(Registry registry, ChunkManager chunkManager, GameObjectFactory factory)
    : IEntityApi
{
    private Entity _entity = null!;
    private Vec2 _requestedMove = Vec2.Zero;

    /// <summary> Read-only view of the entity itself. </summary>
    public IEntity Entity => _entity;

    /// <summary> Attaches the owning entity (called once by the factory). </summary>
    public void Bind(Entity entity) => _entity = entity;

    /// <inheritdoc/>
    public void Move(Vec2 delta) => _requestedMove = delta;

    /// <summary> Returns and clears the movement requested during the last logic tick. </summary>
    public Vec2 ConsumeRequestedMove()
    {
        var move = _requestedMove;
        _requestedMove = Vec2.Zero;
        return move;
    }

    /// <inheritdoc/>
    public int CountItems(string? itemKind = null) => _entity.Inventory.CountItems(itemKind);

    /// <inheritdoc/>
    public int PickUpItems(float radius, string? itemKind = null)
    {
        radius = Math.Clamp(radius, 0, IEntityApi.MaxRange);
        return ItemFlow.PickUp(
            chunkManager, factory, _entity.Inventory, _entity.Coord, radius, itemKind);
    }

    /// <inheritdoc/>
    public int DropItems(string itemKind, int count, Vec2 offset)
    {
        RequireInRange(offset.DistanceTo(Vec2.Zero), "drop point");
        return ItemFlow.Drop(
            chunkManager, factory, _entity.Inventory,
            registry.GetItemLite(itemKind), count, _entity.Coord + offset);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IGroundItem> GroundItemsNear(float radius)
    {
        radius = Math.Clamp(radius, 0, IEntityApi.MaxRange);
        return chunkManager.FindGroundItemsNear(_entity.Coord, radius);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IMachine> MachinesNear(float radius)
    {
        radius = Math.Clamp(radius, 0, IEntityApi.MaxRange);
        return chunkManager.FindMachinesNear(_entity.Coord, radius);
    }

    /// <inheritdoc/>
    public int PullFrom(uint machineId, int count, string? itemKind = null)
    {
        var machine = ReachableMachine(machineId);
        return machine == null ? 0 : ItemFlow.Move(
            machine.Inventory, _entity.Inventory, count, LiteOf(itemKind));
    }

    /// <inheritdoc/>
    public int PushTo(uint machineId, int count, string? itemKind = null)
    {
        var machine = ReachableMachine(machineId);
        return machine == null ? 0 : ItemFlow.Move(
            _entity.Inventory, machine.Inventory, count, LiteOf(itemKind));
    }

    /// <inheritdoc/>
    public string GroundKindAt(Vec2I offset)
    {
        var cell = _entity.Coord.FloorToI() + offset;
        RequireInRange(_entity.Coord.DistanceTo(cell + new Vec2(0.5f, 0.5f)), "cell");
        return chunkManager.GetGroundLiteAt(cell).Kind;
    }

    /// <summary> The machine, if it exists and is within range of the entity. </summary>
    private Machine? ReachableMachine(uint machineId)
    {
        var machine = chunkManager.FindMachineInstById(machineId);
        if (machine == null)
            return null;

        var closest = new Vec2(
            Math.Clamp(_entity.Coord.X, machine.Rect.Position.X, machine.Rect.End.X),
            Math.Clamp(_entity.Coord.Y, machine.Rect.Position.Y, machine.Rect.End.Y));
        return closest.DistanceTo(_entity.Coord) <= IEntityApi.MaxRange ? machine : null;
    }

    private ItemLite? LiteOf(string? itemKind) =>
        itemKind == null ? null : registry.GetItemLite(itemKind);

    private static void RequireInRange(float distance, string what)
    {
        if (distance > IEntityApi.MaxRange + 1f)
            throw new InvalidOperationException(
                $"The {what} is out of the entity's range ({IEntityApi.MaxRange} cells).");
    }
}
