using FlowControlModel.Entities;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// A representation (sprite) of an entity in the game world.
/// </summary>
/// <remarks>
/// <para>Visual representation only. For logic, see <see cref="IEntity"/>.</para>
/// <para>The model moves entities in discrete per-tick steps; the view smooths them out:
/// each simulation tick commits the previous target, and every rendered frame interpolates
/// the sprite between the last two tick positions (rendering one tick behind).</para>
/// </remarks>
public partial class EntityView : Sprite2D
{
    private Vector2 _previous;
    private Vector2 _target;

    /// <summary> Places the sprite immediately, with no interpolation (spawn, chunk load). </summary>
    public void Snap(Vector2 position)
    {
        _previous = _target = position;
        Position = position;
    }

    /// <summary> Sets the position reached by the entity at the end of the current tick. </summary>
    public void MoveTo(Vector2 position) => _target = position;

    /// <summary>
    /// Fixes the current target as the interpolation origin of the next tick.
    /// Called right before every simulation tick.
    /// </summary>
    public void CommitTick() => _previous = _target;

    /// <summary> Renders the sprite between the last two tick positions. </summary>
    /// <param name="alpha"> Fraction of the current tick that has elapsed, 0..1. </param>
    public void Interpolate(float alpha) => Position = _previous.Lerp(_target, alpha);
}
