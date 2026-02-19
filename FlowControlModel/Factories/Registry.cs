using System.Collections.Generic;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Inventories;
using Godot;

namespace FlowControlModel.Factories;

/// <summary>
/// Registry of all machine, ground and entity types.
/// It is used to build new <see cref="Machine"/> objects which will be placed in the world later.
/// </summary>
public partial class Registry
{
    private readonly Dictionary<StringName, MachineLite> _machineLites = [];
    private readonly Dictionary<StringName, MachineLogic> _machineLogics = [];
    private readonly Dictionary<StringName, Vector2I[]> _machineLogicObserverOffsets = [];
    
    private readonly Dictionary<StringName, GroundLite> _groundLites = [];
    private readonly Dictionary<StringName, EntityLite> _entityLites = [];

    private readonly Dictionary<StringName, ItemLite> _itemLites = [];

    /// <summary> Gets the number of cells along one side of a chunk. </summary>
    public int ChunkSize { get; private set; }

    /// <summary>
    /// Returns a rectangle representing the boundaries of local integer coordinates within a chunk.
    /// A rectangle which starts at <c>(0, 0)</c> with dimensions equal to <see cref="ChunkSize"/>.
    /// </summary>
    public Rect2I ChunkRectI => new(0, 0, ChunkSize, ChunkSize);

    /// <summary>
    /// Returns a rectangle representing the boundaries of local floating-point coordinates within a chunk.
    /// A rectangle which starts at <c>(0, 0)</c> with dimensions equal to <see cref="ChunkSize"/>.
    /// </summary>
    public Rect2 ChunkRect => new(0, 0, ChunkSize, ChunkSize);

    /// <summary> Transforms global world coordinates into <see cref="LocalCoordsI"/>. </summary>
    /// <returns> A <see cref="LocalCoordsI"/> struct containing chunk and relative cell positions. </returns>
    public LocalCoordsI ToLocalI(Vector2I globalCoord) =>
        new(
            new Vector2I(
                globalCoord.X >= 0
                    ? globalCoord.X / ChunkSize
                    : (globalCoord.X + 1) / ChunkSize - 1,
                globalCoord.Y >= 0
                    ? globalCoord.Y / ChunkSize
                    : (globalCoord.Y + 1) / ChunkSize - 1
            ),
            new Vector2I(
                Mathf.PosMod(globalCoord.X, ChunkSize),
                Mathf.PosMod(globalCoord.Y, ChunkSize)
            )
        );

    /// <summary> Transforms global world coordinates into <see cref="LocalCoords"/>. </summary>
    /// <returns> A <see cref="LocalCoords"/> struct containing chunk and relative cell positions. </returns>
    public LocalCoords ToLocal(Vector2 globalCoord) =>
        new(
            (Vector2I)(globalCoord / ChunkSize).Floor(),
            new Vector2(
                Mathf.PosMod(globalCoord.X, ChunkSize),
                Mathf.PosMod(globalCoord.Y, ChunkSize)
            )
        );

    /// <summary> Retrieves a <see cref="GroundLite"/> by its <paramref name="kind"/> name. </summary>
    public GroundLite GetGroundLite(StringName kind) => _groundLites[kind];

    /// <summary> Retrieves a <see cref="MachineLite"/> by its <paramref name="kind"/> name. </summary>
    public MachineLite GetMachineLite(StringName kind) => _machineLites[kind];

    /// <summary>
    /// Retrieves a <see cref="MachineLogic"/> prototype. It must be copied and not used directly.
    /// </summary>
    public MachineLogic GetMachineLogic(StringName kind) => _machineLogics[kind];
    
    /// <summary> <c>True</c> if the machine logic can interact with other machines. </summary>
    public bool IsMachineInteractive(StringName kind) => _machineLogicObserverOffsets.ContainsKey(kind);
    
    /// <summary>
    /// If the machine is interactive, it retrieves offsets to the cells it interacts with.
    /// </summary>
    public IEnumerable<Vector2I> GetMachineObserverOffsets(StringName kind) => _machineLogicObserverOffsets[kind];
        
    /// <summary> Retrieves a <see cref="EntityLite"/> by its <paramref name="kind"/> name. </summary>
    public EntityLite GetEntityLite(StringName kind) => _entityLites[kind];
    
    /// <summary> Retrieves specified item kind's <see cref="ItemLite"/>. </summary>
    public ItemLite GetItemLite(StringName kind) => _itemLites[kind];
}

/// <summary>
/// Represents an integer coordinate pair: the chunk's address and the cell's local position within that chunk.
/// </summary>
/// <param name="chunk"> Global coordinates of the chunk. </param>
/// <param name="cell"> Local coordinates of the cell (0 to <see cref="Registry.ChunkSize"/> - 1). </param>
public struct LocalCoordsI(Vector2I chunk, Vector2I cell)
{
    /// <summary> Global coordinates of the chunk. </summary>
    public Vector2I Chunk = chunk;

    /// <summary> Local coordinates of the cell inside the chunk. </summary>
    public Vector2I Cell = cell;
}

/// <summary>
/// Represents a float coordinate pair: the chunk's address and the cell's local position within that chunk.
/// </summary>
/// <param name="chunk">Global coordinates of the chunk.</param>
/// <param name="cell">Local coordinates of the cell (0 to <see cref="Registry.ChunkSize"/> - 1).</param>
public struct LocalCoords(Vector2I chunk, Vector2 cell)
{
    /// <summary>Global coordinates of the chunk.</summary>
    public Vector2I Chunk = chunk;

    /// <summary>Local coordinates of the cell inside the chunk.</summary>
    public Vector2 Cell = cell;
}
