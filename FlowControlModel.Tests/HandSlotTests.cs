using System.Linq;
using FlowControlModel;
using FlowControlModel.Inventories;
using Xunit;

namespace FlowControlModel.Tests;

public class HandSlotTests
{
    [Fact]
    public void EmptyHand_TakesTheWholeStackFromOwnInventory()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 5));
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Equal("iron_bar", player.HandStack.Lite.Kind);
        Assert.Equal(5, player.HandStack.Count);
        Assert.Empty(player.Inventory.EnumerateStacks());
    }

    [Fact]
    public void HeldStack_GoesIntoAMachineSlotAndBack()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(8, 7)));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 5));
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0));
        sim.Tick();

        var chest = sim.ChunkManager.GetMachineAt(new Vec2I(8, 7))!;

        // Put the held bars into the chest's second slot, then take them back
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 1, chest.Id));
        sim.Tick();
        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.HandStack.IsEmpty);
        Assert.Equal(5, chest.Inventory.BlobSlots[1].Count);

        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 1, chest.Id));
        sim.Tick();
        Assert.Equal(5, player.HandStack.Count);
        Assert.True(chest.Inventory.BlobSlots[1].IsEmpty);
    }

    [Fact]
    public void SameKindStacks_MergeUpToStackSize()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        // Two separate iron_bar stacks: 14 in slot 0 (stack size 16), 5 in hand
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 14));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 5));
        sim.Tick();
        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Equal(16, player.Inventory.BlobSlots[0].Count);
        Assert.Equal(3, player.Inventory.BlobSlots[1].Count);

        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 1)); // 3 in hand
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0)); // merge: full slot
        sim.Tick();

        Assert.Equal(16, player.Inventory.BlobSlots[0].Count);
        Assert.Equal(3, player.HandStack.Count); // nothing fit, stays in hand
    }

    [Fact]
    public void DifferentKinds_Swap()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 5));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_ore", 7));
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0)); // bars in hand
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 1)); // swap with ore
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.Equal("iron_ore", player.HandStack.Lite.Kind);
        Assert.Equal("iron_bar", player.Inventory.BlobSlots[1].Lite.Kind);
    }

    [Fact]
    public void PlaceMachine_ConsumesFromTheHandFirst()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "chest", 2));
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(9, 7), actorId: playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)));
        Assert.Equal(1, player.HandStack.Count);
        Assert.Empty(player.Inventory.EnumerateStacks());
    }

    [Fact]
    public void ReturnHand_PutsTheStackBackIntoTheInventory()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new GiveItemsTo(playerId, "iron_bar", 5));
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0));
        sim.ReceiveCommand(new ReturnHand(playerId));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.HandStack.IsEmpty);
        Assert.Equal(5, player.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void ExchangeWithMachine_OutOfReach_IsRejected()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(40, 40)));
        sim.ReceiveCommand(new InsertItemAt("iron_bar", 5, new Vec2I(40, 40)));
        sim.Tick();

        var chest = sim.ChunkManager.GetMachineAt(new Vec2I(40, 40))!;
        sim.ReceiveCommand(new ExchangeSlotWithHand(playerId, InventorySection.Blob, 0, chest.Id));
        sim.Tick();

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.HandStack.IsEmpty);
        Assert.Equal(5, chest.Inventory.BlobSlots[0].Count);
    }
}
