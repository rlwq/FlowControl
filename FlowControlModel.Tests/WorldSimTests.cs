using System.Linq;
using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class WorldSimTests
{
    [Fact]
    public void Manipulator_TransfersItemsBetweenChests()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(2, 3)));

        for (var i = 0; i < 200; i++) sim.Tick();

        var source = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        var target = sim.ChunkManager.GetMachineAt(new Vec2I(4, 4))!;
        Assert.Empty(source.Inventory.EnumerateStacks());
        Assert.Equal(8, target.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void Manipulator_SpendsAFullSwingOnEveryItem()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", new Vec2I(4, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(4, 4)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, new Vec2I(2, 3)));

        // A full cycle (fetch, swing, deliver, swing back) takes ~21 ticks,
        // so after 32 ticks exactly one item must have arrived — not two,
        // as the old fetch-to-deliver shortcut allowed.
        for (var i = 0; i < 32; i++) sim.Tick();

        var target = sim.ChunkManager.GetMachineAt(new Vec2I(4, 4))!;
        Assert.Equal(1, target.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void GetMachineAt_ReturnsNullForEmptyCell()
    {
        var sim = TestWorld.BuildSim();
        Assert.Null(sim.ChunkManager.GetMachineAt(new Vec2I(5, 5)));
    }

    [Fact]
    public void PlaceMachine_RejectsOccupiedRegion()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(3, 3))); // overlaps the first one
        sim.Tick();

        var machine = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(4, 3)));
        Assert.Equal(new Vec2I(2, 3), machine.Coord);
    }

    [Fact]
    public void PlaceMachine_RejectsRegionOccupiedByEntity()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceEntityAt("player", new Vec2(2.5f, 3.5f)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.Tick();

        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(2, 3)));
    }

    [Fact]
    public void MoveEntity_IsBlockedByMachine()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(8, 7)));
        sim.Tick();

        for (var i = 0; i < 100; i++)
        {
            sim.ReceiveCommand(new CmdEntityStepById(playerId, new Vec2(0.25f, 0))); // step right
            sim.Tick();
        }

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.Coord.X < 8f, $"player should be stopped by the chest, but is at {player.Coord}");
    }

    [Fact]
    public void MoveEntity_SlidesAlongBlockedAxis()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(8, 7)));
        sim.Tick();

        // Step diagonally (right + down): X is blocked by the chest, Y should still move
        for (var i = 0; i < 4; i++)
        {
            sim.ReceiveCommand(new CmdEntityStepById(playerId, new Vec2(0.25f, 0.25f)));
            sim.Tick();
        }

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.Coord.Y > 7.5f, "player should slide down along the chest");
    }

    [Fact]
    public void PlaceMachine_OnBehalfOfPlayerRespectsReach()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        sim.ReceiveCommand(new CmdGiveItemsTo(playerId, "chest", 2));

        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(40, 40), actorId: playerId));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(9, 7), actorId: playerId));
        sim.Tick();

        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(40, 40)));
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 7)));
    }

    [Fact]
    public void RemoveMachine_FreesTheRegion()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(2, 3)));
        sim.Tick();
        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(3, 3))); // second tile of the chest
        sim.Tick();

        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(2, 3)));
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(3, 3)));
    }

    [Fact]
    public void SetGround_ReplacesTileAndRaisesEvent()
    {
        var sim = TestWorld.BuildSim();
        var events = 0;
        sim.ChunkManager.GroundTileSet += (_, _) => events++;

        var before = sim.ChunkManager.GetGroundLiteAt(new Vec2I(100, 100)).Kind;
        var newKind = before == "stone" ? "grass" : "stone";
        sim.ReceiveCommand(new CmdSetGroundAt(newKind, new Vec2I(100, 100)));
        sim.Tick();

        Assert.Equal(newKind, sim.ChunkManager.GetGroundLiteAt(new Vec2I(100, 100)).Kind);
        Assert.Equal(1, events);
    }

    /// <summary>
    /// Design invariant: chunks are never unloaded and machines in ALL existing chunks
    /// keep ticking, no matter how far from the origin (or any observer) they are.
    /// </summary>
    [Fact]
    public void MachinesInFarChunksKeepTicking()
    {
        var sim = TestWorld.BuildSim();
        var far = new Vec2I(1000, 1000);
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", far + new Vec2I(0, 0)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("manipulator", far + new Vec2I(2, 0)));
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", far + new Vec2I(2, 1)));
        sim.ReceiveCommand(new CmdInsertItemAt("iron_ingot", 8, far));

        for (var i = 0; i < 200; i++) sim.Tick();

        var target = sim.ChunkManager.GetMachineAt(far + new Vec2I(2, 1))!;
        Assert.Equal(8, target.Inventory.EnumerateStacks().Sum(s => s.Count));
    }

    [Fact]
    public void WorldGeneration_IsDeterministic()
    {
        var simA = TestWorld.BuildSim();
        var simB = TestWorld.BuildSim();

        for (var x = -30; x < 30; x += 7)
        for (var y = -30; y < 30; y += 7)
        {
            var coord = new Vec2I(x, y);
            Assert.Equal(
                simA.ChunkManager.GetGroundLiteAt(coord).Kind,
                simB.ChunkManager.GetGroundLiteAt(coord).Kind);
        }
    }

    [Fact]
    public void MachineOnChunkBorder_IsListedInBothChunks()
    {
        var sim = TestWorld.BuildSim();
        // A 2x1 chest at x = ChunkSize - 1 spans chunks (0,0) and (1,0)
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(TestWorld.ChunkSize - 1, 0)));
        sim.Tick();

        var machine = sim.ChunkManager.GetMachineAt(new Vec2I(TestWorld.ChunkSize - 1, 0))!;
        Assert.Contains(sim.ChunkManager.GetMachinesInChunk(new Vec2I(0, 0)), m => m.Id == machine.Id);
        Assert.Contains(sim.ChunkManager.GetMachinesInChunk(new Vec2I(1, 0)), m => m.Id == machine.Id);

        sim.ReceiveCommand(new CmdRemoveMachineAt(new Vec2I(TestWorld.ChunkSize, 0))); // its second tile
        sim.Tick();

        Assert.DoesNotContain(sim.ChunkManager.GetMachinesInChunk(new Vec2I(0, 0)), m => m.Id == machine.Id);
        Assert.DoesNotContain(sim.ChunkManager.GetMachinesInChunk(new Vec2I(1, 0)), m => m.Id == machine.Id);
    }
}
