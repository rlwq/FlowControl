using System.Linq;
using FlowControlModel;
using FlowControlModel.Inventories;
using Xunit;

namespace FlowControlModel.Tests;

public class InventorySectionTests
{
    [Fact]
    public void SectionViews_ExposeAllSlotsIncludingEmpty()
    {
        var inventory = new Inventory(new InventoryDimensions(1, 2, 1));

        Assert.Equal(1, inventory.InputSlots.Count);
        Assert.Equal(2, inventory.BlobSlots.Count);
        Assert.Equal(1, inventory.OutputSlots.Count);
        Assert.All(inventory.BlobSlots, slot => Assert.True(slot.IsEmpty));
    }

    [Fact]
    public void SectionViews_ReflectInsertedItems()
    {
        var inventory = new Inventory(new InventoryDimensions(1, 2, 0));
        var iron = new ItemLite("iron_ingot", 16);
        inventory.InsertItem(new ItemStack(20, iron));

        // 16 fill the input slot, 4 spill into the blob section
        Assert.Equal(16, inventory.InputSlots[0].Count);
        Assert.Equal(4, inventory.BlobSlots.Sum(s => s.Count));
    }

    [Fact]
    public void MachineLogicState_IsExposedThroughIMachine()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(6, 3)));
        sim.Tick();

        var manipulator = sim.ChunkManager.GetMachineAt(new Vec2I(4, 3))!;
        var chest = sim.ChunkManager.GetMachineAt(new Vec2I(6, 3))!;
        Assert.Contains("Fetching", manipulator.LogicState);
        Assert.Null(chest.LogicState);
    }
}
