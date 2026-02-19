using Godot;

namespace FlowControlModel.Entities;

/// <summary>
/// Defines the contract for looking up an <see cref="Entity"/>'s state but not modifying it.
/// </summary>
public interface IEntity
{
    /// <summary> Static definition data for this machine type. Immutable. </summary>
    EntityLite Lite { get; }

    /// <summary> Global position (center). Immutable. </summary>
    Vector2 Coord { get; }

    /// <summary> The collision box in world coordinates. Immutable. </summary>
    Rect2 Box { get; }

    /// <summary> Machine's unique identifier. </summary>
    uint Id { get; }
}

/// <summary>
/// An Enity instance which can be placed in the world or used as a prototype.
/// </summary>
public class Entity(uint id, EntityLite lite, Vector2 coord) : IEntity
{
    private readonly uint _id = id;
    private readonly EntityLite _lite = lite;

    /// <summary> Machine's unique identifier. </summary>
    public uint Id => _id;

    /// <summary> Static definition data for this machine type. </summary>
    public EntityLite Lite => _lite;

    /// <summary> Global position (center). </summary>
    public Vector2 Coord
    {
        get => coord;
        set => coord = value;
    }

    /// <summary> The collision box in world coordinates. </summary>
    public Rect2 Box => new(Coord - Lite.BoxSize / 2, Lite.BoxSize);
}
