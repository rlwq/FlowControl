using System.Collections.Generic;
using FlowControlModel.Entities;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel;

/// <summary>
/// The core engine of the simulation, responsible for handling high-level business logic.
/// The only entry point for modifying the world: all changes arrive as <see cref="WorldSimCommand"/>s.
/// </summary>
public class WorldSim
{
    /// <summary> How far (in cells) a player can build, remove machines and handle items. </summary>
    public const float PlayerReach = 8f;

    /// <summary> How far (in cells) from the pick-up point ground items are collected. </summary>
    public const float PickUpRadius = 1.5f;

    private readonly Registry _registry;
    private readonly ChunkManager _chunkManager;
    private readonly GameObjectFactory _gameObjectFactory;

    private readonly Queue<WorldSimCommand> _commandQueue = [];

    private ulong _tickCount;

    /// <summary> The logical state of the world. </summary>
    public IChunkManager ChunkManager => _chunkManager;

    /// <summary> Creates a simulation over a fresh world produced by the specified generator. </summary>
    /// <param name="registry"> The catalog of all registered kinds. </param>
    /// <param name="grid"> Geometry of the world's chunk grid. </param>
    /// <param name="generator"> The ground generator used when chunks materialize. </param>
    public WorldSim(Registry registry, WorldGrid grid, IWorldGenerator generator)
    {
        _registry = registry;
        _chunkManager = new ChunkManager(grid, generator);
        _gameObjectFactory = new GameObjectFactory(registry, _chunkManager);
    }

    /// <summary> Adds a new <see cref="WorldSimCommand"/> to the queue. </summary>
    public void ReceiveCommand(WorldSimCommand command) => _commandQueue.Enqueue(command);

    /// <summary> Executes one tick of the simulation. </summary>
    public void Tick()
    {
        // Applies all business logic transactions
        while (_commandQueue.Count > 0)
            _commandQueue.Dequeue().Execute(this);

        _chunkManager.Tick();
        TickEntities();
        TickGroundSpawners();

        _tickCount++;
    }

    /// <summary> Ticks the autonomous logic of every entity that has one. </summary>
    private void TickEntities()
    {
        foreach (var entity in _chunkManager.GetEntitiesSnapshot())
        {
            if (entity.Logic == null)
                continue;
            var delta = entity.Logic.Tick(entity);
            if (delta != Vec2.Zero)
                MoveEntityBy(entity.Id, delta);
        }
    }

    /// <summary>
    /// Spawns items on spawner ground tiles (e.g. ore deposits). A tile spawns at most one
    /// item stack per period, holds at most one stack at a time, and never spawns under a machine.
    /// Spawn moments are phase-shifted per cell so deposits do not fire all at once.
    /// </summary>
    private void TickGroundSpawners()
    {
        foreach (var (cell, lite) in _chunkManager.EnumerateSpawnerCells())
        {
            var spawner = lite.Spawner!;
            if ((_tickCount + SpawnPhase(cell)) % (ulong)spawner.PeriodTicks != 0)
                continue;
            if (!_chunkManager.IsCellEmpty(cell))
                continue;
            if (_chunkManager.HasGroundItemOnCell(cell))
                continue;

            DropStack(new ItemStack(1, spawner.Item), cell + new Vec2(0.5f, 0.5f));
        }
    }

    /// <summary> A deterministic per-cell phase shift for ground spawners. </summary>
    private static ulong SpawnPhase(Vec2I cell) =>
        (ulong)(uint)(cell.X * 73856093 ^ cell.Y * 19349663);

    /// <summary>
    /// Creates and places a new machine, if the target region is free of machines and entities.
    /// If <paramref name="actorId"/> is provided, the target must be within the actor's reach,
    /// and the actor's inventory must contain (and loses) one item of the machine's kind.
    /// </summary>
    internal void PlaceMachine(string kind, Vec2I coord, Rotation rotation = Rotation.North, uint? actorId = null)
    {
        var lite = _registry.GetMachineLite(kind);
        var rect = new RectI(coord, RotationM.RotateDims(lite.Dimensions, rotation));

        if (!ActorCanReach(actorId, rect))
            return;
        if (!_chunkManager.IsBoxFree(rect))
            return;
        if (actorId != null && !TryConsumeItem(actorId.Value, kind))
            return;

        var machine = _gameObjectFactory.CreateMachine(kind, coord, rotation);
        _chunkManager.PlaceMachine(machine);
    }

    /// <summary>
    /// Removes and disposes the machine at the specified position, if any.
    /// The machine's inventory contents spill onto the ground.
    /// If <paramref name="actorId"/> is provided, the machine must be within the actor's reach,
    /// and the actor receives one item of the machine's kind back.
    /// </summary>
    internal void RemoveMachine(Vec2I coord, uint? actorId = null)
    {
        var machine = _chunkManager.GetMachineInstAt(coord);
        if (machine == null)
            return;
        if (!ActorCanReach(actorId, machine.Rect))
            return;

        var rect = machine.Rect;
        _chunkManager.RemoveMachine(machine);

        // The machine itself goes back to the remover (or on the ground if their bag is full)
        if (actorId != null)
        {
            var machineItem = new ItemStack(1, _registry.GetItemLite(machine.Lite.Kind));
            var actor = _chunkManager.FindEntityInstById(actorId.Value);
            var leftover = actor?.Inventory.InsertItem(machineItem) ?? machineItem;
            if (!leftover.IsEmpty)
                DropStack(leftover, rect.GetCenter());
        }

        // The contents spill over the machine's former cells
        var cellIndex = 0;
        foreach (var stack in machine.Inventory.EnumerateStacks())
        {
            var cell = rect.Position + new Vec2I(
                cellIndex % rect.Size.X,
                cellIndex / rect.Size.X % rect.Size.Y);
            DropStack(stack, cell + new Vec2(0.5f, 0.5f));
            cellIndex++;
        }

        machine.Dispose();
    }

    /// <summary>
    /// Moves the entity with the specified id by a relative offset, respecting collisions.
    /// If the full step is blocked, tries to slide along each axis separately.
    /// </summary>
    internal void MoveEntityBy(uint entityId, Vec2 delta)
    {
        var entity = _chunkManager.FindEntityInstById(entityId);
        if (entity == null)
            return;

        var target = entity.Coord + delta;
        if (!CanStandAt(entity, target))
        {
            var xOnly = entity.Coord + new Vec2(delta.X, 0);
            var yOnly = entity.Coord + new Vec2(0, delta.Y);

            if (delta.X != 0 && CanStandAt(entity, xOnly)) target = xOnly;
            else if (delta.Y != 0 && CanStandAt(entity, yOnly)) target = yOnly;
            else return;
        }
        _chunkManager.MoveEntity(entity, target);
    }

    /// <summary>
    /// Creates and places a new entity of the specified kind, if the spot is free.
    /// Returns the new entity's id, or <c>null</c> if the placement was rejected.
    /// </summary>
    internal uint? PlaceEntity(string kind, Vec2 coord)
    {
        var lite = _registry.GetEntityLite(kind);
        if (!_chunkManager.IsBoxFree(new Rect(coord - lite.BoxSize / 2, lite.BoxSize)))
            return null;

        var entity = _gameObjectFactory.CreateEntity(kind, coord);
        _chunkManager.PlaceEntity(entity);
        return entity.Id;
    }

    /// <summary> Removes and disposes the entity with the specified id, if it exists. </summary>
    internal void RemoveEntity(uint entityId)
    {
        var entity = _chunkManager.FindEntityInstById(entityId);
        if (entity == null)
            return;

        _chunkManager.RemoveEntity(entity);
        entity.Dispose();
    }

    /// <summary> Replaces the ground tile at the specified position. </summary>
    internal void SetGround(string kind, Vec2I coord) =>
        _chunkManager.SetGround(coord, _registry.GetGroundLite(kind));

    /// <summary>
    /// Inserts items into the inventory of the machine at the specified position, if any.
    /// Items that do not fit are discarded.
    /// </summary>
    internal void InsertItem(string itemKind, int count, Vec2I coord)
    {
        var machine = _chunkManager.GetMachineInstAt(coord);
        machine?.Inventory.InsertItem(new ItemStack(count, _registry.GetItemLite(itemKind)));
    }

    /// <summary> Inserts items into the inventory of the entity with the specified id, if it exists. </summary>
    internal void GiveItems(uint entityId, string itemKind, int count)
    {
        var entity = _chunkManager.FindEntityInstById(entityId);
        entity?.Inventory.InsertItem(new ItemStack(count, _registry.GetItemLite(itemKind)));
    }

    /// <summary>
    /// Drops items on the ground at the specified position.
    /// If <paramref name="actorId"/> is provided, the spot must be within the actor's reach
    /// and the items are taken from the actor's inventory (as many as it actually holds);
    /// otherwise the items are created out of thin air (system drop).
    /// </summary>
    internal void DropItem(string itemKind, int count, Vec2 coord, uint? actorId = null)
    {
        if (count <= 0)
            return;
        if (!ActorCanReach(actorId, new Rect(coord, Vec2.Zero)))
            return;

        var stack = new ItemStack(count, _registry.GetItemLite(itemKind));
        if (actorId != null)
        {
            var actor = _chunkManager.FindEntityInstById(actorId.Value);
            if (actor == null)
                return;
            stack = actor.Inventory.ExtractItem(stack);
            if (stack.IsEmpty)
                return;
        }

        DropStack(stack, coord);
    }

    /// <summary>
    /// Picks up all ground items within <see cref="PickUpRadius"/> of the specified position
    /// into the actor's inventory. Whatever does not fit stays on the ground.
    /// The pick-up point must be within the actor's reach.
    /// </summary>
    internal void PickUpItems(Vec2 coord, uint actorId)
    {
        var actor = _chunkManager.FindEntityInstById(actorId);
        if (actor == null || !ActorCanReach(actorId, new Rect(coord, Vec2.Zero)))
            return;

        foreach (var item in _chunkManager.FindGroundItemsNear(coord, PickUpRadius))
        {
            var leftover = actor.Inventory.InsertItem(item.Stack);
            _chunkManager.RemoveGroundItem(item);
            if (!leftover.IsEmpty)
                DropStack(leftover, item.Coord);
        }
    }

    /// <summary> Places an item stack on the ground. </summary>
    private void DropStack(ItemStack stack, Vec2 coord)
    {
        var item = _gameObjectFactory.CreateGroundItem(stack, coord);
        _chunkManager.PlaceGroundItem(item);
    }

    /// <summary>
    /// Removes one item of the specified kind from the actor's inventory.
    /// Returns whether the actor actually had one.
    /// </summary>
    private bool TryConsumeItem(uint actorId, string itemKind)
    {
        var actor = _chunkManager.FindEntityInstById(actorId);
        if (actor == null)
            return false;

        var extracted = actor.Inventory.ExtractItem(new ItemStack(1, _registry.GetItemLite(itemKind)));
        return !extracted.IsEmpty;
    }

    /// <summary>
    /// Whether the actor may affect the specified region: <c>true</c> for system commands
    /// (no actor), or when the region's center is within <see cref="PlayerReach"/> of the actor.
    /// </summary>
    private bool ActorCanReach(uint? actorId, Rect rect)
    {
        if (actorId == null)
            return true;

        var actor = _chunkManager.FindEntityInstById(actorId.Value);
        return actor != null && rect.GetCenter().DistanceTo(actor.Coord) <= PlayerReach;
    }

    /// <summary> Whether the entity's collision box fits at the specified position. </summary>
    private bool CanStandAt(Entity entity, Vec2 coord) =>
        _chunkManager.IsBoxFree(new Rect(coord - entity.Lite.BoxSize / 2, entity.Lite.BoxSize), entity.Id);
}

/// <summary>
/// A transactional command which encapsulates business logic.
/// </summary>
public abstract class WorldSimCommand
{
    /// <summary> Executes the command on the specified simulation. </summary>
    public abstract void Execute(WorldSim sim);
}

/// <summary>
/// A transactional command which places a new Machine in the world.
/// Pass <paramref name="actorId"/> to place on behalf of a player (reach is checked and
/// one item of the machine's kind is consumed); omit it for system placements.
/// </summary>
public class PlaceMachineAt(string kind, Vec2I coord, Rotation rotation = Rotation.North, uint? actorId = null)
    : WorldSimCommand
{
    /// <summary> Places a new Machine in the world, if the region is free and reachable. </summary>
    public override void Execute(WorldSim sim) => sim.PlaceMachine(kind, coord, rotation, actorId);
}

/// <summary>
/// A transactional command which removes a Machine from the world. Its inventory spills
/// onto the ground. Pass <paramref name="actorId"/> to remove on behalf of a player
/// (reach is checked and the machine item is returned); omit it for system removals.
/// </summary>
public class RemoveMachineAt(Vec2I coord, uint? actorId = null) : WorldSimCommand
{
    /// <summary> Removes a Machine from the world, if it exists and is reachable. </summary>
    public override void Execute(WorldSim sim) => sim.RemoveMachine(coord, actorId);
}

/// <summary> A transactional command which replaces a ground tile. </summary>
public class SetGroundAt(string kind, Vec2I coord) : WorldSimCommand
{
    /// <summary> Replaces the ground tile at the specified position. </summary>
    public override void Execute(WorldSim sim) => sim.SetGround(kind, coord);
}

/// <summary>
/// A transactional command which inserts items into the inventory of the machine
/// at the specified position.
/// </summary>
public class InsertItemAt(string itemKind, int count, Vec2I coord) : WorldSimCommand
{
    /// <summary> Inserts the items, discarding whatever does not fit. </summary>
    public override void Execute(WorldSim sim) => sim.InsertItem(itemKind, count, coord);
}

/// <summary>
/// A transactional command which inserts items into the inventory of the entity
/// with the specified id (e.g. starting equipment for a player).
/// </summary>
public class GiveItemsTo(uint entityId, string itemKind, int count) : WorldSimCommand
{
    /// <summary> Inserts the items, discarding whatever does not fit. </summary>
    public override void Execute(WorldSim sim) => sim.GiveItems(entityId, itemKind, count);
}

/// <summary>
/// A transactional command which places a new entity in the world.
/// After execution, <see cref="EntityId"/> holds the new entity's id
/// (or <c>null</c> if the placement was rejected).
/// </summary>
public class PlaceEntityAt(string kind, Vec2 coord) : WorldSimCommand
{
    /// <summary> The id of the placed entity. <c>null</c> before execution or when rejected. </summary>
    public uint? EntityId { get; private set; }

    /// <summary> Places a new entity in the world. </summary>
    public override void Execute(WorldSim sim) => EntityId = sim.PlaceEntity(kind, coord);
}

/// <summary> A transactional command which removes an entity from the world. </summary>
public class RemoveEntityById(uint entityId) : WorldSimCommand
{
    /// <summary> Removes the entity, if it exists. </summary>
    public override void Execute(WorldSim sim) => sim.RemoveEntity(entityId);
}

/// <summary>
/// A transactional command which moves an entity by a relative offset, respecting collisions.
/// </summary>
public class EntityStepById(uint entityId, Vec2 delta) : WorldSimCommand
{
    /// <summary> Moves the entity by the offset (or slides along a free axis). </summary>
    public override void Execute(WorldSim sim) => sim.MoveEntityBy(entityId, delta);
}

/// <summary>
/// A transactional command which drops items on the ground. Pass <paramref name="actorId"/>
/// to drop from a player's inventory (reach is checked); omit it for system drops.
/// </summary>
public class DropItemAt(string itemKind, int count, Vec2 coord, uint? actorId = null) : WorldSimCommand
{
    /// <summary> Drops the items on the ground. </summary>
    public override void Execute(WorldSim sim) => sim.DropItem(itemKind, count, coord, actorId);
}

/// <summary>
/// A transactional command which picks up ground items around a point
/// into the actor's inventory.
/// </summary>
public class PickUpItemAt(Vec2 coord, uint actorId) : WorldSimCommand
{
    /// <summary> Picks up nearby ground items; whatever does not fit stays. </summary>
    public override void Execute(WorldSim sim) => sim.PickUpItems(coord, actorId);
}
