using System.Collections.Generic;
using System.Linq;
using FlowControlGodotClient.ResourceRegistries;
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
/// </remarks>
[GlobalClass]
public partial class ChunkManagerView : Node
{
    [Export]
    private PackedScene _chunkScene = null!;

    private IChunkManager _chunkManager = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private readonly Dictionary<Vector2I, World.ChunkView> _chunkViews = [];

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
    }

    /// <summary> Instantiates and prepares a visual chunk at the specified coordinates. </summary>
    public void ShowChunk(Vector2I chunkCoord)
    {
        if (_chunkViews.ContainsKey(chunkCoord))
            return;

        var chunkView = _chunkScene.Instantiate<World.ChunkView>();
        AddChild(chunkView);

        chunkView.Position =
            chunkCoord * _resourceRegistry.CellSize * _chunkManager.Registry.ChunkSize;
        chunkView.Setup(_chunkManager.Registry, _resourceRegistry);
        _chunkViews.Add(chunkCoord, chunkView);

        // Initialize ground
        for (var i = 0; i < _chunkManager.Registry.ChunkSize; i++)
            for (var j = 0; j < _chunkManager.Registry.ChunkSize; j++)
            {
                var groundLite = _chunkManager.GetGroundLiteAt(new Vector2I(j, i));
                chunkView.SetGroundAt(new Vector2I(j, i), groundLite);
            }

        // Initialize existing machines
        foreach (var machine in _chunkManager.GetMachinesInChunk(chunkCoord))
            chunkView.BuildMachineView(machine);

        // Initialize existing entities
        foreach (var entity in _chunkManager.GetEntitiesInChunk(chunkCoord))
            chunkView.BuildEntityView(entity);
    }

    /// <summary> Removes the visual representation of a chunk from the scene tree. </summary>
    public void HideChunk(Vector2I chunkCoord)
    {
        if (!_chunkViews.TryGetValue(chunkCoord, out World.ChunkView? chunkView))
            return;

        RemoveChild(chunkView);
        chunkView.QueueFree();
        _chunkViews.Remove(chunkCoord);
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

    private void OnMachinePlaced(IMachine machineInst)
    {
        var localPos = _chunkManager.Registry.ToLocal(machineInst.Coord);
        if (!IsLoaded(localPos.Chunk))
            return;
        _chunkViews[localPos.Chunk].BuildMachineView(machineInst);
    }

    private void OnMachineRemoved(IMachine machineInst)
    {
        var localPos = _chunkManager.Registry.ToLocal(machineInst.Coord);
        if (!IsLoaded(localPos.Chunk))
            return;
        _chunkViews[localPos.Chunk].RemoveMachineView(machineInst);
    }

    private void OnEntityAdded(IEntity entity)
    {
        var localPos = _chunkManager.Registry.ToLocal(entity.Coord);
        if (!IsLoaded(localPos.Chunk))
            return;
        _chunkViews[localPos.Chunk].BuildEntityView(entity);
    }

    private void OnEntityRemoved(IEntity entity)
    {
        var localPos = _chunkManager.Registry.ToLocal(entity.Coord);
        if (!IsLoaded(localPos.Chunk))
            return;
        _chunkViews[localPos.Chunk].RemoveEntityView(entity);
    }

    private void OnEntityMoved(IEntity entity, Vector2 from)
    {
        var localPosFrom = _chunkManager.Registry.ToLocal(from);
        var localPosTo = _chunkManager.Registry.ToLocal(entity.Coord);

        if (!IsLoaded(localPosFrom.Chunk) && !IsLoaded(localPosTo.Chunk))
            return;

        if (IsLoaded(localPosFrom.Chunk) && !IsLoaded(localPosTo.Chunk))
            _chunkViews[localPosFrom.Chunk].RemoveEntityView(entity);

        if (!IsLoaded(localPosFrom.Chunk) && IsLoaded(localPosTo.Chunk))
            _chunkViews[localPosTo.Chunk].BuildEntityView(entity);

        if (!IsLoaded(localPosFrom.Chunk) || !IsLoaded(localPosTo.Chunk)) return;
        
        var entityView = _chunkViews[localPosFrom.Chunk].ExtractEntityView(entity);
        entityView.Position =
            _chunkManager.Registry.ToLocal(entity.Coord).Cell * _resourceRegistry.CellSize;
        _chunkViews[localPosTo.Chunk].InsertEntityView(entity, entityView);
    }
}
