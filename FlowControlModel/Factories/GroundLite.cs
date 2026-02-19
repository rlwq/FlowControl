using Godot;

namespace FlowControlModel.Factories;

/// <summary> Represents a ground type. Contains all intrinsic properties of a ground type.</summary>
/// <param name="kind">Ground type's unique identifier.</param>
public class GroundLite(StringName kind)
{
    /// <summary> Ground tile's unique identifier. </summary>
    public readonly StringName Kind = kind;
}
