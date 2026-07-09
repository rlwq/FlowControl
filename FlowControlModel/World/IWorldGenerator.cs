
namespace FlowControlModel.World;

/// <summary>
/// Generates the ground layer of the world.
/// Implementations must be deterministic: the same coordinate always yields the same ground.
/// </summary>
public interface IWorldGenerator
{
    /// <summary> Returns the ground kind at the specified global cell. </summary>
    GroundLite GetGroundAt(Vec2I coord);
}
