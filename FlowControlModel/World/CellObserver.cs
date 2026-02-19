using System;
using FlowControlModel.Machines;
using Godot;

namespace FlowControlModel.World;

/// <summary>
/// Observes one specific cell in the world and provides reference to the machine occupying it.
/// </summary>
public sealed class CellObserver : IDisposable
{
    private readonly Vector2I _coord;
    private readonly Chunk _chunk;
    
    /// <summary>
    /// Reference to the <see cref="Machine"/> occupying one specific cell. `null` if the cell is empty.
    /// </summary>
    public Machine? Machine { get; private set; } = null;
    
    /// <summary>
    /// Constructor used by <see cref="ChunkManager"/> to build a proper <see cref="CellObserver"/>
    /// </summary>
    public CellObserver(Chunk chunk, Vector2I localCoord, Vector2I globalCoord)
    {
        _chunk = chunk;
        _coord = globalCoord;

        if (!chunk.IsAirAt(localCoord))
        {
            Machine = chunk.GetMachineAt(localCoord);
            Machine.MachineRemoved += OnMachineRemoved;
            return;
        }
        chunk.MachinePlacedInChunk += OnMachinePlacedInChunk;
    }
    
    /// <summary> Whether the cell is empty. </summary>
    public bool IsEmpty => Machine == null;
    
    
    /// <summary> Unsubscribes from all events and prepares to be deleted. </summary>
    public void Dispose()
    {
        if (Machine != null)
            Machine.MachineRemoved -= OnMachineRemoved;
        _chunk.MachinePlacedInChunk -= OnMachinePlacedInChunk;
    }

    private void OnMachinePlacedInChunk(Machine machine)
    {
        if (!machine.Rect.HasPoint(_coord))
            return;
        Machine = machine;
        _chunk.MachinePlacedInChunk -= OnMachinePlacedInChunk;
        Machine.MachineRemoved += OnMachineRemoved;
    }

    private void OnMachineRemoved(Machine machine)
    {
        Machine = null;
        machine.MachineRemoved -= OnMachineRemoved;
        _chunk.MachinePlacedInChunk += OnMachinePlacedInChunk;
    }
}
