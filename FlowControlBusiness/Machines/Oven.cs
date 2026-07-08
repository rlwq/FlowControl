using FlowControlModel.Machines;
using Godot;

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
        GD.Print(_fuel);
    }
    
    public override MachineLogic Copy() => new Oven();
}
