using System;

namespace FlowControlModel;

/// <summary> An integer 2D vector. Engine-independent replacement for Godot's Vector2I. </summary>
public readonly record struct Vec2I(int X, int Y)
{
    public static readonly Vec2I Zero = new(0, 0);
    public static readonly Vec2I One = new(1, 1);

    public static Vec2I operator +(Vec2I a, Vec2I b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2I operator -(Vec2I a, Vec2I b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2I operator *(Vec2I a, int k) => new(a.X * k, a.Y * k);

    public override string ToString() => $"({X}, {Y})";
}

/// <summary> A float 2D vector. Engine-independent replacement for Godot's Vector2. </summary>
public readonly record struct Vec2(float X, float Y)
{
    public static readonly Vec2 Zero = new(0, 0);

    public static implicit operator Vec2(Vec2I v) => new(v.X, v.Y);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, float k) => new(a.X * k, a.Y * k);
    public static Vec2 operator /(Vec2 a, float k) => new(a.X / k, a.Y / k);

    /// <summary> Euclidean distance to another point. </summary>
    public float DistanceTo(Vec2 other)
    {
        float dx = other.X - X, dy = other.Y - Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary> Component-wise floor, returning an integer vector. </summary>
    public Vec2I FloorToI() => new(MathM.FloorToInt(X), MathM.FloorToInt(Y));

    public override string ToString() => $"({X}, {Y})";
}

/// <summary> An integer axis-aligned rectangle. Engine-independent replacement for Godot's Rect2I. </summary>
public readonly record struct RectI(Vec2I Position, Vec2I Size)
{
    public RectI(int x, int y, int width, int height)
        : this(new Vec2I(x, y), new Vec2I(width, height)) { }

    /// <summary> The corner opposite to <see cref="Position"/> (Position + Size). </summary>
    public Vec2I End => Position + Size;

    /// <summary> Whether the point lies inside the rectangle (the end edges are exclusive). </summary>
    public bool HasPoint(Vec2I point) =>
        point.X >= Position.X && point.Y >= Position.Y
        && point.X < End.X && point.Y < End.Y;

    /// <summary> The center of the rectangle. </summary>
    public Vec2 GetCenter() => new(Position.X + Size.X / 2f, Position.Y + Size.Y / 2f);

    public static implicit operator Rect(RectI r) => new(r.Position, new Vec2(r.Size.X, r.Size.Y));
}

/// <summary> A float axis-aligned rectangle. Engine-independent replacement for Godot's Rect2. </summary>
public readonly record struct Rect(Vec2 Position, Vec2 Size)
{
    public Rect(float x, float y, float width, float height)
        : this(new Vec2(x, y), new Vec2(width, height)) { }

    /// <summary> The corner opposite to <see cref="Position"/> (Position + Size). </summary>
    public Vec2 End => Position + Size;

    /// <summary> Whether the point lies inside the rectangle (the end edges are exclusive). </summary>
    public bool HasPoint(Vec2 point) =>
        point.X >= Position.X && point.Y >= Position.Y
        && point.X < End.X && point.Y < End.Y;

    /// <summary> Whether the rectangles overlap (touching edges do not count). </summary>
    public bool Intersects(Rect other) =>
        Position.X < other.End.X && End.X > other.Position.X
        && Position.Y < other.End.Y && End.Y > other.Position.Y;

    /// <summary> The center of the rectangle. </summary>
    public Vec2 GetCenter() => Position + Size / 2f;
}

/// <summary> Math helpers for the model. Engine-independent replacement for Godot's Mathf. </summary>
public static class MathM
{
    /// <summary> Largest integer not greater than <paramref name="value"/>. </summary>
    public static int FloorToInt(float value) => (int)MathF.Floor(value);

    /// <summary> Modulo that is always non-negative for a positive divisor. </summary>
    public static int PosMod(int a, int b)
    {
        var m = a % b;
        return m < 0 ? m + b : m;
    }

    /// <summary> Modulo that is always non-negative for a positive divisor. </summary>
    public static float PosMod(float a, float b)
    {
        var m = a % b;
        return m < 0 ? m + b : m;
    }

    /// <summary> Linear interpolation between <paramref name="from"/> and <paramref name="to"/>. </summary>
    public static float Lerp(float from, float to, float weight) => from + (to - from) * weight;

    /// <summary> Cubic Hermite interpolation of <paramref name="t"/> in [0, 1]. </summary>
    public static float SmoothStep(float t) => t * t * (3 - 2 * t);
}
