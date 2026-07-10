using System.Linq;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Factories;

/// <summary>
/// Instantiates game objects from their registered types,
/// assigning unique identifiers and wiring up machine logic.
/// </summary>
internal class GameObjectFactory(Registry registry, ChunkManager chunkManager)
{
    private uint _availableMachineId = 0;
    private uint _availableEntityId = 0;
    private uint _availableGroundItemId = 0;

    /// <summary>
    /// Instantiates a new <see cref="Machine"/> of the specified <paramref name="kind"/> at a world position,
    /// wiring up its <see cref="BuildingApi"/> with ports over the registered observer offsets
    /// (rotated along with the machine).
    /// </summary>
    public Machine CreateMachine(string kind, Vec2I globalCoord, Rotation rotation)
    {
        var lite = registry.GetMachineLite(kind);
        var logic = registry.GetMachineLogic(kind).Copy();

        var observers = registry.IsMachineInteractive(kind)
            ? registry
                .GetMachineObserverOffsets(kind)
                .Select(offset => chunkManager.CreateCellObserver(
                    globalCoord + RotationM.RotateOffset(offset, lite.Dimensions, rotation)))
                .ToArray()
            : [];

        var api = new BuildingApi(registry, chunkManager, this, observers);
        var machine = new Machine(_availableMachineId++, globalCoord, rotation, lite, logic, api);
        api.Bind(machine);
        return machine;
    }

    /// <summary>
    /// Instantiates a new <see cref="Entity"/> of the specified <paramref name="kind"/> at a world position,
    /// wiring up a copy of its logic prototype, if the kind has one.
    /// </summary>
    public Entity CreateEntity(string kind, Vec2 globalCoord)
    {
        var lite = registry.GetEntityLite(kind);
        var logic = registry.FindEntityLogic(kind)?.Copy();
        return new Entity(_availableEntityId++, lite, globalCoord, logic);
    }

    /// <summary> Instantiates a new <see cref="GroundItem"/> holding the specified stack. </summary>
    public GroundItem CreateGroundItem(ItemStack stack, Vec2 globalCoord)
    {
        return new GroundItem(_availableGroundItemId++, globalCoord, stack);
    }
}
