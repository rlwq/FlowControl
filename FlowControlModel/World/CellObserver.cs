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

    private Machine? _machine;

    /// <summary>
    /// Reference to the <see cref="IMachine"/> occupying one specific cell. `null` if the cell is empty.
    /// </summary>
    public IMachine? Machine => _machine;

    /// <summary>
    /// Constructor used by <see cref="ChunkManager"/> to build a proper <see cref="CellObserver"/>
    /// </summary>
    internal CellObserver(Chunk chunk, Vector2I localCoord, Vector2I globalCoord)
    {
        _chunk = chunk;
        _coord = globalCoord;

        if (!chunk.IsAirAt(localCoord))
        {
            _machine = chunk.GetMachineAt(localCoord);
            _machine.MachineRemoved += OnMachineRemoved;
            return;
        }
        chunk.MachinePlacedInChunk += OnMachinePlacedInChunk;
    }

    /// <summary> Whether the cell is empty. </summary>
    public bool IsEmpty => _machine == null;


    /// <summary> Unsubscribes from all events and prepares to be deleted. </summary>
    public void Dispose()
    {
        if (_machine != null)
            _machine.MachineRemoved -= OnMachineRemoved;
        _chunk.MachinePlacedInChunk -= OnMachinePlacedInChunk;
    }

    private void OnMachinePlacedInChunk(Machine machine)
    {
        if (!machine.Rect.HasPoint(_coord))
            return;
        _machine = machine;
        _chunk.MachinePlacedInChunk -= OnMachinePlacedInChunk;
        _machine.MachineRemoved += OnMachineRemoved;
    }

    private void OnMachineRemoved(Machine machine)
    {
        _machine = null;
        machine.MachineRemoved -= OnMachineRemoved;
        _chunk.MachinePlacedInChunk += OnMachinePlacedInChunk;
    }
}
