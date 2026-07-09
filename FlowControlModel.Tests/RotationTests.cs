using System.Linq;
using FlowControlModel;
using Xunit;

namespace FlowControlModel.Tests;

public class RotationTests
{
    [Fact]
    public void RotateDims_SwapsAxesOnQuarterTurns()
    {
        var dims = new Vec2I(2, 1);
        Assert.Equal(new Vec2I(2, 1), RotationM.RotateDims(dims, Rotation.North));
        Assert.Equal(new Vec2I(1, 2), RotationM.RotateDims(dims, Rotation.East));
        Assert.Equal(new Vec2I(2, 1), RotationM.RotateDims(dims, Rotation.South));
        Assert.Equal(new Vec2I(1, 2), RotationM.RotateDims(dims, Rotation.West));
    }

    [Fact]
    public void RotateOffset_TurnsNeighborOffsetsClockwise()
    {
        var dims = Vec2I.One;
        var left = new Vec2I(-1, 0);
        Assert.Equal(new Vec2I(-1, 0), RotationM.RotateOffset(left, dims, Rotation.North));
        Assert.Equal(new Vec2I(0, -1), RotationM.RotateOffset(left, dims, Rotation.East));
        Assert.Equal(new Vec2I(1, 0), RotationM.RotateOffset(left, dims, Rotation.South));
        Assert.Equal(new Vec2I(0, 1), RotationM.RotateOffset(left, dims, Rotation.West));
    }

    [Fact]
    public void RotateOffset_MapsFootprintCellsOntoRotatedFootprint()
    {
        var dims = new Vec2I(2, 1);
        var rotatedCells = new[] { new Vec2I(0, 0), new Vec2I(1, 0) }
            .Select(cell => RotationM.RotateOffset(cell, dims, Rotation.East))
            .ToHashSet();

        Assert.Equal([new Vec2I(0, 0), new Vec2I(0, 1)], rotatedCells);
    }

    [Fact]
    public void RotatedMachine_OccupiesRotatedFootprint()
    {
        var sim = TestWorld.BuildSim();
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(8, 8), Rotation.East));
        sim.Tick();

        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(8, 8)));
        Assert.False(sim.ChunkManager.IsCellEmpty(new Vec2I(8, 9)), "a rotated 2x1 chest must occupy the cell below");
        Assert.True(sim.ChunkManager.IsCellEmpty(new Vec2I(9, 8)), "a rotated 2x1 chest must not occupy the cell to the right");

        var machine = sim.ChunkManager.GetMachineAt(new Vec2I(8, 9))!;
        Assert.Equal(Rotation.East, machine.Rotation);
        Assert.Equal(new Vec2I(1, 2), machine.Dimensions);
    }

    [Fact]
    public void RotatedManipulator_TransfersAlongRotatedObservers()
    {
        var sim = TestWorld.BuildSim();
        // Manipulator rotated East: input (-1,0) becomes (0,-1) [above], output (0,1) becomes (-1,0) [left]
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(3, 2)));       // covers (3,2),(4,2) — above
        sim.ReceiveCommand(new PlaceMachineAt("manipulator", new Vec2I(4, 3), Rotation.East));
        sim.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(2, 3)));       // covers (2,3),(3,3) — left
        sim.ReceiveCommand(new InsertItemAt("iron_bar", 8, new Vec2I(3, 2)));

        for (var i = 0; i < 200; i++) sim.Tick();

        var target = sim.ChunkManager.GetMachineAt(new Vec2I(2, 3))!;
        Assert.Equal(8, target.Inventory.EnumerateStacks().Sum(s => s.Count));
    }
}
