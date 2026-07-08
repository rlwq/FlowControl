using System;
using System.Collections.Generic;
using FlowControlModel.World;

namespace FlowControlModel.Machines;

/// <summary> Base class for all machine logics. </summary>
public abstract class MachineLogic : IDisposable
{
    /// <summary> Executes one quant of the logic. </summary>
    public abstract void Tick(IMachine machineInst);
    
    /// <summary>
    /// A copy method used for prototyping. Should copy logic settings.
    /// </summary>
    public abstract MachineLogic Copy();
    
    /// <summary> Prepares logic to be removed. </summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary> Base class for all machine logics which interact with other machines in the world. </summary>
public abstract class MachineInteractiveLogic : MachineLogic
{
    private readonly List<CellObserver> _cellObservers = [];
    
    /// <summary> Register all observers a machine uses to free them later. </summary>
    protected void RegisterObservers(IList<CellObserver> observers)
    {
        foreach (var observer in observers)
            _cellObservers.Add(observer);
    }
    
    /// <summary>
    /// Receive all observers a machine uses and save them to its inner fields.
    /// Must call <see cref="RegisterObservers"/>
    /// </summary>
    public abstract void LinkObservers(IList<CellObserver> observers);
    
    /// <summary> Prepares logic to be removed. </summary>
    public override void Dispose()
    {
        foreach (var observer in _cellObservers)
            observer.Dispose();
        _cellObservers.Clear();
        GC.SuppressFinalize(this);
    }
}
