using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;
using FlowControlBusiness.Entities;
using FlowControlBusiness.Machines;
using FlowControlBusiness.WorldGeneration;

namespace FlowControlModel.Tests;

/// <summary> Builds a small ready-to-use world shared by the test suites. </summary>
public static class TestWorld
{
    public const int ChunkSize = 20;

    /// <summary> How often deposit tiles spawn ore in the test registry, in ticks. </summary>
    public const int OreSpawnPeriod = 50;

    /// <summary>
    /// A builder pre-loaded with the standard kinds (everything the generator and the
    /// demo flows need). Test suites extend it with their probe kinds before building.
    /// </summary>
    public static Registry.RegistryBuilder StandardBuilder() =>
        new Registry.RegistryBuilder()
            .RegisterEntity("player", new Vec2(0.75f, 0.75f), new InventoryDimensions(0, 16, 0))
            .RegisterItem("iron_ingot", 16)
            .RegisterItem("iron_ore", 32)
            .RegisterItem("copper_ore", 32)
            .RegisterItem("coal", 32)
            .RegisterItem("chest", 8)
            .RegisterItem("manipulator", 8)
            .RegisterGround("stone", speedModifier: 1.25f)
            .RegisterGround("grass")
            .RegisterGround("water", passable: false)
            .RegisterGround("iron_deposit", spawnsItemKind: "iron_ore", spawnPeriodTicks: OreSpawnPeriod)
            .RegisterGround("copper_deposit", spawnsItemKind: "copper_ore", spawnPeriodTicks: OreSpawnPeriod)
            .RegisterGround("coal_deposit", spawnsItemKind: "coal", spawnPeriodTicks: OreSpawnPeriod)
            .RegisterMachine("chest", new Vec2I(2, 1), new InventoryDimensions(0, 8, 0))
            .RegisterMachine("manipulator", new Vec2I(1, 1), new InventoryDimensions(0, 1, 0))
            .RegisterMachineLogic("chest", new Dumb())
            .RegisterMachineLogic("manipulator", new Manipulator(), [new Vec2I(-1, 0), new Vec2I(0, 1)])
            .RegisterEntity("cow", new Vec2(1, 1))
            .RegisterEntityLogic("cow", new Wanderer());

    /// <summary> A registry with the standard demo kinds. </summary>
    public static Registry BuildRegistry() => StandardBuilder().Build();

    /// <summary> A simulation over a noise-generated world with a fixed seed. </summary>
    public static WorldSim BuildSim(Registry? registry = null)
    {
        registry ??= BuildRegistry();
        return new WorldSim(
            registry, new WorldGrid(ChunkSize), new NoiseWorldGenerator(registry, seed: 42), seed: 42);
    }

    /// <summary> Spawns a player through the command queue (runs one tick) and returns its id. </summary>
    public static uint SpawnPlayer(WorldSim sim, Vec2 coord)
    {
        var command = new CmdPlaceEntityAt("player", coord);
        sim.ReceiveCommand(command);
        sim.Tick();
        return command.EntityId!.Value;
    }
}
