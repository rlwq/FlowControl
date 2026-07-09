using System.Collections.Generic;
using System.Linq;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel;
using FlowControlModel.Entities;
using FlowControlModel.Machines;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// Orchestrates the visual representation of the game world by managing <see cref="World.ChunkView"/> instances.
/// </summary>
/// <remarks>
/// This class synchronizes the logical state of the <see cref="ChunkManager"/> with the Godot scene tree.
/// It handles the dynamic loading/unloading of chunk visuals based on the camera or player position.
/// Machine views are owned directly by this node (not by chunk views): a machine may span
/// several chunks, and its single view lives for as long as any of them is visible.
/// </remarks>
[GlobalClass]
public partial class ChunkManagerView : Node
{
    [Export]
    private PackedScene _chunkScene = null!;

    private IChunkManager _chunkManager = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private readonly Dictionary<Vector2I, World.ChunkView> _chunkViews = [];
    private readonly Dictionary<uint, World.MachineView> _machineViews = [];

    /// <summary> Initializes the view with logical managers and subscribes to world changes. </summary>
    public void Setup(IChunkManager chunkManager, ResourceRegistry resourceRegistry)
    {
        _chunkManager = chunkManager;
        _resourceRegistry = resourceRegistry;

        chunkManager.MachinePlaced += OnMachinePlaced;
        chunkManager.MachineRemoved += OnMachineRemoved;

        chunkManager.EntityPlaced += OnEntityAdded;
        chunkManager.EntityRemoved += OnEntityRemoved;
        chunkManager.EntityMoved += OnEntityMoved;

        chunkManager.GroundTileSet += OnGroundTileSet;

        chunkManager.GroundItemPlaced += OnGroundItemPlaced;
        chunkManager.GroundItemRemoved += OnGroundItemRemoved;
    }

    /// <summary> Instantiates and prepares a visual chunk at the specified coordinates. </summary>
    public void ShowChunk(Vector2I chunkCoord)
    {
        if (_chunkViews.ContainsKey(chunkCoord))
            return;

        var chunkView = _chunkScene.Instantiate<World.ChunkView>();
        AddChild(chunkView);

        chunkView.Position =
            chunkCoord * _resourceRegistry.CellSize * _chunkManager.Grid.ChunkSize;
        chunkView.Setup(_resourceRegistry);
        _chunkViews.Add(chunkCoord, chunkView);

        // Initialize ground
        var chunkOrigin = chunkCoord.ToModel() * _chunkManager.Grid.ChunkSize;
        for (var i = 0; i < _chunkManager.Grid.ChunkSize; i++)
            for (var j = 0; j < _chunkManager.Grid.ChunkSize; j++)
            {
                var groundLite = _chunkManager.GetGroundLiteAt(chunkOrigin + new Vec2I(j, i));
                chunkView.SetGroundAt(new Vector2I(j, i), groundLite);
            }

        // Initialize existing machines (unless another visible chunk already shows them)
        foreach (var machine in _chunkManager.GetMachinesInChunk(chunkCoord.ToModel()))
            ShowMachineView(machine);

        // Initialize existing entities
        foreach (var entity in _chunkManager.GetEntitiesInChunk(chunkCoord.ToModel()))
            chunkView.BuildEntityView(entity);

        // Initialize existing ground items
        foreach (var item in _chunkManager.GetGroundItemsInChunk(chunkCoord.ToModel()))
            chunkView.BuildItemView(item);
    }

    /// <summary> Removes the visual representation of a chunk from the scene tree. </summary>
    public void HideChunk(Vector2I chunkCoord)
    {
        if (!_chunkViews.TryGetValue(chunkCoord, out World.ChunkView? chunkView))
            return;

        RemoveChild(chunkView);
        chunkView.QueueFree();
        _chunkViews.Remove(chunkCoord);

        // Machine views survive as long as any chunk they overlap is still visible
        foreach (var machine in _chunkManager.GetMachinesInChunk(chunkCoord.ToModel()))
            if (!OverlappedChunks(machine).Any(IsLoaded))
                HideMachineView(machine);
    }

    /// <summary>
    /// Updates the visible area, loading new chunks in the rectangle and unloading those outside.
    /// </summary>
    /// <param name="rect">Target rectangle in chunk coordinates.</param>
    public void MaintainRect(Rect2I rect)
    {
        foreach (var chunkView in _chunkViews.Keys.ToList())
            if (!rect.HasPoint(chunkView))
                HideChunk(chunkView);

        for (var i = rect.Position.Y; i < rect.End.Y; i++)
            for (var j = rect.Position.X; j < rect.End.X; j++)
                ShowChunk(new Vector2I(j, i));
    }

    private bool IsLoaded(Vector2I chunkCoord) => _chunkViews.ContainsKey(chunkCoord);

    /// <summary> Enumerates the coordinates of all chunks the machine overlaps. </summary>
    private IEnumerable<Vector2I> OverlappedChunks(IMachine machine)
    {
        var begin = _chunkManager.Grid.ToLocalI(machine.Coord).Chunk;
        var end = _chunkManager.Grid.ToLocalI(machine.End - Vec2I.One).Chunk;
        for (var i = begin.Y; i <= end.Y; i++)
        for (var j = begin.X; j <= end.X; j++)
            yield return new Vector2I(j, i);
    }

    /// <summary> Builds the machine's view (a direct child of this node), if it does not exist yet. </summary>
    private void ShowMachineView(IMachine machine)
    {
        if (_machineViews.ContainsKey(machine.Id))
            return;

        var machineView = _resourceRegistry.BuildMachineView(machine);
        AddChild(machineView);
        _machineViews[machine.Id] = machineView;
    }

    /// <summary> Destroys the machine's view, if it exists. </summary>
    private void HideMachineView(IMachine machine)
    {
        if (!_machineViews.TryGetValue(machine.Id, out var view))
            return;

        RemoveChild(view);
        view.QueueFree();
        _machineViews.Remove(machine.Id);
    }

    private void OnGroundTileSet(Vec2I coord, GroundLite lite)
    {
        var localPos = _chunkManager.Grid.ToLocalI(coord);
        var chunk = localPos.Chunk.ToGodot();
        if (!IsLoaded(chunk))
            return;
        _chunkViews[chunk].SetGroundAt(localPos.Cell.ToGodot(), lite);
    }

    private void OnMachinePlaced(IMachine machineInst)
    {
        if (OverlappedChunks(machineInst).Any(IsLoaded))
            ShowMachineView(machineInst);
    }

    private void OnMachineRemoved(IMachine machineInst)
    {
        HideMachineView(machineInst);
    }

    private void OnEntityAdded(IEntity entity)
    {
        var chunk = _chunkManager.Grid.ToLocal(entity.Coord).Chunk.ToGodot();
        if (!IsLoaded(chunk))
            return;
        _chunkViews[chunk].BuildEntityView(entity);
    }

    private void OnEntityRemoved(IEntity entity)
    {
        var chunk = _chunkManager.Grid.ToLocal(entity.Coord).Chunk.ToGodot();
        if (!IsLoaded(chunk))
            return;
        _chunkViews[chunk].RemoveEntityView(entity);
    }

    private void OnEntityMoved(IEntity entity, Vec2 from)
    {
        var chunkFrom = _chunkManager.Grid.ToLocal(from).Chunk.ToGodot();
        var chunkTo = _chunkManager.Grid.ToLocal(entity.Coord).Chunk.ToGodot();

        if (!IsLoaded(chunkFrom) && !IsLoaded(chunkTo))
            return;

        if (IsLoaded(chunkFrom) && !IsLoaded(chunkTo))
            _chunkViews[chunkFrom].RemoveEntityView(entity);

        if (!IsLoaded(chunkFrom) && IsLoaded(chunkTo))
            _chunkViews[chunkTo].BuildEntityView(entity);

        if (!IsLoaded(chunkFrom) || !IsLoaded(chunkTo)) return;

        var entityView = _chunkViews[chunkFrom].ExtractEntityView(entity);
        entityView.Position = _resourceRegistry.GetEntityViewPosition(entity);
        _chunkViews[chunkTo].InsertEntityView(entity, entityView);
    }

    private void OnGroundItemPlaced(IGroundItem item)
    {
        var chunk = _chunkManager.Grid.ToLocal(item.Coord).Chunk.ToGodot();
        if (!IsLoaded(chunk))
            return;
        _chunkViews[chunk].BuildItemView(item);
    }

    private void OnGroundItemRemoved(IGroundItem item)
    {
        var chunk = _chunkManager.Grid.ToLocal(item.Coord).Chunk.ToGodot();
        if (!IsLoaded(chunk))
            return;
        _chunkViews[chunk].RemoveItemView(item);
    }
}
