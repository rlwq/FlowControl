using System;

namespace FlowControlModel.Machines;

/// <summary>
/// Base class for all machine logics. A logic acts on the world exclusively through
/// the <see cref="IBuildingApi"/> it receives every tick: own inventory, ports to
/// observed cells (registered with the logic in the registry), nearby ground items.
/// </summary>
public abstract class MachineLogic : IDisposable
{
    /// <summary>
    /// A short human-readable description of the logic's current state
    /// (for inspection windows and debug overlays), or <c>null</c> for stateless logics.
    /// </summary>
    public virtual string? DisplayState => null;

    /// <summary> Executes one quant of the logic. </summary>
    public abstract void Tick(IBuildingApi building);

    /// <summary>
    /// A copy method used for prototyping. Should copy logic settings.
    /// </summary>
    public abstract MachineLogic Copy();

    /// <summary> Prepares logic to be removed. </summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
