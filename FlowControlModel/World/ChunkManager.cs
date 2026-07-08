using System;
using System.Collections.Generic;
using FlowControlModel.Factories;
using FlowControlModel.Entities;
using FlowControlModel.Machines;
using System.Diagnostics;
using Godot;

namespace FlowControlModel.World;

/// <summary>
/// An interface providing access to events emitted by the <see cref="ChunkManager"/>.
/// </summary>
public interface IChunkManager
{
    /// <summary> A reference to the <see cref="Registry"/>. </summary>
    Registry Registry { get; }

    /// <summary> Emitted when a machine is placed in the world. </summary>
    event Action<IMachine>? MachinePlaced;

    /// <summary> Emitted when a machine is removed from the world. </summary>
    event Action<IMachine>? MachineRemoved;

    /// <summary> Emitted when an entity is added to the world. </summary>
    event Action<IEntity>? EntityPlaced;

    /// <summary> Emitted when an entity is removed from the world. </summary>
    event Action<IEntity>? EntityRemoved;

    /// <summary> Emitted when an entity is moved in the world. Contains entities old position the entity itself with already updated position. </summary>
    event Action<IEntity, Vector2>? EntityMoved;

    /// <summary> Returns the kind of the ground tile at the specified global position. </summary>
    GroundLite GetGroundLiteAt(Vector2I coord);

    /// <summary> Returns the Machine object linked to the specified global position. Returns "Air" if empty. </summary>
    IMachine GetMachineAt(Vector2I coord);

    /// <summary> Returns all entities obtained by the specified chunk. </summary>
    IEntity[] GetEntitiesInChunk(Vector2I chunkCoord);

    /// <summary> Returns all entities obtained by the specified chunk. </summary>
    IMachine[] GetMachinesInChunk(Vector2I chunkCoord);
}

/// <summary>
/// A specialized data structure responsible for maintaining the integrity of the game world.
/// Centralizes all world modifications to ensure that spatial relationships remain consistent.
/// </summary>
public class ChunkManager(Registry registry) : IChunkManager
{
    private readonly Dictionary<Vector2I, Chunk> _chunks = [];

    private readonly Dictionary<uint, Entity> _entitiesById = [];

    // private readonly Dictionary<uint, Machine> _machineById = [];

    /// <summary> A reference to the <see cref="Registry"/>. </summary>
    public Registry Registry { get; } = registry;

    /// <summary> Emitted when a machine is placed in the world. </summary>
    public event Action<IMachine>? MachinePlaced;

    /// <summary> Emitted when a machine is removed from the world. </summary>
    public event Action<IMachine>? MachineRemoved;

    /// <summary> Emitted when an entity is added to the world. </summary>
    public event Action<IEntity>? EntityPlaced;

    /// <summary> Emitted when an entity is removed from the world. </summary>
    public event Action<IEntity>? EntityRemoved;

    /// <summary> Emitted when an entity is moved in the world. Contains entities old position the entity itself with already updated position. </summary>
    public event Action<IEntity, Vector2>? EntityMoved;

    /// <summary> Returns whether the tile at the specified global position has no Machine on it. </summary>
    public bool IsCellEmpty(Vector2I coord)
    {
        var localCoord = Registry.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).IsAirAt(localCoord.Cell);
    }

    /// <summary> Returns whether the specified rectangular region is completely empty. </summary>
    public bool IsRegionEmpty(Rect2I rect)
    {
        for (var j = rect.Position.X; j < rect.End.X; j++)
        for (var i = rect.Position.Y; i < rect.End.Y; i++)
            if (!IsCellEmpty(new Vector2I(j, i)))
                return false;
        return true;
    }

    /// <summary>
    /// Places the specified Machine in the game world.
    /// Links all occupied tiles in the corresponding Chunks. Throws if the region is not empty.
    /// </summary>
    internal void PlaceMachine(Machine machine)
    {
        var localCoord = Registry.ToLocalI(machine.Coord);

        // Safety check as per GDScript logic
        Debug.Assert(IsRegionEmpty(machine.Rect), $"Region is already occupied at {machine.Coord}");

        LoadChunk(localCoord.Chunk).RegisterMachine(machine);

        Vector2I beginChunk = localCoord.Chunk;
        Vector2I endChunk = Registry.ToLocalI(machine.End).Chunk;

        for (int i = beginChunk.Y; i <= endChunk.Y; i++)
        for (int j = beginChunk.X; j <= endChunk.X; j++)
            LoadChunk(new Vector2I(j, i)).InvokeMachinePlacedInChunk(machine);

        for (int i = machine.Coord.Y; i < machine.End.Y; i++)
        for (int j = machine.Coord.X; j < machine.End.X; j++)
        {
            var tileCoord = Registry.ToLocalI(new Vector2I(j, i));
            LoadChunk(tileCoord.Chunk).LinkTile(tileCoord.Cell, machine);
        }

        MachinePlaced?.Invoke(machine);
    }

    /// <summary>
    /// Removes the Machine from the world entirely and frees all occupied tiles.
    /// Throws if the starting tile is not occupied by a Machine.
    /// </summary>
    internal void RemoveMachine(Machine machine)
    {
        Debug.Assert(!IsCellEmpty(machine.Coord), $"No machine found to remove at {machine.Coord}");

        var localCoord = Registry.ToLocalI(machine.Coord);

        var beginChunk = localCoord.Chunk;
        var endChunk = Registry.ToLocalI(machine.End).Chunk;

        for (var i = beginChunk.Y; i <= endChunk.Y; i++)
        for (var j = beginChunk.X; j <= endChunk.X; j++)
            LoadChunk(new Vector2I(j, i)).InvokeMachineRemovedFromChunk(machine);

        for (var i = machine.Coord.Y; i < machine.End.Y; i++)
        for (var j = machine.Coord.X; j < machine.End.X; j++)
        {
            var tileCoord = Registry.ToLocalI(new Vector2I(j, i));
            LoadChunk(tileCoord.Chunk).FreeTile(tileCoord.Cell);
        }

        LoadChunk(Registry.ToLocalI(machine.Coord).Chunk).UnregisterMachine(machine);

        MachineRemoved?.Invoke(machine);
    }

    /// <summary> Returns the Machine object linked to the specified global position. Returns "Air" if empty. </summary>
    public IMachine GetMachineAt(Vector2I coord) => GetMachineInstAt(coord);

    /// <summary> Same as <see cref="GetMachineAt"/>, but returns the concrete <see cref="Machine"/>. </summary>
    internal Machine GetMachineInstAt(Vector2I coord)
    {
        var localCoord = Registry.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).GetMachineAt(localCoord.Cell);
    }

    /// <summary> Returns all Machine instances located within the specified chunk. </summary>
    public IMachine[] GetMachinesInChunk(Vector2I chunkCoord)
    {
        return LoadChunk(chunkCoord).GetMachines();
    }

    /// <summary> Returns the kind of the ground tile at the specified global position. </summary>
    public GroundLite GetGroundLiteAt(Vector2I coord)
    {
        var localCoord = Registry.ToLocalI(coord);
        return LoadChunk(localCoord.Chunk).GetGroundLite(localCoord.Cell);
    }

    /// <summary> Returns all entities located within the specified chunk. </summary>
    public IEntity[] GetEntitiesInChunk(Vector2I chunkCoord)
    {
        return LoadChunk(chunkCoord).GetEntities();
    }

    /// <summary> Places an entity to the world. </summary>
    public void PlaceEntity(Entity entity)
    {
        var localCoord = Registry.ToLocal(entity.Coord);
        LoadChunk(localCoord.Chunk).AddEntity(entity);

        _entitiesById[entity.Id] = entity;
        EntityPlaced?.Invoke(entity);
    }

    /// <summary> Removes an entity from the world. </summary>
    public void RemoveEntity(Entity entity)
    {
        var localCoord = Registry.ToLocal(entity.Coord);
        LoadChunk(localCoord.Chunk).RemoveEntity(entity);

        _entitiesById.Remove(entity.Id);
        EntityRemoved?.Invoke(entity);
    }

    /// <summary> Moves an entity to a new location. Handles transition between chunks, if necessary. </summary>
    public void MoveEntity(IEntity entity, Vector2 toCoord)
    {
        var from = entity.Coord;

        var fromLocal = Registry.ToLocal(from).Chunk;
        var toLocal = Registry.ToLocal(toCoord).Chunk;

        ((Entity)entity).Coord = toCoord;

        if (fromLocal != toLocal)
        {
            LoadChunk(fromLocal).RemoveEntity(entity);
            LoadChunk(toLocal).AddEntity(entity);
        }

        EntityMoved?.Invoke(entity, from);
    }

    /// <summary> Returns the entity with the specified ID. </summary>
    public IEntity GetEntityById(uint id) => _entitiesById[id];

    public void Tick()
    {
        foreach (var chunk in _chunks.Values)
        foreach (var machine in chunk.GetMachines())
            machine.Tick();
    }

    /// <summary>
    /// Returns the Chunk object at the specified position.
    /// Creates and initializes a new one if it doesn't exist.
    /// </summary>
    private Chunk LoadChunk(Vector2I chunkCoord)
    {
        if (_chunks.TryGetValue(chunkCoord, out var existingChunk))
            return existingChunk;

        GD.Print($"Created new chunk at {chunkCoord}");

        var chunk = new Chunk(Registry, chunkCoord);
        _chunks.Add(chunkCoord, chunk);
        return chunk;
    }

    internal CellObserver CreateCellObserver(Vector2I coord)
    {
        var localCoord = Registry.ToLocalI(coord);
        return new CellObserver(LoadChunk(localCoord.Chunk), localCoord.Cell, coord);
    }
}
