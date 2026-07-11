using System.Linq;
using FlowControlBusiness.Content;
using FlowControlModel;
using FlowControlModel.World;
using Xunit;

namespace FlowControlModel.Tests;

/// <summary>
/// Tests for the per-kind content files: one JSON file per kind, the file name is the kind,
/// core data only (visuals live in a parallel tree consumed by the client).
/// </summary>
public class ContentManifestTests
{
    /// <summary> Mimics the shipped Content/ tree, fed as (kind, file text) pairs. </summary>
    private static ContentLoader MainLoader() =>
        new ContentLoader(LogicCatalog.Standard())
            .AddGround("stone", "{}")
            .AddGround("grass", "{}")
            .AddGround("iron_deposit", """{ "spawnsItem": "iron_ore", "spawnPeriodTicks": 300 }""")
            .AddItem("iron_ingot", """{ "stackSize": 16 }""")
            .AddItem("iron_ore", """{ "stackSize": 32 }""")
            .AddItem("chest", """{ "stackSize": 8 }""")
            .AddItem("manipulator", """{ "stackSize": 8 }""")
            .AddMachine("chest", """{ "dimensions": [2, 1], "inventory": [0, 8, 0], "logic": "dumb" }""")
            .AddMachine("manipulator", """
                {
                    "dimensions": [1, 1],
                    "inventory": [0, 1, 0],
                    "logic": "manipulator",
                    "observerOffsets": [[-1, 0], [0, 1]]
                }
                """)
            .AddEntity("cow", """{ "boxSize": [1, 1], "logic": "wanderer", "logicParams": { "speed": 0.06 } }""");

    [Fact]
    public void ContentFiles_BuildAWorkingRegistry()
    {
        var registry = MainLoader().BuildRegistry();

        Assert.Equal(new Vec2I(2, 1), registry.GetMachineLite("chest").Dimensions);
        Assert.Equal(8, registry.GetMachineLite("chest").InventoryDimensions.Blob);
        Assert.Equal(16, registry.GetItemLite("iron_ingot").StackSize);
        Assert.Equal(new Vec2(1, 1), registry.GetEntityLite("cow").BoxSize);
        Assert.NotNull(registry.GetGroundLite("iron_deposit").Spawner);
        Assert.Equal("iron_ore", registry.GetGroundLite("iron_deposit").Spawner!.Item.Kind);
    }

    [Fact]
    public void ContentWorld_RunsTheFullItemFlow()
    {
        var registry = MainLoader().BuildRegistry();
        var sim = new WorldSim(registry, new WorldGrid(20), new SingleGroundStub(registry));

        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(2, 3)));
        for (var i = 0; i < 200; i++) sim.Tick();

        var target = sim.ChunkManager.GetMachineAt(new Vec2I(4, 4))!;
        Assert.Equal(8, target.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void ExtraFiles_AddKindsWithoutTouchingCode()
    {
        var registry = MainLoader()
            .AddItem("copper_ingot", """{ "stackSize": 16 }""")
            .BuildRegistry();

        Assert.Equal(16, registry.GetItemLite("copper_ingot").StackSize);
    }

    [Fact]
    public void RedefinedKind_Throws()
    {
        var loader = MainLoader();
        var error = Assert.Throws<ContentException>(
            () => loader.AddItem("iron_ingot", """{ "stackSize": 99 }"""));
        Assert.Contains("iron_ingot", error.Message);
    }

    [Fact]
    public void UnknownLogicName_Throws()
    {
        var loader = new ContentLoader(LogicCatalog.Standard())
            .AddItem("teleporter", """{ "stackSize": 1 }""")
            .AddMachine("teleporter", """{ "dimensions": [1, 1], "logic": "quantum" }""");
        var error = Assert.Throws<ContentException>(() => loader.BuildRegistry());
        Assert.Contains("quantum", error.Message);
    }

    [Fact]
    public void MachineWithoutMatchingItem_Throws()
    {
        var loader = new ContentLoader(LogicCatalog.Standard())
            .AddMachine("ghost_machine", """{ "dimensions": [1, 1] }""");
        var error = Assert.Throws<ContentException>(() => loader.BuildRegistry());
        Assert.Contains("ghost_machine", error.Message);
    }

    [Fact]
    public void NonBuildableMachine_NeedsNoItemPair()
    {
        var registry = new ContentLoader(LogicCatalog.Standard())
            .AddMachine("hub", """{ "dimensions": [2, 2], "playerBuildable": false, "indestructible": true }""")
            .BuildRegistry();

        var lite = registry.GetMachineLite("hub");
        Assert.False(lite.PlayerBuildable);
        Assert.True(lite.Indestructible);
    }

    [Fact]
    public void Player_IsOrdinaryContent()
    {
        var registry = new ContentLoader(LogicCatalog.Standard())
            .AddEntity("player", """{ "boxSize": [0.75, 0.75], "inventory": [0, 16, 0] }""")
            .BuildRegistry();

        var lite = registry.GetEntityLite("player");
        Assert.Equal(new Vec2(0.75f, 0.75f), lite.BoxSize);
        Assert.Equal(16, lite.InventoryDimensions.Blob);
    }

    [Fact]
    public void MalformedJson_ThrowsWithTheKindName()
    {
        var loader = new ContentLoader(LogicCatalog.Standard());
        var error = Assert.Throws<ContentException>(() => loader.AddItem("broken", "{ not json"));
        Assert.Contains("broken", error.Message);
    }

    /// <summary> A single-ground generator stub so the test does not depend on noise content. </summary>
    private sealed class SingleGroundStub(FlowControlModel.Factories.Registry registry) : IWorldGenerator
    {
        private readonly GroundLite _grass = registry.GetGroundLite("grass");
        public GroundLite GetGroundAt(Vec2I coord) => _grass;
    }
}
