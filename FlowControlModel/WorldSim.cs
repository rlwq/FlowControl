using System.Collections.Generic;
using FlowControlModel.Factories;
using FlowControlModel.Entities;
using FlowControlModel.World;
using Godot;

namespace FlowControlModel;

/// <summary>
/// The core engine of the simulation, responsible for handling high-level business logic.
/// </summary>
public class WorldSim
{
    private readonly Registry _registry;
    internal readonly ChunkManager _chunkManager;
    internal readonly GameObjectFactory _gameObjectFactory;

    private readonly Dictionary<uint, IEntity> _entitiesById = [];
    private readonly Queue<WorldSimCommand> _commandQueue = [];

    internal IEntity GetEntityById(uint id) => _entitiesById[id];

    /// <summary> The logical state of the world. </summary>
    public IChunkManager ChunkManager => _chunkManager;

    public WorldSim(Registry registry)
    {
        _registry = registry;
        _chunkManager = new ChunkManager(_registry);
        _gameObjectFactory = new GameObjectFactory(_chunkManager);
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
    }

    public uint AddPlayer()
    {
        var player = _gameObjectFactory.CreateEntity("player", new Vector2(0, 0));
        _chunkManager.PlaceEntity(player);
        return player.Id;
    }
}

/// <summary>
/// A transactional command which encapsulates business logic.
/// </summary>
public abstract class WorldSimCommand
{
    /// <summary> Executes the command on the specified simulation. </summary>
    public abstract void Execute(WorldSim sim);
}

/// <summary> A transactional command which places a new Machine in the world. </summary>
public class PlaceMachineAt(StringName kind, Vector2I coord) : WorldSimCommand
{
    private readonly StringName Kind = kind;
    private readonly Vector2I Coord = coord;

    /// <summary> Places a new Machine in the world, if the region is empty. </summary>
    public override void Execute(WorldSim sim)
    {
        var machine = sim._gameObjectFactory.CreateMachine(Kind, Coord);
        if (!sim._chunkManager.IsRegionEmpty(machine.Rect))
            return;
        sim._chunkManager.PlaceMachine(machine);
    }
}

/// <summary> A transactional command which removes a Machine from the world. </summary>
public class RemoveMachineAt(Vector2I coord) : WorldSimCommand
{
    private readonly Vector2I Coord = coord;

    /// <summary> Removes a Machine from the world, if it exists. </summary>
    public override void Execute(WorldSim sim)
    {
        if (sim._chunkManager.IsCellEmpty(Coord))
            return;
        var machine = sim._chunkManager.GetMachineInstAt(Coord);
        sim._chunkManager.RemoveMachine(machine);
    }
}

/// <summary> A transactional command which moves an entity. </summary>
public class EntityStepById(uint entityId, float amount, int dir) : WorldSimCommand
{
    /// <summary> Moves an entity in the specified direction. </summary>
    public override void Execute(WorldSim sim)
    {
        var entity = sim._chunkManager.GetEntityById(entityId);
        var newPos = entity.Coord;
        switch (dir) {
            case 0: newPos.Y -= amount; break;
            case 1: newPos.Y += amount; break;
            case 2: newPos.X -= amount; break;
            case 3: newPos.X += amount; break;
        }
        sim._chunkManager.MoveEntity(entity, newPos);
    }
}
