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
        new Registry.RegistryBuilder()
            .RegisterEntity("player", new Vec2(0.75f, 0.75f), new InventoryDimensions(0, 16, 0))
            .RegisterGround("stone")
            .RegisterGround("grass")
            .RegisterGround("iron_deposit", spawnsItemKind: "iron_ore", spawnPeriodTicks: 50)
            .RegisterItem("iron_bar", 16)
            .RegisterItem("iron_ore", 32)
            .RegisterItem("chest", 8)
            .RegisterMachine("chest", new Vec2I(2, 1), new InventoryDimensions(0, 8, 0))
            .RegisterMachine("hub", new Vec2I(2, 2), new InventoryDimensions(0, 4, 0),
                playerBuildable: false, indestructible: true)
            .RegisterMachineLogic("chest", new FlowControlBusiness.Machines.Dumb())
            .RegisterMachineLogic("hub", new FlowControlBusiness.Machines.Dumb())
            .Build();

    [Fact]
    public void NonBuildableMachine_IsRejectedForPlayers_ButPlacedBySystem()
    {
        var sim = TestWorld.BuildSim(HubRegistry());
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));

        sim.ReceiveCommand(new PlaceMachineAt("hub", new Vec2I(9, 7), actorId: playerId));
        sim.Tick();
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "a player must not build the hub");

        sim.ReceiveCommand(new PlaceMachineAt("hub", new Vec2I(9, 7)));
        sim.Tick();
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "the system must build the hub");
    }

    [Fact]
    public void IndestructibleMachine_SurvivesPlayers_ButNotTheSystem()
    {
        var sim = TestWorld.BuildSim(HubRegistry());
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new PlaceMachineAt("hub", new Vec2I(9, 7)));
        sim.Tick();

        sim.ReceiveCommand(new RemoveMachineAt(new Vec2I(9, 7), actorId: playerId));
        sim.Tick();
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "a player must not remove the hub");

        sim.ReceiveCommand(new RemoveMachineAt(new Vec2I(9, 7)));
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
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.Tick();

        var chest = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        chest.Inventory.InsertFilter = lite => lite.Kind == "iron_ore";

        sim.ReceiveCommand(new InsertItemAt("iron_bar", 5, new Vec2I(2, 3)));
        sim.ReceiveCommand(new InsertItemAt("iron_ore", 5, new Vec2I(2, 3)));
        sim.Tick();

        Assert.Equal(0, chest.Inventory.CountItems("iron_bar"));
        Assert.Equal(5, chest.Inventory.CountItems("iron_ore"));
    }

    [Fact]
    public void InsertFilter_MakesManipulatorKeepTheItem()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new PlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new InsertItemAt("iron_bar", 4, new Vec2I(2, 3)));
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
