using System;
using System.Collections.Generic;
using System.Diagnostics;
using FlowControlModel.Factories;
using FlowControlModel.Entities;
using FlowControlModel.Machines;

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
internal class Chunk
{
    public event Action<Machine>? MachinePlacedInChunk;
    public event Action<Machine>? MachineRemovedFromChunk;

    private readonly WorldGrid _grid;

    private Vec2I _chunkCoord;
    private readonly HashSet<Entity> _entities = [];
    private readonly HashSet<Machine> _machines = [];
    private readonly HashSet<GroundItem> _groundItems = [];
    private readonly Machine?[,] _machineTiles;
    private readonly GroundLite[,] _groundTiles;
    private readonly Dictionary<Vec2I, GroundLite> _spawnerCells = [];

    /// <summary> Amount of tiles along one side of the chunk. </summary>
    public int ChunkSize => _grid.ChunkSize;

    /// <summary> Gets the world-space boundaries of this chunk, in integer coordinates. </summary>
    public RectI ChunkRectI => new(_chunkCoord * ChunkSize, new Vec2I(ChunkSize, ChunkSize));

    /// <summary> Gets the world-space boundaries of this chunk, in floating-point coordinates. </summary>
    public Rect ChunkRect => new(_chunkCoord * ChunkSize, new Vec2I(ChunkSize, ChunkSize));

    /// <summary> Local coordinates of tiles whose ground periodically spawns items. </summary>
    public IReadOnlyDictionary<Vec2I, GroundLite> SpawnerCells => _spawnerCells;

    /// <summary> Returns the ground type at the specified local coordinates. </summary>
    public GroundLite GetGroundLite(Vec2I localPos) => _groundTiles[localPos.X, localPos.Y];

    /// <summary> Replaces the ground type at the specified local coordinates. </summary>
    public void SetGroundLite(Vec2I localCoord, GroundLite lite)
    {
        Debug.Assert(_grid.LocalRectI.HasPoint(localCoord));
        _groundTiles[localCoord.X, localCoord.Y] = lite;

        if (lite.Spawner != null) _spawnerCells[localCoord] = lite;
        else _spawnerCells.Remove(localCoord);
    }

    /// <summary> Initializes a new chunk, generating its ground layer. </summary>
    public Chunk(WorldGrid grid, IWorldGenerator generator, Vec2I chunkCoord)
    {
        _grid = grid;
        _chunkCoord = chunkCoord;

        _machineTiles = new Machine[ChunkSize, ChunkSize];
        _groundTiles = new GroundLite[ChunkSize, ChunkSize];

        var origin = _chunkCoord * ChunkSize;
        for (int i = 0; i < ChunkSize; i++)
        for (int j = 0; j < ChunkSize; j++)
        {
            var lite = generator.GetGroundAt(origin + new Vec2I(j, i));
            _groundTiles[j, i] = lite;
            if (lite.Spawner != null)
                _spawnerCells[new Vec2I(j, i)] = lite;
        }
    }

    /// <summary> Returns an array of all unique machines overlapping this chunk. </summary>
    public Machine[] GetMachines() => [.. _machines];

    /// <summary> Adds a machine to the chunk's internal registry. </summary>
    public void RegisterMachine(Machine machine) => _machines.Add(machine);

    /// <summary> Removes a machine from the chunk's internal registry. </summary>
    public void UnregisterMachine(Machine machine)
    {
        Debug.Assert(_machines.Contains(machine));
        _machines.Remove(machine);
    }

    /// <summary> Associates a specific local tile with a machine instance. </summary>
    /// <param name="localCoord">Coordinates relative to the chunk (0 to ChunkSize-1).</param>
    /// <param name="machine">The machine to link to the tile.</param>
    public void LinkTile(Vec2I localCoord, Machine machine)
    {
        Debug.Assert(_grid.LocalRectI.HasPoint(localCoord));
        Debug.Assert(_machineTiles[localCoord.X, localCoord.Y] == null);
        _machineTiles[localCoord.X, localCoord.Y] = machine;
    }

    /// <summary> Clears the machine reference from a specific local tile. </summary>
    public void FreeTile(Vec2I localCoord)
    {
        Debug.Assert(_grid.LocalRectI.HasPoint(localCoord));
        Debug.Assert(_machineTiles[localCoord.X, localCoord.Y] != null);
        _machineTiles[localCoord.X, localCoord.Y] = null;
    }

    /// <summary> Returns the machine occupying the local tile, or <c>null</c> if it is empty. </summary>
    public Machine? GetMachineAt(Vec2I localCoord)
    {
        Debug.Assert(_grid.LocalRectI.HasPoint(localCoord));
        return _machineTiles[localCoord.X, localCoord.Y];
    }

    /// <summary> Checks if there is no machine at the specified local coordinates. </summary>
    public bool IsAirAt(Vec2I localCoord)
    {
        Debug.Assert(_grid.LocalRectI.HasPoint(localCoord));
        return _machineTiles[localCoord.X, localCoord.Y] == null;
    }

    /// <summary> Returns an array of all entities registered in this chunk. </summary>
    public Entity[] GetEntities() => [.. _entities];

    /// <summary> Adds an entity to the chunk. </summary>
    public void AddEntity(Entity entity)
    {
        Debug.Assert(ChunkRect.HasPoint(entity.Coord));
        _entities.Add(entity);
    }

    /// <summary> Removes an entity from the chunk. </summary>
    public void RemoveEntity(Entity entity)
    {
        Debug.Assert(_entities.Contains(entity));
        _entities.Remove(entity);
    }

    /// <summary> Returns an array of all ground items lying in this chunk. </summary>
    public GroundItem[] GetGroundItems() => [.. _groundItems];

    /// <summary> All ground items lying in this chunk, without copying. </summary>
    public IReadOnlyCollection<GroundItem> GroundItems => _groundItems;

    /// <summary> Adds a ground item to the chunk. </summary>
    public void AddGroundItem(GroundItem item)
    {
        Debug.Assert(ChunkRect.HasPoint(item.Coord));
        _groundItems.Add(item);
    }

    /// <summary> Removes a ground item from the chunk. </summary>
    public void RemoveGroundItem(GroundItem item)
    {
        Debug.Assert(_groundItems.Contains(item));
        _groundItems.Remove(item);
    }

    public void InvokeMachinePlacedInChunk(Machine machine) =>
        MachinePlacedInChunk?.Invoke(machine);

    public void InvokeMachineRemovedFromChunk(Machine machine) =>
        MachineRemovedFromChunk?.Invoke(machine);
}
