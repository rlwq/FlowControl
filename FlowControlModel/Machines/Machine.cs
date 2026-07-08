using System;
using Godot;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Machines;

public interface IMachine
{
    uint Id { get; }
    MachineLite Lite { get; }
    Inventory Inventory { get; }
    Vector2I Coord { get; }
    Rect2I Rect { get; }
    Vector2I End { get; }
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
    public Machine(uint id, Vector2I coord, MachineLite lite, MachineLogic logic)
    {
        Id = id;
        _logic = logic;
        Lite = lite;
        Inventory = new Inventory(Lite.InventoryDimensions);
        Coord = coord;
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
    public Vector2I Coord { get; }

    /// <summary> Calculates the machine's area based on its position and dimensions. </summary>
    public Rect2I Rect => new(Coord, Lite.Dimensions);

    /// <summary> Calculated coordinate of the machine's end point (Coord + Dimensions). </summary>
    public Vector2I End => Coord + Lite.Dimensions;
    
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
