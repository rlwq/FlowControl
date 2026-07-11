using System.Linq;
using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using Xunit;

namespace FlowControlModel.Tests;

public class MachineFlagTests
{
    /// <summary> The standard test registry plus a Hub-like machine. </summary>
    private static Registry HubRegistry() =>
        TestWorld.StandardBuilder()
            .RegisterMachine("hub", new Vec2I(2, 2), new InventoryDimensions(0, 4, 0),
                playerBuildable: false, indestructible: true)
            .RegisterMachineLogic("hub", new FlowControlBusiness.Machines.Dumb())
            .Build();

    [Fact]
    public void NonBuildableMachine_IsRejectedForPlayers_ButPlacedBySystem()
    {
        var sim = TestWorld.BuildSim(HubRegistry());
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));

        sim.ReceiveCommand(new CmdPlaceMachineAt("hub", new Vec2I(9, 7), actorId: playerId));
        sim.Tick();
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "a player must not build the hub");

        sim.ReceiveCommand(new CmdPlaceMachineAt("hub", new Vec2I(9, 7)));
        sim.Tick();
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "the system must build the hub");
    }

    [Fact]
    public void IndestructibleMachine_SurvivesPlayers_ButNotTheSystem()
    {
        var sim = TestWorld.BuildSim(HubRegistry());
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdPlaceMachineAt("hub", new Vec2I(9, 7)));
        sim.Tick();

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(9, 7), actorId: playerId));
        sim.Tick();
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "a player must not remove the hub");

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(9, 7)));
        sim.Tick();
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "the system may remove the hub");

        // A non-buildable machine leaves no machine item behind
        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Empty(player.Inventory.EnumerateStacks());
    }

    [Fact]
    public void InsertFilter_RejectsForeignItems()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.Tick();

        var chest = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        chest.Inventory.InsertFilter = lite => lite.Kind == "iron_ore";

        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 5, new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ore", 5, new Vec2I(2, 3)));
        sim.Tick();

        Assert.Equal(0, chest.Inventory.CountItems("iron_ingot"));
        Assert.Equal(5, chest.Inventory.CountItems("iron_ore"));
    }

    [Fact]
    public void InsertFilter_MakesManipulatorKeepTheItem()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 4, new Vec2I(2, 3)));
        sim.Tick();

        // The target chest refuses everything: the manipulator must not lose the item
        var target = sim.ChunkManager.GetMachineAt(new Vec2I(4, 4))!;
        target.Inventory.InsertFilter = _ => false;

        for (var i = 0; i < 100; i++) sim.Tick();

        var source = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        var manipulator = sim.ChunkManager.GetMachineAt(new Vec2I(4, 3))!;
        Assert.Equal(0, target.Inventory.CountItems());
        Assert.Equal(1, manipulator.Inventory.CountItems()); // holds one, waiting
        Assert.Equal(3, source.Inventory.CountItems());      // the rest never left
    }
}
