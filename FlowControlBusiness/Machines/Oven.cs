using FlowControlModel.Machines;

namespace FlowControlBusiness.Machines;

/// <summary> Logic for the Oven machine. </summary>
public class Oven : MachineLogic
{
    private float _fuel = 10;

    /// <summary> Executes one quant of the logic. </summary>
    public override void Tick(IMachine machineInst)
    {
        if (_fuel > 0)
            _fuel -= 0.1f;
    }
    
    /// <inheritdoc/>
    public override MachineLogic Copy() => new Oven();
}
