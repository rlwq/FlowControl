using System;

namespace FlowControlModel.Entities;

/// <summary>
/// Base class for all entity logics (autonomous behavior of non-player entities).
/// Ticked by the simulation analogously to <c>MachineLogic</c>.
/// </summary>
public abstract class EntityLogic : IDisposable
{
    /// <summary>
    /// Executes one quant of the logic.
    /// Returns the desired movement delta for this tick; the simulation applies it
    /// through the regular collision-aware movement path.
    /// </summary>
    public abstract Vec2 Tick(IEntity entity);

    /// <summary> A copy method used for prototyping. Should copy logic settings. </summary>
    public abstract EntityLogic Copy();

    /// <summary> Prepares logic to be removed. </summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
