using FlowControlModel;
using FlowControlModel.World;
using Xunit;

namespace FlowControlModel.Tests;

public class WorldGridTests
{
    private readonly WorldGrid _grid = new(20);

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(19, 19, 0, 0, 19, 19)]
    [InlineData(20, 20, 1, 1, 0, 0)]
    [InlineData(-1, -1, -1, -1, 19, 19)]
    [InlineData(-20, -20, -1, -1, 0, 0)]
    [InlineData(-21, 39, -2, 1, 19, 19)]
    public void ToLocalI_SplitsGlobalCoordinates(int gx, int gy, int chunkX, int chunkY, int cellX, int cellY)
    {
        var local = _grid.ToLocalI(new Vec2I(gx, gy));

        Assert.Equal(new Vec2I(chunkX, chunkY), local.Chunk);
        Assert.Equal(new Vec2I(cellX, cellY), local.Cell);
    }

    [Fact]
    public void ToLocal_HandlesNegativeCoordinates()
    {
        var local = _grid.ToLocal(new Vec2(-0.5f, 20.5f));

        Assert.Equal(new Vec2I(-1, 1), local.Chunk);
        Assert.Equal(19.5f, local.Cell.X, precision: 4);
        Assert.Equal(0.5f, local.Cell.Y, precision: 4);
    }

    [Fact]
    public void LocalRectI_CoversExactlyOneChunk()
    {
        Assert.True(_grid.LocalRectI.HasPoint(new Vec2I(0, 0)));
        Assert.True(_grid.LocalRectI.HasPoint(new Vec2I(19, 19)));
        Assert.False(_grid.LocalRectI.HasPoint(new Vec2I(20, 0)));
        Assert.False(_grid.LocalRectI.HasPoint(new Vec2I(-1, 0)));
    }
}
