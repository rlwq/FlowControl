using System;
using System.Collections.Generic;
using System.Diagnostics;
using FlowControlModel.Factories;
using FlowControlModel.Entities;
using FlowControlModel.Machines;
using Godot;

namespace FlowControlModel.World;

/// <summary>
/// Represents a square section of the game (logic) world.
/// </summary>
/// <remarks>
/// <para>Each <see cref="Chunk"/> contains a map linking each tile to the corresponding <see cref="Machine"/>.</para>
/// <para>This class maintains only worlds' logical structure. For visual representation, see <c>ChunkView</c>.</para>
/// <para>IMPORTANT: Public methods of this class must be used exclusively by <c>ChunkManager</c> as it can't
/// guarantee data integrity by itself.</para>
/// </remarks>
public class Chunk
{
    public event Action<Machine>? MachinePlacedInChunk;
    public event Action<Machine>? MachineRemovedFromChunk;

    private readonly Registry _registry;

    private Vector2I _chunkCoord;
    private readonly HashSet<IEntity> _entities = [];
    private readonly HashSet<Machines.Machine> _machines = [];
    private readonly Machines.Machine?[,] _machineTiles;
    private readonly GroundLite[,] _groundTiles;

    /// <summary> Amount of tiles along one side of the chunk. </summary>
    public int ChunkSize => _registry.ChunkSize;

    /// <summary> Gets the world-space boundaries of this chunk, in integer coordinates. </summary>
    public Rect2I ChunkRectI => new(_chunkCoord * ChunkSize, new Vector2I(ChunkSize, ChunkSize));

    /// <summary> Gets the world-space boundaries of this chunk, in floating-point coordinates. </summary>
    public Rect2 ChunkRect => new(_chunkCoord * ChunkSize, new Vector2I(ChunkSize, ChunkSize));

    /// <summary> Returns the ground type at the specified local coordinates. </summary>
    public GroundLite GetGroundLite(Vector2I localPos) => _groundTiles[localPos.X, localPos.Y];

    /// <summary> Initializes a new chunk. </summary>
    public Chunk(Registry registry, Vector2I chunkCoord)
    {
        _registry = registry;
        _chunkCoord = chunkCoord;

        _machineTiles = new Machines.Machine[ChunkSize, ChunkSize];
        _groundTiles = new GroundLite[ChunkSize, ChunkSize];

        // Default world generation: checkerboard pattern
        for (int i = 0; i < ChunkSize; i++)
        for (int j = 0; j < ChunkSize; j++)
        {
            _groundTiles[j, i] =
                (i + j) % 2 == 0
                    ? _registry.GetGroundLite("stone")
                    : _registry.GetGroundLite("grass");
        }
    }

    /// <summary> Returns an array of all unique machines registered in this chunk. </summary>
    public Machines.Machine[] GetMachines() => [.. _machines];

    /// <summary> Adds a machine to the chunk's internal registry. </summary>
    public void RegisterMachine(Machines.Machine machine) => _machines.Add(machine);

    /// <summary> Removes a machine from the chunk's internal registry. </summary>
    public void UnregisterMachine(Machines.Machine machine)
    {
        Debug.Assert(ChunkRectI.HasPoint(machine.Coord));
        Debug.Assert(_machines.Contains(machine));
        machine.Remove();
        _machines.Remove(machine);
    }

    /// <summary> Associates a specific local tile with a machine instance. </summary>
    /// <param name="localCoord">Coordinates relative to the chunk (0 to ChunkSize-1).</param>
    /// <param name="machine">The machine to link to the tile.</param>
    public void LinkTile(Vector2I localCoord, Machine machine)
    {
        Debug.Assert(_registry.ChunkRectI.HasPoint(localCoord));
        Debug.Assert(_machineTiles[localCoord.X, localCoord.Y] == null);
        _machineTiles[localCoord.X, localCoord.Y] = machine;
    }

    /// <summary> Clears the machine reference from a specific local tile. </summary>
    public void FreeTile(Vector2I localCoord)
    {
        Debug.Assert(_registry.ChunkRectI.HasPoint(localCoord));
        Debug.Assert(_machineTiles[localCoord.X, localCoord.Y] != null);
        _machineTiles[localCoord.X, localCoord.Y] = null;
    }

    /// <summary> Returns the machine occupying the local tile. </summary>
    public Machine GetMachineAt(Vector2I localCoord)
    {
        Debug.Assert(_registry.ChunkRectI.HasPoint(localCoord));
        return _machineTiles[localCoord.X, localCoord.Y]!;
    }

    /// <summary> Checks if there is no machine at the specified local coordinates. </summary>
    public bool IsAirAt(Vector2I localCoord)
    {
        Debug.Assert(_registry.ChunkRectI.HasPoint(localCoord));
        return _machineTiles[localCoord.X, localCoord.Y] == null;
    }

    /// <summary> Returns an array of all entities registered in this chunk. </summary>
    public IEntity[] GetEntities() => [.. _entities];

    /// <summary> Adds an entity to the chunk. </summary>
    public void AddEntity(IEntity entity)
    {
        Debug.Assert(ChunkRect.HasPoint(entity.Coord));
        _entities.Add(entity);
    }

    /// <summary> Removes an entity from the chunk. </summary>
    public void RemoveEntity(IEntity entity)
    {
        Debug.Assert(_entities.Contains(entity));
        _entities.Remove(entity);
    }

    public void InvokeMachinePlacedInChunk(Machine machine) =>
        MachinePlacedInChunk?.Invoke(machine);

    public void InvokeMachineRemovedFromChunk(Machine machine) =>
        MachineRemovedFromChunk?.Invoke(machine);
}
