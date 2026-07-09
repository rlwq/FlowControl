
namespace FlowControlModel.World;

/// <summary>
/// Immutable geometry of the world's chunk grid.
/// Knows how big a chunk is and how to convert global coordinates into chunk-local ones.
/// </summary>
public sealed class WorldGrid(int chunkSize)
{
    /// <summary> Amount of cells along one side of a chunk. </summary>
    public int ChunkSize { get; } = chunkSize;

    /// <summary>
    /// Boundaries of local integer coordinates within a chunk.
    /// A rectangle which starts at <c>(0, 0)</c> with dimensions equal to <see cref="ChunkSize"/>.
    /// </summary>
    public RectI LocalRectI => new(0, 0, ChunkSize, ChunkSize);

    /// <summary> Transforms global world coordinates into <see cref="LocalCoordsI"/>. </summary>
    /// <returns> A <see cref="LocalCoordsI"/> struct containing chunk and relative cell positions. </returns>
    public LocalCoordsI ToLocalI(Vec2I globalCoord) =>
        new(
            new Vec2I(
                globalCoord.X >= 0
                    ? globalCoord.X / ChunkSize
                    : (globalCoord.X + 1) / ChunkSize - 1,
                globalCoord.Y >= 0
                    ? globalCoord.Y / ChunkSize
                    : (globalCoord.Y + 1) / ChunkSize - 1
            ),
            new Vec2I(
                MathM.PosMod(globalCoord.X, ChunkSize),
                MathM.PosMod(globalCoord.Y, ChunkSize)
            )
        );

    /// <summary> Transforms global world coordinates into <see cref="LocalCoords"/>. </summary>
    /// <returns> A <see cref="LocalCoords"/> struct containing chunk and relative cell positions. </returns>
    public LocalCoords ToLocal(Vec2 globalCoord) =>
        new(
            (globalCoord / ChunkSize).FloorToI(),
            new Vec2(
                MathM.PosMod(globalCoord.X, ChunkSize),
                MathM.PosMod(globalCoord.Y, ChunkSize)
            )
        );
}

/// <summary>
/// Represents an integer coordinate pair: the chunk's address and the cell's local position within that chunk.
/// </summary>
/// <param name="chunk"> Global coordinates of the chunk. </param>
/// <param name="cell"> Local coordinates of the cell (0 to <see cref="WorldGrid.ChunkSize"/> - 1). </param>
public struct LocalCoordsI(Vec2I chunk, Vec2I cell)
{
    /// <summary> Global coordinates of the chunk. </summary>
    public Vec2I Chunk = chunk;

    /// <summary> Local coordinates of the cell inside the chunk. </summary>
    public Vec2I Cell = cell;
}

/// <summary>
/// Represents a float coordinate pair: the chunk's address and the cell's local position within that chunk.
/// </summary>
/// <param name="chunk">Global coordinates of the chunk.</param>
/// <param name="cell">Local coordinates of the cell (0 to <see cref="WorldGrid.ChunkSize"/> - 1).</param>
public struct LocalCoords(Vec2I chunk, Vec2 cell)
{
    /// <summary>Global coordinates of the chunk.</summary>
    public Vec2I Chunk = chunk;

    /// <summary>Local coordinates of the cell inside the chunk.</summary>
    public Vec2 Cell = cell;
}
