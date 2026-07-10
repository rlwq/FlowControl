using System;
using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.Machines;
using Xunit;

namespace FlowControlModel.Tests;

public class BuildingApiTests
{
    /// <summary> The api captured by the latest <see cref="ApiProbe"/> tick. </summary>
    private static IBuildingApi? _lastApi;

    /// <summary> A logic that captures its <see cref="IBuildingApi"/> so tests can drive it directly. </summary>
    private sealed class ApiProbe : MachineLogic
    {
        public override void Tick(IBuildingApi building) => _lastApi = building;
        public override MachineLogic Copy() => new ApiProbe();
    }

    /// <summary> The standard test registry plus a 1x1 probe machine with two ports. </summary>
    private static Registry ProbeRegistry() =>
        new Registry.RegistryBuilder()
            .RegisterGround("stone")
            .RegisterGround("grass")
            .RegisterGround("iron_deposit", spawnsItemKind: "iron_ore", spawnPeriodTicks: 50)
            .RegisterItem("iron_bar", 16)
            .RegisterItem("iron_ore", 32)
            .RegisterMachine("chest", new Vec2I(2, 1), new InventoryDimensions(0, 8, 0))
            .RegisterMachine("probe", new Vec2I(1, 1), new InventoryDimensions(0, 2, 0))
            .RegisterMachineLogic("chest", new FlowControlBusiness.Machines.Dumb())
            .RegisterMachineLogic("probe", new ApiProbe(), [new Vec2I(-1, 0), new Vec2I(0, 1)])
            .Build();

    /// <summary>
    /// Places the probe at (4,3) between two chests (input port at (3,3), output port
    /// at (4,4)), stocks the first chest with 8 iron bars and captures the probe's api.
    /// </summary>
    private static (WorldSim Sim, IBuildingApi Api) Setup()
    {
        _lastApi = null;
        var sim = TestWorld.BuildSim(ProbeRegistry());
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new PlaceMachineAt("probe", new Vec2I(4, 3)));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new InsertItemAt("iron_bar", 8, new Vec2I(2, 3)));
        sim.Tick();

        Assert.NotNull(_lastApi);
        Assert.Equal("probe", _lastApi!.Machine.Lite.Kind);
        return (sim, _lastApi);
    }

    [Fact]
    public void Ports_SeeNeighborsAndRejectBadIndexes()
    {
        var (_, api) = Setup();

        Assert.Equal(2, api.PortCount);
        Assert.True(api.Port(0).HasMachine);
        Assert.Equal("chest", api.Port(0).Machine!.Lite.Kind);
        Assert.True(api.Port(1).HasMachine);
        Assert.Throws<ArgumentOutOfRangeException>(() => api.Port(2));
    }

    [Fact]
    public void Pull_MovesItemsIntoOwnInventory_NeverConjures()
    {
        var (_, api) = Setup();

        Assert.Equal(1, api.Port(0).Pull(1));
        Assert.Equal(1, api.CountItems("iron_bar"));

        // The chest has 7 bars left: pulling 99 moves only what exists
        Assert.Equal(7, api.Port(0).Pull(99));
        Assert.Equal(8, api.CountItems());
        Assert.Equal(0, api.Port(0).Pull(1));
    }

    [Fact]
    public void Push_MovesItemsOut_AndKeepsLeftoversHome()
    {
        var (_, api) = Setup();
        api.Port(0).Pull(8);

        Assert.Equal(8, api.Port(1).Push(8));
        Assert.Equal(0, api.CountItems());
        Assert.Equal(8, api.Port(1).Machine!.Inventory.CountItems("iron_bar"));
        Assert.Equal(0, api.Port(1).Push(1)); // own inventory is empty: nothing moves
    }

    [Fact]
    public void GroundInteraction_FlowsThroughOwnInventory()
    {
        var (sim, api) = Setup();
        sim.ReceiveCommand(new DropItemAt("iron_ore", 3, new Vec2(5.5f, 3.5f)));
        sim.Tick();

        Assert.Single(api.GroundItemsNear(2f));
        Assert.Equal(3, api.PickUpItems(2f, "iron_ore"));
        Assert.Equal(3, api.CountItems("iron_ore"));
        Assert.Empty(api.GroundItemsNear(2f));

        Assert.Equal(2, api.DropItems("iron_ore", 2, new Vec2(1.5f, 0)));
        Assert.Equal(1, api.CountItems("iron_ore"));
        Assert.Single(api.GroundItemsNear(2f));

        // Dropping more than owned drops only what exists — never conjures
        Assert.Equal(1, api.DropItems("iron_ore", 99, new Vec2(0, 1.5f)));
        Assert.Equal(0, api.DropItems("iron_ore", 1, new Vec2(0, 1.5f)));
    }

    [Fact]
    public void WorldQueries_AreBoundedByRange()
    {
        var (_, api) = Setup();

        Assert.False(string.IsNullOrEmpty(api.GroundKindAt(new Vec2I(0, 0))));
        Assert.False(string.IsNullOrEmpty(api.GroundKindAt(new Vec2I(2, 2))));
        Assert.Throws<InvalidOperationException>(() => api.GroundKindAt(new Vec2I(40, 0)));
        Assert.Throws<InvalidOperationException>(() => api.DropItems("iron_ore", 1, new Vec2(40, 0)));
    }
}
