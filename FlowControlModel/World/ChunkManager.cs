using System;
using System.Collections.Generic;
using FlowControlModel.Factories;
using FlowControlModel.Entities;
using FlowControlModel.Machines;
using System.Diagnostics;

namespace FlowControlModel.World;

/// <summary>
/// A read-only view of the game world: queries and change notifications.
/// All modifications go through <see cref="WorldSim"/> commands.
/// </summary>
public interface IChunkManager
{
    /// <summary> Geometry of the world's chunk grid. </summary>
    WorldGrid Grid { get; }

    /// <summary> Emitted when a machine is placed in the world. </summary>
    event Action<IMachine>? MachinePlaced;

    /// <summary> Emitted when a machine is removed from the world. </summary>
    event Action<IMachine>? MachineRemoved;

    /// <summary> Emitted when an entity is added to the world. </summary>
    event Action<IEntity>? EntityPlaced;

    /// <summary> Emitted when an entity is removed from the world. </summary>
    event Action<IEntity>? EntityRemoved;

    /// <summary> Emitted when an entity is moved in the world. Contains entities old position the entity itself with already updated position. </summary>
    event Action<IEntity, Vec2>? EntityMoved;

    /// <summary> Emitted when a ground tile is replaced. Contains the global position and the new ground kind. </summary>
    event Action<Vec2I, GroundLite>? GroundTileSet;

    /// <summary> Emitted when an item stack appears on the ground. </summary>
    event Action<IGroundItem>? GroundItemPlaced;

    /// <summary> Emitted when an item stack disappears from the ground. </summary>
    event Action<IGroundItem>? GroundItemRemoved;

    /// <summary> Returns the kind of the ground tile at the specified global position. </summary>
    GroundLite GetGroundLiteAt(Vec2I coord);

    /// <summary> Returns whether the tile at the specified global position has no Machine on it. </summary>
    bool IsCellEmpty(Vec2I coord);

    /// <summary> Returns whether the box overlaps no machines and no entities. </summary>
    bool IsBoxFree(Rect box);

    /// <summary> Returns the entity with the specified ID. </summary>
    IEntity GetEntityById(uint id);

    /// <summary> Returns the Machine occupying the specified global position, or <c>null</c> if it is empty. </summary>
    IMachine? GetMachineAt(Vec2I coord);

    /// <summary> Returns all entities obtained by the specified chunk. </summary>
    IEntity[] GetEntitiesInChunk(Vec2I chunkCoord);

    /// <summary> Returns all machines overlapping the specified chunk. </summary>
    IMachine[] GetMachinesInChunk(Vec2I chunkCoord);

    /// <summary> Returns all item stacks lying on the ground within the specified chunk. </summary>
    IGroundItem[] GetGroundItemsInChunk(Vec2I chunkCoord);
}

/// <summary>
/// A specialized data structure responsible for maintaining the integrity of the game world.
/// Centralizes all world modifications to ensure that spatial relationships remain consistent.
/// </summary>
public class ChunkManager(WorldGrid grid, IWorldGenerator generator) : IChunkManager
{
    private readonly Dictionary<Vec2I, Chunk> _chunks = [];

    private readonly Dictionary<uint, Machine> _machinesById = [];
    private readonly Dictionary<uint, Entity> _entitiesById = [];
    private readonly Dictionary<uint, GroundItem> _groundItemsById = [];

    /// <summary> Geometry of the world's chunk grid. </summary>
    public WorldGrid Grid { get; } = grid;

    /// <summary> Emitted when a machine is placed in the world. </summary>
    public event Action<IMachine>? MachinePlaced;

    /// <summary> Emitted when a machine is removed from the world. </summary>
    public event Action<IMachine>? MachineRemoved;

    /// <summary> Emitted when an entity is added to the world. </summary>
    public event Action<IEntity>? EntityPlaced;

    /// <summary> Emitted when an entity is removed from the world. </summary>
    public event Action<IEntity>? EntityRemoved;

    /// <summary> Emitted when an entity is moved in the world. Contains entities old position the entity itself with already updated position. </summary>
    public event Action<IEntity, Vec2>? EntityMoved;

    /// <summary> Emitted when a ground tile is replaced. Contains the global position and the new ground kind. </summary>
    public event Action<Vec2I, GroundLite>? GroundTileSet;

    /// <summary> Emitted when an item stack appears on the ground. </summary>
    public event Action<IGroundItem>? GroundItemPlaced;

    /// <summary> Emitted when an item stack disappears from the ground. </summary>
    public event Action<IGroundItem>? GroundItemRemoved;

    /// <summary> Returns whether the tile at the specified global position has no Machine on it. </summary>
    public bool IsCellEmpty(Vec2I coord)
    {
        var localCoord = Grid.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).IsAirAt(localCoord.Cell);
    }

    /// <summary> Returns whether the box overlaps no machines and no entities. </summary>
    public bool IsBoxFree(Rect box) => IsBoxFree(box, null);

    /// <summary>
    /// Same as <see cref="IsBoxFree(Rect)"/>, but the entity with the
    /// <paramref name="ignoreEntityId"/> is excluded from the check (used when moving it).
    /// </summary>
    internal bool IsBoxFree(Rect box, uint? ignoreEntityId)
    {
        if (!AreCellsFree(box))
            return false;

        // Entities: check the chunks overlapped by the box, grown by one,
        // as entity boxes may cross chunk borders
        var beginChunk = Grid.ToLocal(box.Position).Chunk - Vec2I.One;
        var endChunk = Grid.ToLocal(box.End).Chunk + Vec2I.One;
        for (var i = beginChunk.Y; i <= endChunk.Y; i++)
        for (var j = beginChunk.X; j <= endChunk.X; j++)
        {
            if (!_chunks.TryGetValue(new Vec2I(j, i), out var chunk))
                continue;
            foreach (var entity in chunk.GetEntities())
                if (entity.Id != ignoreEntityId && entity.Box.Intersects(box))
                    return false;
        }
        return true;
    }

    /// <summary> Whether every cell overlapped by the box is free of machines. </summary>
    private bool AreCellsFree(Rect box)
    {
        for (var i = MathM.FloorToInt(box.Position.Y); i < box.End.Y; i++)
        for (var j = MathM.FloorToInt(box.Position.X); j < box.End.X; j++)
            if (!IsCellEmpty(new Vec2I(j, i)))
                return false;
        return true;
    }

    /// <summary>
    /// Places the specified Machine in the game world.
    /// Registers it in every chunk it overlaps and links all occupied tiles.
    /// The region must have been verified to be empty beforehand.
    /// </summary>
    internal void PlaceMachine(Machine machine)
    {
        Debug.Assert(AreCellsFree(machine.Rect), $"Region is already occupied at {machine.Coord}");

        _machinesById.Add(machine.Id, machine);

        foreach (var chunkCoord in ChunksOverlapping(machine.Rect))
        {
            var chunk = LoadChunk(chunkCoord);
            chunk.RegisterMachine(machine);
            chunk.InvokeMachinePlacedInChunk(machine);
        }

        for (int i = machine.Coord.Y; i < machine.End.Y; i++)
        for (int j = machine.Coord.X; j < machine.End.X; j++)
        {
            var tileCoord = Grid.ToLocalI(new Vec2I(j, i));
            LoadChunk(tileCoord.Chunk).LinkTile(tileCoord.Cell, machine);
        }

        MachinePlaced?.Invoke(machine);
    }

    /// <summary>
    /// Removes the Machine from the world entirely and frees all occupied tiles.
    /// </summary>
    internal void RemoveMachine(Machine machine)
    {
        Debug.Assert(_machinesById.ContainsKey(machine.Id), $"Machine #{machine.Id} is not placed");

        machine.Remove();

        foreach (var chunkCoord in ChunksOverlapping(machine.Rect))
        {
            var chunk = LoadChunk(chunkCoord);
            chunk.InvokeMachineRemovedFromChunk(machine);
            chunk.UnregisterMachine(machine);
        }

        for (var i = machine.Coord.Y; i < machine.End.Y; i++)
        for (var j = machine.Coord.X; j < machine.End.X; j++)
        {
            var tileCoord = Grid.ToLocalI(new Vec2I(j, i));
            LoadChunk(tileCoord.Chunk).FreeTile(tileCoord.Cell);
        }

        _machinesById.Remove(machine.Id);

        MachineRemoved?.Invoke(machine);
    }

    /// <summary> Returns the Machine occupying the specified global position, or <c>null</c> if it is empty. </summary>
    public IMachine? GetMachineAt(Vec2I coord) => GetMachineInstAt(coord);

    /// <summary> Returns the concrete machine with the specified ID, or <c>null</c> if it does not exist. </summary>
    internal Machine? FindMachineInstById(uint id) => _machinesById.GetValueOrDefault(id);

    /// <summary> Same as <see cref="GetMachineAt"/>, but returns the concrete <see cref="Machine"/>. </summary>
    internal Machine? GetMachineInstAt(Vec2I coord)
    {
        var localCoord = Grid.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).GetMachineAt(localCoord.Cell);
    }

    /// <summary> Returns all Machine instances overlapping the specified chunk. </summary>
    public IMachine[] GetMachinesInChunk(Vec2I chunkCoord)
    {
        return LoadChunk(chunkCoord).GetMachines();
    }

    /// <summary> Returns the kind of the ground tile at the specified global position. </summary>
    public GroundLite GetGroundLiteAt(Vec2I coord)
    {
        var localCoord = Grid.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).GetGroundLite(localCoord.Cell);
    }

    /// <summary> Replaces the ground tile at the specified global position. </summary>
    internal void SetGround(Vec2I coord, GroundLite lite)
    {
        var localCoord = Grid.ToLocalI(coord);
        LoadChunk(localCoord.Chunk).SetGroundLite(localCoord.Cell, lite);
        GroundTileSet?.Invoke(coord, lite);
    }

    /// <summary> Returns all entities located within the specified chunk. </summary>
    public IEntity[] GetEntitiesInChunk(Vec2I chunkCoord)
    {
        return LoadChunk(chunkCoord).GetEntities();
    }

    /// <summary> Places an entity to the world. </summary>
    internal void PlaceEntity(Entity entity)
    {
        var localCoord = Grid.ToLocal(entity.Coord);
        LoadChunk(localCoord.Chunk).AddEntity(entity);

        _entitiesById[entity.Id] = entity;
        EntityPlaced?.Invoke(entity);
    }

    /// <summary> Removes an entity from the world. </summary>
    internal void RemoveEntity(Entity entity)
    {
        var localCoord = Grid.ToLocal(entity.Coord);
        LoadChunk(localCoord.Chunk).RemoveEntity(entity);

        _entitiesById.Remove(entity.Id);
        EntityRemoved?.Invoke(entity);
    }

    /// <summary> Moves an entity to a new location. Handles transition between chunks, if necessary. </summary>
    internal void MoveEntity(Entity entity, Vec2 toCoord)
    {
        var from = entity.Coord;

        var fromLocal = Grid.ToLocal(from).Chunk;
        var toLocal = Grid.ToLocal(toCoord).Chunk;

        entity.Coord = toCoord;

        if (fromLocal != toLocal)
        {
            LoadChunk(fromLocal).RemoveEntity(entity);
            LoadChunk(toLocal).AddEntity(entity);
        }

        EntityMoved?.Invoke(entity, from);
    }

    /// <summary> Returns the entity with the specified ID. </summary>
    public IEntity GetEntityById(uint id) => _entitiesById[id];

    /// <summary> Returns the concrete entity with the specified ID, or <c>null</c> if it does not exist. </summary>
    internal Entity? FindEntityInstById(uint id) => _entitiesById.GetValueOrDefault(id);

    /// <summary> A snapshot of all placed entities, safe to iterate while the world changes. </summary>
    internal Entity[] GetEntitiesSnapshot() => [.. _entitiesById.Values];

    /// <summary> Returns all item stacks lying on the ground within the specified chunk. </summary>
    public IGroundItem[] GetGroundItemsInChunk(Vec2I chunkCoord)
    {
        return LoadChunk(chunkCoord).GetGroundItems();
    }

    /// <summary> Places an item stack on the ground. </summary>
    internal void PlaceGroundItem(GroundItem item)
    {
        var localCoord = Grid.ToLocal(item.Coord);
        LoadChunk(localCoord.Chunk).AddGroundItem(item);

        _groundItemsById[item.Id] = item;
        GroundItemPlaced?.Invoke(item);
    }

    /// <summary> Removes an item stack from the ground. </summary>
    internal void RemoveGroundItem(GroundItem item)
    {
        var localCoord = Grid.ToLocal(item.Coord);
        LoadChunk(localCoord.Chunk).RemoveGroundItem(item);

        _groundItemsById.Remove(item.Id);
        GroundItemRemoved?.Invoke(item);
    }

    /// <summary> Returns all ground items within the specified radius of a point. </summary>
    internal List<GroundItem> FindGroundItemsNear(Vec2 coord, float radius)
    {
        var result = new List<GroundItem>();
        var beginChunk = Grid.ToLocal(coord - new Vec2(radius, radius)).Chunk;
        var endChunk = Grid.ToLocal(coord + new Vec2(radius, radius)).Chunk;
        for (var i = beginChunk.Y; i <= endChunk.Y; i++)
        for (var j = beginChunk.X; j <= endChunk.X; j++)
        {
            if (!_chunks.TryGetValue(new Vec2I(j, i), out var chunk))
                continue;
            foreach (var item in chunk.GroundItems)
                if (item.Coord.DistanceTo(coord) <= radius)
                    result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Returns all machines whose footprint lies within the specified radius of a point
    /// (distance to the closest footprint cell), without duplicates.
    /// </summary>
    internal List<Machine> FindMachinesNear(Vec2 coord, float radius)
    {
        var result = new List<Machine>();
        var seen = new HashSet<uint>();
        var beginChunk = Grid.ToLocal(coord - new Vec2(radius, radius)).Chunk;
        var endChunk = Grid.ToLocal(coord + new Vec2(radius, radius)).Chunk;
        for (var i = beginChunk.Y; i <= endChunk.Y; i++)
        for (var j = beginChunk.X; j <= endChunk.X; j++)
        {
            if (!_chunks.TryGetValue(new Vec2I(j, i), out var chunk))
                continue;
            foreach (var machine in chunk.GetMachines())
                if (seen.Add(machine.Id) && DistanceToRect(machine.Rect, coord) <= radius)
                    result.Add(machine);
        }
        return result;
    }

    /// <summary> Distance from a point to the closest point of a rectangle (0 inside). </summary>
    private static float DistanceToRect(RectI rect, Vec2 point)
    {
        var closest = new Vec2(
            System.Math.Clamp(point.X, rect.Position.X, rect.End.X),
            System.Math.Clamp(point.Y, rect.Position.Y, rect.End.Y));
        return closest.DistanceTo(point);
    }

    /// <summary> Whether any ground item lies within the specified cell. </summary>
    internal bool HasGroundItemOnCell(Vec2I cell)
    {
        if (!_chunks.TryGetValue(Grid.ToLocalI(cell).Chunk, out var chunk))
            return false;
        foreach (var item in chunk.GroundItems)
            if (item.Coord.FloorToI() == cell)
                return true;
        return false;
    }

    /// <summary>
    /// Enumerates all cells of existing chunks whose ground has an <see cref="ItemSpawner"/>.
    /// Yields global cell coordinates. Spawning an item into an enumerated cell is safe:
    /// the cell's chunk already exists, so the chunk map is not modified.
    /// </summary>
    internal IEnumerable<(Vec2I Cell, GroundLite Lite)> EnumerateSpawnerCells()
    {
        foreach (var (chunkCoord, chunk) in _chunks)
        {
            var origin = chunkCoord * Grid.ChunkSize;
            foreach (var (localCell, lite) in chunk.SpawnerCells)
                yield return (origin + localCell, lite);
        }
    }

    /// <summary> Executes one quant of every placed machine's logic. </summary>
    internal void Tick()
    {
        foreach (var machine in _machinesById.Values)
            machine.Tick();
    }

    /// <summary> Enumerates the coordinates of all chunks overlapped by the region. </summary>
    private IEnumerable<Vec2I> ChunksOverlapping(RectI rect)
    {
        var begin = Grid.ToLocalI(rect.Position).Chunk;
        var end = Grid.ToLocalI(rect.End - Vec2I.One).Chunk;
        for (var i = begin.Y; i <= end.Y; i++)
        for (var j = begin.X; j <= end.X; j++)
            yield return new Vec2I(j, i);
    }

    /// <summary>
    /// Returns the Chunk object at the specified position.
    /// Creates and initializes a new one if it doesn't exist.
    /// </summary>
    private Chunk LoadChunk(Vec2I chunkCoord)
    {
        if (_chunks.TryGetValue(chunkCoord, out var existingChunk))
            return existingChunk;

        var chunk = new Chunk(Grid, generator, chunkCoord);
        _chunks.Add(chunkCoord, chunk);
        return chunk;
    }

    /// <summary> Builds a <see cref="CellObserver"/> watching the specified global position. </summary>
    internal CellObserver CreateCellObserver(Vec2I coord)
    {
        var localCoord = Grid.ToLocalI(coord);
        return new CellObserver(LoadChunk(localCoord.Chunk), localCoord.Cell, coord);
    }
}
