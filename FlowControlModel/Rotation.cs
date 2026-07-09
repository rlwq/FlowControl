
namespace FlowControlModel;

/// <summary> A cardinal orientation of a machine, in clockwise quarter turns. </summary>
public enum Rotation
{
    /// <summary> The default, unrotated orientation. </summary>
    North = 0,
    /// <summary> One quarter turn clockwise. </summary>
    East = 1,
    /// <summary> Two quarter turns (upside down). </summary>
    South = 2,
    /// <summary> Three quarter turns clockwise. </summary>
    West = 3,
}

/// <summary> Geometry helpers for <see cref="Rotation"/>. </summary>
public static class RotationM
{
    /// <summary> The next rotation clockwise. </summary>
    public static Rotation RotatedCw(this Rotation rotation) => (Rotation)(((int)rotation + 1) % 4);

    /// <summary> Footprint dimensions of a machine after rotation (width and height swap on quarter turns). </summary>
    public static Vec2I RotateDims(Vec2I dims, Rotation rotation) =>
        (int)rotation % 2 == 0 ? dims : new Vec2I(dims.Y, dims.X);

    /// <summary>
    /// Rotates a cell offset defined relative to an unrotated footprint of the specified dimensions.
    /// The footprint's top-left corner remains the anchor: rotating all footprint cells
    /// yields exactly the cells of the rotated footprint.
    /// </summary>
    public static Vec2I RotateOffset(Vec2I offset, Vec2I dims, Rotation rotation)
    {
        for (var k = 0; k < (int)rotation; k++)
        {
            offset = new Vec2I(dims.Y - 1 - offset.Y, offset.X);
            dims = new Vec2I(dims.Y, dims.X);
        }
        return offset;
    }
}
