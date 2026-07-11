using System.Collections.Generic;
using System.Linq;
using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class DepositTests
{
    /// <summary> Finds a few deposit tiles around the origin, materializing their chunks. </summary>
    private static List<Vec2I> FindDeposits(WorldSim sim, int radius, string kind = "iron_deposit")
    {
        var deposits = new List<Vec2I>();
        for (var x = -radius; x < radius; x++)
        for (var y = -radius; y < radius; y++)
        {
            var coord = new Vec2I(x, y);
            if (sim.ChunkManager.GetGroundLiteAt(coord).Kind == kind)
                deposits.Add(coord);
        }
        return deposits;
    }

    [Fact]
    public void Generator_ProducesAllVeinKinds()
    {
        var sim = TestWorld.BuildSim();

        foreach (var kind in new[] { "iron_deposit", "copper_deposit", "coal_deposit" })
        {
            var deposits = FindDeposits(sim, 60, kind);
            Assert.NotEmpty(deposits);
            // Deposits must stay rare: well under 10% of the scanned area
            Assert.InRange(deposits.Count, 1, 60 * 60 * 4 / 10);
        }
    }

    [Fact]
    public void DepositTiles_PeriodicallySpawnOre()
    {
        var sim = TestWorld.BuildSim();
        var deposit = FindDeposits(sim, 60).First();

        for (var i = 0; i <= TestWorld.OreSpawnPeriod; i++)
            sim.Tick();

        var chunkCoord = sim.ChunkManager.Grid.ToLocalI(deposit).Chunk;
        var oreOnTile = sim.ChunkManager.GetGroundItemsInChunk(chunkCoord)
            .Where(i => i.Coord.FloorToI() == deposit)
            .Sum(i => i.Stack.Count);
        Assert.Equal(1, oreOnTile);
    }

    [Fact]
    public void DepositTiles_DoNotStackOreInfinitely()
    {
        var sim = TestWorld.BuildSim();
        var deposit = FindDeposits(sim, 60).First();

        // Many periods pass, but an uncollected tile holds at most one stack
        for (var i = 0; i < TestWorld.OreSpawnPeriod * 4; i++)
            sim.Tick();

        var chunkCoord = sim.ChunkManager.Grid.ToLocalI(deposit).Chunk;
        var stacksOnTile = sim.ChunkManager.GetGroundItemsInChunk(chunkCoord)
            .Count(i => i.Coord.FloorToI() == deposit);
        Assert.Equal(1, stacksOnTile);
    }
}
