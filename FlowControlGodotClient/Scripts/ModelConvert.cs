using FlowControlModel;
using Godot;

namespace FlowControlGodotClient;

/// <summary>
/// Conversions between the model's engine-independent primitives and Godot types.
/// The model knows nothing about Godot; every coordinate crossing the boundary goes through here.
/// </summary>
public static class ModelConvert
{
    public static Vector2I ToGodot(this Vec2I v) => new(v.X, v.Y);

    public static Vector2 ToGodot(this Vec2 v) => new(v.X, v.Y);

    public static Vec2I ToModel(this Vector2I v) => new(v.X, v.Y);

    public static Vec2 ToModel(this Vector2 v) => new(v.X, v.Y);
}
