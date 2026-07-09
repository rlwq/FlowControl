using FlowControlModel.Machines;

namespace FlowControlBusiness.Machines;

/// <summary>
/// A no-op logic for passive machines (chests, doors): they hold state but do nothing on their own.
/// </summary>
public class Dumb : MachineLogic
{
    /// <summary> Does nothing. </summary>
    public override void Tick(IMachine machineInst) { }

    /// <inheritdoc/>
    public override MachineLogic Copy() { return new Dumb(); }
}
