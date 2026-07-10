using System;

namespace FlowControlModel.Entities;

/// <summary>
/// Base class for all entity logics (autonomous behavior of non-player entities).
/// A logic acts on the world exclusively through the <see cref="IEntityApi"/> it receives
/// every tick: movement requests, its own inventory, ground items and machines around.
/// Ticked by the simulation analogously to <c>MachineLogic</c>.
/// </summary>
public abstract class EntityLogic : IDisposable
{
    /// <summary> Executes one quant of the logic. </summary>
    public abstract void Tick(IEntityApi entity);

    /// <summary> A copy method used for prototyping. Should copy logic settings. </summary>
    public abstract EntityLogic Copy();

    /// <summary> Prepares logic to be removed. </summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
