using System.Collections.Generic;
using System.Linq;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.World;
using Godot;

namespace FlowControlModel.Factories;

internal class GameObjectFactory(ChunkManager chunkManager)
{
    private uint _availableMachineId = 0;
    private uint _availableEntityId = 0;

    public Machine CreateMachine(StringName kind, Vector2I globalCoord)
    {
        var lite = chunkManager.Registry.GetMachineLite(kind);
        var logic = chunkManager.Registry.GetMachineLogic(kind).Copy();

        if (chunkManager.Registry.IsMachineInteractive(kind))
        {
            List<CellObserver> observers = [];
            observers.AddRange(chunkManager
                .Registry
                .GetMachineObserverOffsets(kind)
                .Select(chunkManager.CreateCellObserver));
            
            ((MachineInteractiveLogic) logic).LinkObservers(observers);
        }
        
        var machine = new Machine(_availableMachineId++, globalCoord, lite, logic);
        
        return machine;
    }

    /// <summary>
    /// Instantiates a new <see cref="Entity"/> of the specified <paramref name="kind"/> at a world position.
    /// </summary>
    public Entity CreateEntity(StringName kind, Vector2 globalCoord)
    {
        var lite = chunkManager.Registry.GetEntityLite(kind);
        var entity = new Entity(_availableEntityId++, lite, globalCoord);
        return entity;
    }
}
