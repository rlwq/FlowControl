using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.World;
using Xunit;

namespace FlowControlModel.Tests;

public class GroundPropertiesTests
{
    /// <summary> Grass everywhere, a water lake at x ≥ 10, a fast stone path at y = 5. </summary>
    private sealed class MapStub(Registry registry) : IWorldGenerator
    {
        private readonly GroundLite _grass = registry.GetGroundLite("grass");
        private readonly GroundLite _water = registry.GetGroundLite("water");
        private readonly GroundLite _stone = registry.GetGroundLite("stone");

        public GroundLite GetGroundAt(Vec2I coord) =>
            coord.X >= 10 ? _water
            : coord.Y == 5 ? _stone
            : _grass;
    }

    private static WorldSim BuildSim()
    {
        var registry = TestWorld.BuildRegistry();
        return new WorldSim(registry, new WorldGrid(TestWorld.ChunkSize), new MapStub(registry), seed: 1);
    }

    [Fact]
    public void Water_BlocksEntityMovement()
    {
        var sim = BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(8.5f, 2.5f));

        for (var i = 0; i < 100; i++)
        {
            sim.ReceiveCommand(new CmdEntityStepById(playerId, new Vec2(0.3f, 0)));
            sim.Tick();
        }

        var player = sim.ChunkManager.GetEntityById(playerId);
        Assert.True(player.Coord.X < 10f, $"the shoreline must stop the player, but they are at {player.Coord}");
    }

    [Fact]
    public void Water_RejectsMachineAndEntityPlacement()
    {
        var sim = BuildSim();
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(11, 2)));
        var cow = new CmdPlaceEntityAt("cow", new Vec2(12.5f, 2.5f));
        sim.ReceiveCommand(cow);
        sim.Tick();

        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(11, 2)));
        Assert.Null(cow.EntityId);
    }

    [Fact]
    public void MachineOverlappingShoreline_IsRejected()
    {
        var sim = BuildSim();
        // A 2x1 chest at (9,2) covers (9,2) on grass and (10,2) on water
        sim.ReceiveCommand(new CmdPlaceMachineAt("chest", new Vec2I(9, 2)));
        sim.Tick();

        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 2)));
    }

    [Fact]
    public void StonePath_SpeedsMovementUp()
    {
        var sim = BuildSim();
        var onGrass = TestWorld.SpawnPlayer(sim, new Vec2(2.5f, 2.5f));
        var onStone = TestWorld.SpawnPlayer(sim, new Vec2(2.5f, 5.5f));

        sim.ReceiveCommand(new CmdEntityStepById(onGrass, new Vec2(1f, 0)));
        sim.ReceiveCommand(new CmdEntityStepById(onStone, new Vec2(1f, 0)));
        sim.Tick();

        Assert.Equal(3.5f, sim.ChunkManager.GetEntityById(onGrass).Coord.X, 3);
        Assert.Equal(3.75f, sim.ChunkManager.GetEntityById(onStone).Coord.X, 3); // 1.25x on stone
    }
}
