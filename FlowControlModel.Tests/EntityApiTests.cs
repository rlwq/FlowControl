using System;
using System.Linq;
using FlowControlModel;
using FlowControlModel.Entities;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using Xunit;

namespace FlowControlModel.Tests;

public class EntityApiTests
{
    /// <summary> The api captured by the latest <see cref="ApiProbe"/> tick. </summary>
    private static IEntityApi? _lastApi;

    /// <summary> A logic that captures its <see cref="IEntityApi"/> so tests can drive it directly. </summary>
    private sealed class ApiProbe : EntityLogic
    {
        public override void Tick(IEntityApi entity) => _lastApi = entity;
        public override EntityLogic Copy() => new ApiProbe();
    }

    /// <summary> A logic that requests a constant step each tick. </summary>
    private sealed class Walker(Vec2 step) : EntityLogic
    {
        public override void Tick(IEntityApi entity) => entity.Move(step);
        public override EntityLogic Copy() => new Walker(step);
    }

    private static Registry ProbeRegistry(EntityLogic probeLogic) =>
        TestWorld.StandardBuilder()
            .RegisterEntity("probe", new Vec2(0.75f, 0.75f), new InventoryDimensions(0, 4, 0))
            .RegisterEntityLogic("probe", probeLogic)
            .Build();

    /// <summary> Spawns the probe entity at (7.5, 7.5) next to a stocked chest at (8, 6). </summary>
    private static (WorldSim Sim, IEntityApi Api) Setup()
    {
        _lastApi = null;
        var sim = TestWorld.BuildSim(ProbeRegistry(new ApiProbe()));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(8, 6)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(8, 6)));
        sim.ReceiveCommand(new CmdPlaceEntityAt("probe", new Vec2(7.5f, 7.5f)));
        sim.Tick();

        Assert.NotNull(_lastApi);
        Assert.Equal("probe", _lastApi!.Entity.Lite.Kind);
        return (sim, _lastApi);
    }

    [Fact]
    public void Move_IsAppliedThroughCollisions()
    {
        var sim = TestWorld.BuildSim(ProbeRegistry(new Walker(new Vec2(0.25f, 0))));
        sim.ReceiveCommand(new CmdPlaceEntityAt("probe", new Vec2(5.5f, 7.5f)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(8, 7)));
        sim.Tick();

        for (var i = 0; i < 100; i++) sim.Tick();

        // The walker marches right and must be stopped by the chest at x = 8
        var probe = sim.ChunkManager.GetEntitiesInChunk(new Vec2I(0, 0))
            .Single(e => e.Lite.Kind == "probe");
        Assert.True(probe.Coord.X > 6f, "the walker should have moved right");
        Assert.True(probe.Coord.X < 8f, $"the walker should be blocked by the chest, but is at {probe.Coord}");
    }

    [Fact]
    public void MachinesNear_FindsAndPullsWithinRange()
    {
        var (_, api) = Setup();

        var machine = Assert.Single(api.MachinesNear(3f));
        Assert.Equal("chest", machine.Lite.Kind);

        Assert.Equal(3, api.PullFrom(machine.Id, 3, "iron_ingot"));
        Assert.Equal(3, api.CountItems("iron_ingot"));

        Assert.Equal(2, api.PushTo(machine.Id, 2));
        Assert.Equal(1, api.CountItems());
        Assert.Equal(7, machine.Inventory.CountItems("iron_ingot"));
    }

    [Fact]
    public void PullFrom_OutOfRangeMachine_MovesNothing()
    {
        var (sim, api) = Setup();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(20, 20)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 5, new Vec2I(20, 20)));
        sim.Tick();

        var farChest = sim.ChunkManager.GetMachineAt(new Vec2I(20, 20))!;
        Assert.Equal(0, api.PullFrom(farChest.Id, 5));
        Assert.Equal(0, api.CountItems());
    }

    [Fact]
    public void GroundInteraction_FlowsThroughOwnInventory()
    {
        var (sim, api) = Setup();
        sim.ReceiveCommand(new CmdDropItemAt("iron_ore", 4, new Vec2(8.5f, 7.5f)));
        sim.Tick();

        Assert.Single(api.GroundItemsNear(2f));
        Assert.Equal(4, api.PickUpItems(2f, "iron_ore"));
        Assert.Equal(4, api.CountItems("iron_ore"));

        Assert.Equal(4, api.DropItems("iron_ore", 99, new Vec2(1f, 1f))); // only what exists
        Assert.Equal(0, api.CountItems());

        Assert.Throws<InvalidOperationException>(() => api.DropItems("iron_ore", 1, new Vec2(40, 0)));
        Assert.Throws<InvalidOperationException>(() => api.GroundKindAt(new Vec2I(40, 0)));
        Assert.False(string.IsNullOrEmpty(api.GroundKindAt(new Vec2I(0, 1))));
    }
}
