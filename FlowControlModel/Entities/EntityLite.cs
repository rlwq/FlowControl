using Godot;

namespace FlowControlModel.Entities;

/// <summary>
/// Represents an entity type. Contains all intrinsic properties of an entity type.
/// </summary>
/// <param name="kind"> Entity type's unique identifier. </param>
/// <param name="boxSize"> A vector representing the collison box size (width and height) in grid cells of the entity type. </param>
public class EntityLite(StringName kind, Vector2 boxSize)
{
    /// <summary> Entity type's unique identifier. </summary>
    public readonly StringName Kind = kind;

    /// <summary> A vector representing the collison box size (width and height) in grid cells of the entity type. </summary>
    public readonly Vector2 BoxSize = boxSize;
}
