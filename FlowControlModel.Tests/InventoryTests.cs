using System.Linq;
using FlowControlModel.Inventories;
using Xunit;

namespace FlowControlModel.Tests;

public class InventoryTests
{
    private static readonly ItemLite Iron = new("iron_ingot", 16);

    [Fact]
    public void InsertItem_ReturnsLeftoverWhenFull()
    {
        var inventory = new Inventory(0, 1, 0); // single blob slot, stack size 16

        var leftover = inventory.InsertItem(new ItemStack(20, Iron));

        Assert.Equal(4, leftover.Count);
        Assert.Equal(16, inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void Extract_TakesUpToRequestedAmount()
    {
        var inventory = new Inventory(0, 8, 0);
        inventory.InsertItem(new ItemStack(8, Iron));

        var extracted = inventory.Extract(3);

        Assert.Equal(3, extracted.Count);
        Assert.Equal(Iron, extracted.Lite);
        Assert.Equal(5, inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void Extract_FromEmptyInventoryReturnsEmptyStack()
    {
        var inventory = new Inventory(0, 8, 0);

        var extracted = inventory.Extract(1);

        Assert.True(extracted.IsEmpty);
    }

    [Fact]
    public void InsertItem_MergesIntoExistingStacks()
    {
        var inventory = new Inventory(0, 2, 0);
        inventory.InsertItem(new ItemStack(10, Iron));

        var leftover = inventory.InsertItem(new ItemStack(10, Iron));

        Assert.True(leftover.IsEmpty);
        Assert.Equal(20, inventory.EnumerateStacks().Sum(s => s.Count));
    }
}
