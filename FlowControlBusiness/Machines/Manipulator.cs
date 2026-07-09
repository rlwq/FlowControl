using System.Collections.Generic;
using System.Diagnostics;
using FlowControlModel.Machines;
using FlowControlModel.World;

namespace FlowControlBusiness.Machines;

/// <summary>
/// A one-cell arm which carries items from the machine under its input observer
/// to the machine under its output observer, one item per full swing.
/// Each half rotation (to the target and back) takes <see cref="HalfRotationTime"/> ticks.
/// </summary>
public class Manipulator : MachineInteractiveLogic
{
    /// <summary> Which half of the swing cycle the arm is in. </summary>
    public enum State
    {
        /// <summary> The arm carries an item towards the output cell. </summary>
        Delivering,
        /// <summary> The arm returns empty-handed towards the input cell. </summary>
        Fetching,
    }

    private const int HalfRotationTime = 10;

    private CellObserver _input = null!;
    private CellObserver _output = null!;
    private State _state = State.Fetching;
    private int _motionTicks = HalfRotationTime;

    /// <inheritdoc/>
    public override string? DisplayState =>
        _motionTicks < HalfRotationTime
            ? $"{_state} ({_motionTicks}/{HalfRotationTime})"
            : $"{_state} (waiting)";

    /// <inheritdoc/>
    public override void LinkObservers(IList<CellObserver> observers)
    {
        Debug.Assert(observers is { Count: 2 });
        RegisterObservers(observers);
        _input = observers[0];
        _output = observers[1];
    }

    /// <summary> Executes one quant of the logic: swings the arm or moves one item. </summary>
    public override void Tick(IMachine machineInst)
    {
        // The arm is still swinging towards its target cell
        if (_motionTicks < HalfRotationTime) {
            _motionTicks++;
            return;
        }

        if (_state == State.Fetching) {
            if (_input.IsEmpty) return;
            var item = _input.Machine!.Inventory.Extract(1);
            if (item.IsEmpty) return; // the source machine has nothing to give
            machineInst.Inventory.InsertItem(item);
            _state = State.Delivering;
            _motionTicks = 0;
            return;
        }

        // Delivering: the arm has arrived at the output cell with an item in hand
        if (_output.IsEmpty) return;
        var held = machineInst.Inventory.Extract(1);
        if (held.IsEmpty) { // nothing to deliver: swing back empty-handed
            _state = State.Fetching;
            _motionTicks = 0;
            return;
        }
        var leftover = _output.Machine!.Inventory.InsertItem(held);
        if (!leftover.IsEmpty) { // the target is full: keep holding the item
            machineInst.Inventory.InsertItem(leftover);
            return;
        }
        _state = State.Fetching;
        _motionTicks = 0;
    }

    /// <inheritdoc/>
    public override MachineLogic Copy() => new Manipulator();
}
