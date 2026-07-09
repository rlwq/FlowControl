using System;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Machines;

/// <summary>
/// Defines the contract for interacting with an active machine in the game world.
/// The only view of a machine available outside the model.
/// </summary>
public interface IMachine
{
    /// <summary> Machine's unique identifier. </summary>
    uint Id { get; }

    /// <summary> Machine's Lite object (intrinsic state). Immutable. </summary>
    MachineLite Lite { get; }

    /// <summary> Machine's inventory. </summary>
    Inventory Inventory { get; }

    /// <summary> Machine's global position (top-left corner). Immutable. </summary>
    Vec2I Coord { get; }

    /// <summary> Machine's orientation. Immutable. </summary>
    Rotation Rotation { get; }

    /// <summary> Footprint dimensions with <see cref="Rotation"/> applied. </summary>
    Vec2I Dimensions { get; }

    /// <summary> The machine's area based on its position and rotated dimensions. </summary>
    RectI Rect { get; }

    /// <summary> Coordinate of the machine's end point (Coord + Dimensions). </summary>
    Vec2I End { get; }
}

/// <summary>
/// An active machine in the game world.
/// A composition of some intrinsic, extrinsic properties, logic and inventory.
/// </summary>
internal class Machine : IMachine, IDisposable
{
    private readonly MachineLogic _logic;

    /// <summary>
    /// An active machine in the game world.
    /// A composition of some intrinsic, extrinsic properties, logic and inventory.
    /// </summary>
    public Machine(uint id, Vec2I coord, Rotation rotation, MachineLite lite, MachineLogic logic)
    {
        Id = id;
        _logic = logic;
        Lite = lite;
        Inventory = new Inventory(Lite.InventoryDimensions);
        Coord = coord;
        Rotation = rotation;
    }

    /// <summary> Event used to notify all observers that the machine is being removed. </summary>
    public event Action<Machine>? MachineRemoved;

    /// <summary> Machine's unique identifier. </summary>
    public uint Id { get; }

    /// <summary> <see cref="Machine"/>'s Lite object (intrinsic state). </summary>
    public MachineLite Lite { get; }

    /// <summary>
    /// <see cref="Machine"/>'s inventory object.
    /// Is an empty inventory if not specified else in the <see cref="Registry"/>.
    /// </summary>
    public Inventory Inventory { get; }

    /// <summary> <see cref="Machine"/>'s global position (top-left corner). </summary>
    public Vec2I Coord { get; }

    /// <summary> <see cref="Machine"/>'s orientation. </summary>
    public Rotation Rotation { get; }

    /// <summary> Footprint dimensions with <see cref="Rotation"/> applied. </summary>
    public Vec2I Dimensions => RotationM.RotateDims(Lite.Dimensions, Rotation);

    /// <summary> Calculates the machine's area based on its position and rotated dimensions. </summary>
    public RectI Rect => new(Coord, Dimensions);

    /// <summary> Calculated coordinate of the machine's end point (Coord + Dimensions). </summary>
    public Vec2I End => Coord + Dimensions;

    /// <summary> Executes one quant of its internal logic. </summary>
    public void Tick() => _logic.Tick(this);

    /// <summary>
    /// Used by <see cref="ChunkManager"/> to invoke <see cref="MachineRemoved"/> when removed.
    /// </summary>
    public void Remove() => MachineRemoved?.Invoke(this);

    /// <summary> Prepares to be deleted. </summary>
    public void Dispose()
    {
        _logic.Dispose();
        GC.SuppressFinalize(this);
    }
}
