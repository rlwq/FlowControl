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
/// Manages a <see cref="TileMapLayer"/> for terrain rendering plus the ground item
/// visuals lying inside the chunk. All coordinates passed to this class are expected to be
/// local to the chunk (0 to ChunkSize-1). Machine and entity visuals are owned by
/// <see cref="ChunkManagerView"/>: machines may span multiple chunks, and entity sprites
/// are interpolated in global coordinates.
/// </remarks>
[GlobalClass]
public partial class ChunkView : Node2D
{
    [Export]
    private TileMapLayer _groundTiles = null!;

    private ResourceRegistry _resourceRegistry = null!;
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
