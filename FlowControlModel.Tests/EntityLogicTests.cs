using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class EntityLogicTests
{
    [Fact]
    public void Cow_WandersAroundOnItsOwn()
    {
        var sim = TestWorld.BuildSim();
        var place = new CmdPlaceEntityAt("cow", new Vec2(50.5f, 50.5f));
        sim.ReceiveCommand(place);
        sim.Tick();
        var cowId = place.EntityId!.Value;
        var start = sim.ChunkManager.GetEntityById(cowId).Coord;

        var moved = false;
        for (var i = 0; i < 500 && !moved; i++)
        {
            sim.Tick();
            moved = sim.ChunkManager.GetEntityById(cowId).Coord != start;
        }

        Assert.True(moved, "the cow should wander away from its spawn point");
    }

    [Fact]
    public void Player_HasNoAutonomousLogic()
    {
        var sim = TestWorld.BuildSim();
        var playerId = TestWorld.SpawnPlayer(sim, new Vec2(7.5f, 7.5f));
        var start = sim.ChunkManager.GetEntityById(playerId).Coord;

        for (var i = 0; i < 200; i++) sim.Tick();

        Assert.Equal(start, sim.ChunkManager.GetEntityById(playerId).Coord);
    }

    [Fact]
    public void RemoveEntityById_RemovesTheEntityAndRaisesEvent()
    {
        var sim = TestWorld.BuildSim();
        var place = new CmdPlaceEntityAt("cow", new Vec2(5.5f, 5.5f));
        sim.ReceiveCommand(place);
        sim.Tick();
        var cowId = place.EntityId!.Value;

        var removed = 0;
        sim.ChunkManager.EntityRemoved += _ => removed++;

        sim.ReceiveCommand(new CmdRemoveEntityById(cowId));
        sim.Tick();

        Assert.Equal(1, removed);
        Assert.Empty(sim.ChunkManager.GetEntitiesInChunk(new Vec2I(0, 0)));
    }
}
