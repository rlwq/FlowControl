using System.Collections.Generic;
using System.Diagnostics;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Entities;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.World;

/// <summary>
/// A visual representation of a single world chunk.
/// </summary>
/// <remarks>
/// Manages a <see cref="TileMapLayer"/> for terrain rendering plus the entity and ground item
/// visuals living inside the chunk. All coordinates passed to this class are expected to be
/// local to the chunk (0 to ChunkSize-1). Machine visuals are owned by
/// <see cref="ChunkManagerView"/>, as machines may span multiple chunks.
/// </remarks>
[GlobalClass]
public partial class ChunkView : Node2D
{
    [Export]
    private TileMapLayer _groundTiles = null!;

    private ResourceRegistry _resourceRegistry = null!;
    private readonly Dictionary<uint, EntityView> _entityViews = [];
    private readonly Dictionary<uint, ItemView> _itemViews = [];

    /// <summary> Initializes the chunk view with the visual asset registry. </summary>
    public void Setup(ResourceRegistry resourceRegistry)
    {
        Debug.Assert(resourceRegistry != null);
        _resourceRegistry = resourceRegistry;
    }

    /// <summary> Updates a specific terrain tile visual within the chunk. </summary>
    public void SetGroundAt(Vector2I localPos, GroundLite groundLite)
    {
        _groundTiles.SetCell(localPos, 0, _resourceRegistry.GetGroundTileLite(groundLite.Kind));
    }

    /// <summary> Creates and attaches a visual representation for an entity. </summary>
    public void BuildEntityView(IEntity entity)
    {
        Debug.Assert(!_entityViews.ContainsKey(entity.Id));

        var entityView = _resourceRegistry.BuildEntityView(entity);
        AddChild(entityView);
        _entityViews[entity.Id] = entityView;
    }

    /// <summary> Destroys and removes the visual representation of an entity. </summary>
    public void RemoveEntityView(IEntity entity)
    {
        if (!_entityViews.TryGetValue(entity.Id, out var view)) return;
        RemoveChild(view);
        view.QueueFree();
        _entityViews.Remove(entity.Id);
    }

    /// <summary> Inserts an already existing visual representation of the specified entity. </summary>
    public void InsertEntityView(IEntity entity, EntityView entityView)
    {
        AddChild(entityView);
        _entityViews[entity.Id] = entityView;
    }

    /// <summary> Extracts and returns visual representation of the specified entity. </summary>
    public EntityView ExtractEntityView(IEntity entity)
    {
        Debug.Assert(_entityViews.ContainsKey(entity.Id));
        var view = _entityViews[entity.Id];
        RemoveChild(view);
        _entityViews.Remove(entity.Id);
        return view;
    }

    /// <summary> Creates and attaches a visual representation for a ground item. </summary>
    public void BuildItemView(IGroundItem item)
    {
        Debug.Assert(!_itemViews.ContainsKey(item.Id));

        var itemView = _resourceRegistry.BuildItemView(item);
        AddChild(itemView);
        _itemViews[item.Id] = itemView;
    }

    /// <summary> Destroys and removes the visual representation of a ground item. </summary>
    public void RemoveItemView(IGroundItem item)
    {
        if (!_itemViews.TryGetValue(item.Id, out var view)) return;
        RemoveChild(view);
        view.QueueFree();
        _itemViews.Remove(item.Id);
    }
}
