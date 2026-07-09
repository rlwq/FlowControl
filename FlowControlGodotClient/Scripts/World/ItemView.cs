using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// A representation (sprite) of an item stack lying on the ground.
/// </summary>
/// <remarks>
/// <para>Visual representation only. For logic, see <see cref="IGroundItem"/>.</para>
/// </remarks>
public partial class ItemView : Sprite2D { }
