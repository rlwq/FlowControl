using FlowControlModel.Entities;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// A representation (sprite) of an entity in the game world.
/// </summary>
/// <remarks>
/// <para>Visual representation only. For logic, see <see cref="IEntity"/>.</para>
/// </remarks>
public partial class EntityView : Sprite2D { }
