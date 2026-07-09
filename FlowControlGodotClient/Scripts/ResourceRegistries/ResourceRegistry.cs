using System.Collections.Generic;
using FlowControlModel;
using FlowControlModel.Entities;
using FlowControlModel.Machines;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.ResourceRegistries;

/// <summary>
/// A centralized repository for visual assets, mapping logical identifiers to Godot resources.
/// </summary>
/// <remarks>
/// This registry acts as a factory for visual components.
/// and provides lookup tables for tilemap atlas coordinates and textures.
/// </remarks>
public partial class ResourceRegistry
{
    /// <summary> Size of a ground item sprite as a fraction of a cell. </summary>
    private const float ItemViewCellFraction = 0.5f;

    private readonly WorldGrid _grid;

    private int _cellSize;
    private readonly Dictionary<string, Vector2I> _groundTilesAtlasCords = [];
    private readonly Dictionary<string, Texture2D> _machineTextures = [];
    private readonly Dictionary<string, Texture2D> _entityTextures = [];
    private readonly Dictionary<string, Texture2D> _itemTextures = [];

    /// <summary> Amount of pixels along one side of a cell. </summary>
    public int CellSize => _cellSize;

    private ResourceRegistry(WorldGrid grid)
    {
        _grid = grid;
    }

    /// <summary> Returns the atlas coordinates for a specific ground type. </summary>
    public Vector2I GetGroundTileLite(string kind) => _groundTilesAtlasCords[kind];

    /// <summary>
    /// Constructs and configures a <see cref="World.MachineView"/> in a way to achieve the 2.5D effect.
    /// The view is positioned in global pixel coordinates.
    /// </summary>
    /// <remarks>
    /// The sprite is uniformly scaled so its width matches the machine's unrotated footprint;
    /// any extra height rises above the footprint (2.5D). The whole sprite is then rotated
    /// around the footprint's center along with the machine, so the footprint always
    /// covers the machine's actual (rotated) cells.
    /// </remarks>
    public World.MachineView BuildMachineView(IMachine machineInst)
    {
        var texture = _machineTextures[machineInst.Lite.Kind];
        float texW = texture.GetWidth(), texH = texture.GetHeight();
        var dims = machineInst.Lite.Dimensions;
        var scale = dims.X * _cellSize / texW;
        // Texture rows covering the footprint itself (the rest is the 2.5D overhang)
        var footprintTexH = texW * dims.Y / dims.X;

        var machineView = new World.MachineView
        {
            Position = machineInst.Rect.GetCenter().ToGodot() * _cellSize,
            // Anchor the sprite by the center of its footprint block, so rotation
            // spins the footprint in place
            Offset = new Vector2(-texW / 2, footprintTexH / 2 - texH),
            Rotation = (int)machineInst.Rotation * Mathf.Pi / 2,
            Scale = new Vector2(scale, scale),

            Texture = texture,
            Centered = false,
            YSortEnabled = true,
            ZIndex = 2,
        };

        return machineView;
    }

    /// <summary>
    /// Computes the in-chunk position of an entity's view: the bottom-center ("feet")
    /// of its collision box.
    /// </summary>
    public Vector2 GetEntityViewPosition(IEntity entity) =>
        (_grid.ToLocal(entity.Coord).Cell + new Vec2(0, entity.Lite.BoxSize.Y / 2)).ToGodot() * _cellSize;

    /// <summary> Constructs and configures a <see cref="World.EntityView"/>. </summary>
    /// <remarks>
    /// The sprite is uniformly scaled so its width matches the entity's collision box
    /// and anchored by its feet: the texture's bottom-center is placed at the bottom of
    /// the collision box, so taller sprites rise above it (2.5D effect).
    /// </remarks>
    public World.EntityView BuildEntityView(IEntity entity)
    {
        var texture = _entityTextures[entity.Lite.Kind];
        var scale = entity.Lite.BoxSize.X * _cellSize / texture.GetWidth();
        var entityView = new World.EntityView
        {
            Position = GetEntityViewPosition(entity),
            Offset = new Vector2(-texture.GetWidth() / 2f, -texture.GetHeight()),
            Centered = false,
            Texture = texture,
            Scale = new Vector2(scale, scale),
            ZIndex = 1,
            ZAsRelative = true,
        };
        return entityView;
    }

    /// <summary> Constructs and configures a <see cref="World.ItemView"/> for a ground item. </summary>
    /// <remarks> The sprite is centered on the stack's position and scaled to half a cell. </remarks>
    public World.ItemView BuildItemView(IGroundItem item)
    {
        var texture = _itemTextures[item.Stack.Lite.Kind];
        var scale = ItemViewCellFraction * _cellSize / texture.GetWidth();
        var itemView = new World.ItemView
        {
            Position = _grid.ToLocal(item.Coord).Cell.ToGodot() * _cellSize,
            Texture = texture,
            Scale = new Vector2(scale, scale),
            ZIndex = 0,
        };
        return itemView;
    }
}
