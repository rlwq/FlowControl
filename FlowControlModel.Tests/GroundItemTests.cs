using System.Linq;
using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class GroundItemTests
{
    [Fact]
    public void DropItemAt_CreatesGroundItemAndRaisesEvent()
    {
        var sim = TestWorld.BuildSim();
        var events = 0;
        // Deposit tiles spawn their own ore; count only the ingots we drop
        sim.ChunkManager.GroundItemPlaced += item =>
            events += item.Stack.Lite.Kind == "iron_ingot" ? 1 : 0;

        sim.ReceiveCommand(new CmdDropItemAt("iron_ingot", 3, new Vec2(5.5f, 5.5f)));
        sim.Tick();

        var items = sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(0, 0))
            .Where(i => i.Stack.Lite.Kind == "iron_ingot");
        var item = Assert.Single(items);
        Assert.Equal(3, item.Stack.Count);
        Assert.Equal(1, events);
    }

    [Fact]
    public void PickUpItemAt_MovesItemsIntoActorInventory()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdDropItemAt("iron_ingot", 3, new Vec2(8.2f, 7.5f)));
        sim.Tick();

        sim.ReceiveCommand(new CmdPickUpItemAt(new Vec2(7.5f, 7.5f), playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Empty(sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(0, 0))
            .Where(i => i.Stack.Lite.Kind == "iron_ingot"));
        Assert.Equal(3, player.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void PickUpItemAt_OutOfReach_LeavesItemsOnTheGround()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdDropItemAt("iron_ingot", 3, new Vec2(30.5f, 30.5f)));
        sim.ReceiveCommand(new CmdPickUpItemAt(new Vec2(30.5f, 30.5f), playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Single(sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(1, 1)));
        Assert.Empty(player.Inventory.EnumerateStacks());
    }

    [Fact]
    public void RemoveMachine_SpillsItsInventoryOnTheGround()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 5, new Vec2I(2, 3)));
        sim.Tick();

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(2, 3)));
        sim.Tick();

        var spilled = sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(0, 0))
            .Where(i => i.Stack.Lite.Kind == "iron_ingot")
            .Sum(i => i.Stack.Count);
        Assert.Equal(5, spilled);
    }

    [Fact]
    public void PlaceMachine_ByPlayer_ConsumesTheMachineItem()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdGiveItemsTo(playerId, "chest", 1));

        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(9, 7), actorId: playerId));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(9, 9), actorId: playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)), "the first placement should succeed");
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 9)), "the second placement should fail: no item left");
        Assert.Empty(player.Inventory.EnumerateStacks());
    }

    [Fact]
    public void RemoveMachine_ByPlayer_ReturnsTheMachineItem()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdGiveItemsTo(playerId, "chest", 1));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(9, 7), actorId: playerId));
        sim.Tick();

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(9, 7), actorId: playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        var stack = Assert.Single(player.Inventory.EnumerateStacks());
        Assert.Equal("chest", stack.Lite.Kind);
        Assert.Equal(1, stack.Count);
    }
}
