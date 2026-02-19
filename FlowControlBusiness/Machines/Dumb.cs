using FlowControlModel.Machines;

namespace FlowControlBusiness.Machines;

public class Dumb : MachineLogic
{
    /// <summary> Does nothing. </summary>
    public override void Tick(Machine machineInst) { }

    public override MachineLogic Copy() { return new Dumb(); }
}