using FlowControlModel.Machines;

namespace FlowControlBusiness.Machines;

/// <summary>
/// A one-cell arm which carries items from the machine under its input port (0)
/// to the machine under its output port (1), one item per full swing.
/// Each half rotation (to the target and back) takes <see cref="HalfRotationTime"/> ticks.
/// </summary>
public class Manipulator : MachineLogic
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
    private const int InputPort = 0;
    private const int OutputPort = 1;

    private State _state = State.Fetching;
    private float _progress = HalfRotationTime;

    /// <inheritdoc/>
    public override string? DisplayState =>
        _progress < HalfRotationTime
            ? $"{_state} ({(int)_progress}/{HalfRotationTime})"
            : $"{_state} (waiting)";

    /// <summary>
    /// Executes one quant of the logic: swings the arm or moves one item.
    /// The swing speed scales with the received electric power (kinds registered
    /// without a power demand always run at full speed).
    /// </summary>
    public override void Tick(IBuildingApi building)
    {
        var power = building.Power;
        if (power <= 0)
            return; // starved: the arm freezes

        // The arm is still swinging towards its target cell
        if (_progress < HalfRotationTime) {
            _progress += power;
            return;
        }

        if (_state == State.Fetching) {
            if (building.Port(InputPort).Pull(1) == 0) return; // nothing to grab yet
            _state = State.Delivering;
            _progress = 0;
            return;
        }

        // Delivering: the arm has arrived at the output cell with an item in hand
        if (building.Port(OutputPort).Push(1) == 1) {
            _state = State.Fetching;
            _progress = 0;
            return;
        }
        if (building.CountItems() == 0) { // the hand was emptied externally: swing back
            _state = State.Fetching;
            _progress = 0;
        }
        // Otherwise the target is missing or full: keep holding the item
    }

    /// <inheritdoc/>
    public override MachineLogic Copy() => new Manipulator();
}
