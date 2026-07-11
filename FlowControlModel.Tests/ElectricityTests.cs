using System.Linq;
using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.Machines;
using FlowControlBusiness.Machines;
using Xunit;

namespace FlowControlModel.Tests;

public class ElectricityTests
{
    /// <summary>
    /// The standard kinds plus electricity: a pole, a coal generator and a powered
    /// manipulator variant (the standard test manipulator stays unpowered on purpose).
    /// </summary>
    private static Registry PowerRegistry() =>
        TestWorld.StandardBuilder()
            .RegisterMachine("power_pole", new Vec2I(1, 1),
                pole: new PoleSpec(WireReach: 6f, SupplyRadius: 3.5f))
            .RegisterMachineLogic("power_pole", new Dumb())
            .RegisterMachine("generator", new Vec2I(2, 2), new InventoryDimensions(1, 1, 0))
            .RegisterMachineLogic("generator", new BurnerGenerator(power: 10f, burnTicksPerFuel: 100))
            .RegisterMachine("p_manipulator", new Vec2I(1, 1), new InventoryDimensions(0, 1, 0),
                powerDemand: 10f)
            .RegisterMachineLogic("p_manipulator", new Manipulator(), [new Vec2I(-1, 0), new Vec2I(0, 1)])
            .Build();

    /// <summary> Chests at (2,3) and (4,4) with a powered manipulator at (4,3) between them. </summary>
    private static WorldSim BuildLine()
    {
        var sim = TestWorld.BuildSim(PowerRegistry());
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("p_manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(2, 3)));
        sim.Tick();
        return sim;
    }

    private static int Delivered(WorldSim sim) =>
        sim.ChunkManager.GetMachineAt(new Vec2I(4, 4))!.Inventory.CountItems("iron_ingot");

    [Fact]
    public void UnpoweredManipulator_DoesNotWork()
    {
        var sim = BuildLine();
        for (var i = 0; i < 100; i++) sim.Tick();

        Assert.Equal(0, Delivered(sim));
        Assert.Equal(0f, sim.ChunkManager.GetMachineAt(new Vec2I(4, 3))!.PowerSatisfaction);
    }

    [Fact]
    public void PoweredManipulator_TransfersItems()
    {
        var sim = BuildLine();
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(5, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(6, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 5, new Vec2I(6, 1)));

        for (var i = 0; i < 200; i++) sim.Tick();

        Assert.Equal(8, Delivered(sim));
        Assert.Equal(1f, sim.ChunkManager.GetMachineAt(new Vec2I(4, 3))!.PowerSatisfaction);
    }

    [Fact]
    public void GeneratorWithoutFuel_PowersNothing()
    {
        var sim = BuildLine();
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(5, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(6, 1)));
        // no coal

        for (var i = 0; i < 100; i++) sim.Tick();

        Assert.Equal(0, Delivered(sim));
    }

    [Fact]
    public void ConsumerOutsideSupplyRadius_StaysDark()
    {
        var sim = BuildLine();
        // The pole is far from the manipulator but close to the generator
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(12, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(13, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 5, new Vec2I(13, 1)));

        for (var i = 0; i < 100; i++) sim.Tick();

        Assert.Equal(0, Delivered(sim));
    }

    [Fact]
    public void ConnectingPole_MergesNetworksAndPowersTheLine()
    {
        var sim = BuildLine();
        // Generator's pole is out of the manipulator's reach…
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(10, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(11, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 5, new Vec2I(11, 1)));
        for (var i = 0; i < 50; i++) sim.Tick();
        Assert.Equal(0, Delivered(sim));

        // …until a second pole bridges the gap
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(5, 2)));
        for (var i = 0; i < 200; i++) sim.Tick();
        Assert.True(Delivered(sim) > 0, "the bridging pole must power the manipulator");
    }

    [Fact]
    public void RemovingThePole_CutsThePower()
    {
        var sim = BuildLine();
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(5, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(6, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 5, new Vec2I(6, 1)));
        for (var i = 0; i < 50; i++) sim.Tick();
        var before = Delivered(sim);
        Assert.True(before > 0);

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(5, 2)));
        for (var i = 0; i < 100; i++) sim.Tick();

        Assert.Equal(before, Delivered(sim)); // nothing moved since the pole fell
    }

    [Fact]
    public void Undersupply_SlowsConsumersDown()
    {
        // Two manipulators demand 20 in total; one generator offers 10 → 50% speed
        var sim = TestWorld.BuildSim(PowerRegistry());
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("p_manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("p_manipulator", new Vec2I(6, 3))); // second consumer, idle
        sim.ReceiveCommand(new CmdPlaceMachineAt("power_pole", new Vec2I(5, 2)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(6, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 5, new Vec2I(6, 1)));
        sim.Tick();

        for (var i = 0; i < 50; i++) sim.Tick();

        var manipulator = sim.ChunkManager.GetMachineAt(new Vec2I(4, 3))!;
        Assert.Equal(0.5f, manipulator.PowerSatisfaction, 3);
        // Half speed: a full swing takes ~40 ticks instead of ~20 → at most 2 deliveries in 51
        Assert.InRange(Delivered(sim), 1, 2);
    }

    [Fact]
    public void ConsumeItems_ReachesTheInputSection()
    {
        // The generator's inventory is (1,1,0): coal lands in the input section
        // and the burner must still be able to eat it
        var sim = TestWorld.BuildSim(PowerRegistry());
        sim.ReceiveCommand(new CmdPlaceMachineAt("generator", new Vec2I(6, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("coal", 1, new Vec2I(6, 1)));
        sim.Tick();
        sim.Tick();

        var generator = sim.ChunkManager.GetMachineAt(new Vec2I(6, 1))!;
        Assert.Equal(0, generator.Inventory.CountItems("coal"));
        Assert.Contains("Burning", generator.LogicState);
    }
}
