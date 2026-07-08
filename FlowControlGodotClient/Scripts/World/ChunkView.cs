using System.Collections.Generic;
using System.Diagnostics;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Entities;
using FlowControlModel.Factories;
using FlowControlModel.Machines;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// A visual representation of a single world chunk.
/// </summary>
/// <remarks>
/// Manages a <see cref="TileMapLayer"/> for terrain rendering and a collection of
/// machine visuals. All coordinates passed to this class are expected to be
/// local to the chunk (0 to ChunkSize-1).
/// </remarks>
[GlobalClass]
public partial class ChunkView : Node2D
{
    [Export]
    private TileMapLayer _groundTiles = null!;

    private Registry _registry = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private readonly Dictionary<IMachine, World.MachineView> _machineViews = [];
    private readonly Dictionary<IEntity, EntityView> _entityViews = [];

    /// <summary> Initializes the chunk view with essential registries. </summary>
    public void Setup(Registry registry, ResourceRegistry resourceRegistry)
    {
        Debug.Assert(registry != null && resourceRegistry != null);
        _registry = registry;
        _resourceRegistry = resourceRegistry;
    }

    /// <summary> Updates a specific terrain tile visual within the chunk. </summary>
    public void SetGroundAt(Vector2I localPos, GroundLite groundLite)
    {
        _groundTiles.SetCell(localPos, 0, _resourceRegistry.GetGroundTileLite(groundLite.Kind));
    }

    /// <summary> Creates and attaches a visual representation for a machine. </summary>
    public void BuildMachineView(IMachine machineInst)
    {
        Debug.Assert(!_machineViews.ContainsKey(machineInst));

        var machineView = _resourceRegistry.BuildMachineView(machineInst);
        AddChild(machineView);
        _machineViews[machineInst] = machineView;
    }

    /// <summary> Destroys and removes the visual representation of a machine. </summary>
    public void RemoveMachineView(IMachine machineInst)
    {
        if (!_machineViews.TryGetValue(machineInst, out var view)) return;
        RemoveChild(view);
        view.QueueFree();
        _machineViews.Remove(machineInst);
    }

    /// <summary> Creates and attaches a visual representation for an entity. </summary>
    public void BuildEntityView(IEntity entity)
    {
        Debug.Assert(!_entityViews.ContainsKey(entity));

        var entityView = _resourceRegistry.BuildEntityView(entity);
        AddChild(entityView);
        _entityViews[entity] = entityView;
    }

    /// <summary> Destroys and removes the visual representation of an entity. </summary>
    public void RemoveEntityView(IEntity entity)
    {
        if (!_entityViews.TryGetValue(entity, out var view)) return;
        RemoveChild(view);
        view.QueueFree();
        _entityViews.Remove(entity);
    }

    /// <summary> Inserts an already existing visual representation of the speciefied entity. </summary>
    public void InsertEntityView(IEntity entity, EntityView entityView)
    {
        AddChild(entityView);
        _entityViews[entity] = entityView;
    }

    /// <summary> Extracts and returns visual representation of the specified entity. </summary>
    public EntityView ExtractEntityView(IEntity entity)
    {
        Debug.Assert(_entityViews.ContainsKey(entity));
        var view = _entityViews[entity];
        RemoveChild(view);
        _entityViews.Remove(entity);
        return view;
    }
}
