using System.Collections.Generic;
using System.Diagnostics;
using FlowControlModel.Machines;
using FlowControlModel.World;
using Godot;

namespace FlowControlBusiness.Machines;

public class Manipulator : MachineInteractiveLogic
{
    public enum State { Delivering, Fetching }
    private const int HalfRotationTime = 10;
    
    private CellObserver _input = null!;
    private CellObserver _output = null!;
    private State _state = State.Delivering;
    private int _motionTicks;
    
    public override void LinkObservers(IList<CellObserver> observers)
    {
        Debug.Assert(observers is { Count: 2 });
        RegisterObservers(observers);
        _input = observers[0];
        _output = observers[1];
    }
    
    public override void Tick(Machine machineInst)
    {
        if (_motionTicks < HalfRotationTime) {
            _motionTicks++;
            return;
        }
        if (_state == State.Delivering) {
            if (_output.IsEmpty) return;
            GD.Print(_output.IsEmpty);
            _motionTicks = 0;
            var insertedItem = machineInst.Inventory.Extract(1);
            _output.Machine!.Inventory.InsertItem(insertedItem);
            _state = State.Fetching;
            return;
        }
        
        if (_input.IsEmpty) return;
        var extractedItem = _input.Machine!.Inventory.Extract(1);
        machineInst.Inventory.InsertItem(extractedItem);
        _state = State.Delivering;
    }

    public override MachineLogic Copy() => new Manipulator();
}
