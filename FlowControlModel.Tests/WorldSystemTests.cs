using System.Collections.Generic;
using System.Linq;
using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class WorldSystemTests
{
    /// <summary> Records everything it sees and drops an item every five ticks. </summary>
    private sealed class RecordingSystem : IWorldSystem
    {
        public readonly List<ulong> Ticks = [];
        public readonly List<int> RandomRolls = [];

        public void Tick(IWorldContext world)
        {
            Ticks.Add(world.TickCount);
            RandomRolls.Add(world.Random.Next(100));

            if (world.TickCount % 5 == 0)
                world.Enqueue(new CmdDropItemAt("iron_ingot", 1, new Vec2(5.5f, 5.5f)));
        }
    }

    [Fact]
    public void Systems_AreTickedInOrderWithGrowingTickCount()
    {
        var sim = TestWorld.BuildSim();
        var system = new RecordingSystem();
        sim.AddSystem(system);

        for (var i = 0; i < 4; i++) sim.Tick();

        Assert.Equal(new ulong[] { 0, 1, 2, 3 }, system.Ticks);
    }

    [Fact]
    public void SystemCommands_ExecuteOnTheNextTick()
    {
        var sim = TestWorld.BuildSim();
        sim.AddSystem(new RecordingSystem());

        sim.Tick(); // tick 0: the system enqueues a drop
        Assert.Empty(sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(0, 0)));

        sim.Tick(); // the queued command executes here
        Assert.Single(sim.ChunkManager.GetGroundItemsInChunk(new Vec2I(0, 0)));
    }

    [Fact]
    public void SimRandomness_IsDeterministicPerSeed()
    {
        var rollsOf = (int seed) =>
        {
            var registry = TestWorld.BuildRegistry();
            var sim = new WorldSim(
                registry,
                new FlowControlModel.World.WorldGrid(TestWorld.ChunkSize),
                new FlowControlBusiness.WorldGeneration.NoiseWorldGenerator(registry, seed),
                seed);
            var system = new RecordingSystem();
            sim.AddSystem(system);
            for (var i = 0; i < 20; i++) sim.Tick();
            return system.RandomRolls;
        };

        Assert.Equal(rollsOf(42), rollsOf(42));
        Assert.NotEqual(rollsOf(42), rollsOf(43));
    }

    [Fact]
    public void Seed_IsExposedToSystems()
    {
        var registry = TestWorld.BuildRegistry();
        var sim = new WorldSim(
            registry,
            new FlowControlModel.World.WorldGrid(TestWorld.ChunkSize),
            new FlowControlBusiness.WorldGeneration.NoiseWorldGenerator(registry, 7),
            seed: 7);

        var seenSeed = 0;
        sim.AddSystem(new LambdaSystem(world => seenSeed = world.Seed));
        sim.Tick();

        Assert.Equal(7, seenSeed);
        Assert.Equal(7, sim.Seed);
    }

    private sealed class LambdaSystem(System.Action<IWorldContext> action) : IWorldSystem
    {
        public void Tick(IWorldContext world) => action(world);
    }
}
