using System.Collections.Generic;
using FlowControlModel.Entities;
using FlowControlModel.Factories;
using FlowControlModel.Machines;
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
    private readonly Registry _registry;

    private int _cellSize;
    private readonly Dictionary<StringName, Vector2I> _groundTilesAtlasCords = [];
    private readonly Dictionary<StringName, Texture2D> _machineTextures = [];
    private readonly Dictionary<StringName, Texture2D> _enityTextures = [];

    /// <summary> Amount of pixels along one side of a cell. </summary>
    public int CellSize => _cellSize;

    private ResourceRegistry(Registry registry)
    {
        _registry = registry;
    }

    /// <summary> Returns the atlas coordinates for a specific ground type. </summary>
    public Vector2I GetGroundTileLite(StringName kind) => _groundTilesAtlasCords[kind];

    /// <summary> Constructs and configures a <see cref="World.MachineView"/> in a way to achieve the 2.5D effect. </summary>
    public World.MachineView BuildMachineView(IMachine machineInst)
    {
        var texture = _machineTextures[machineInst.Lite.Kind];
        var machineView = new World.MachineView
        {
            Position =
                (_registry.ToLocal(machineInst.Coord).Cell + new Vector2I(0, machineInst.Lite.Dimensions.Y))
                * _cellSize,
            Offset = new Vector2(0, -texture.GetHeight()),

            Texture = texture,
            Centered = false,
            YSortEnabled = true,
            ZIndex = 2,
        };

        return machineView;
    }

    /// <summary> Constructs and configures a <see cref="EntityView"/>. </summary>
    public EntityView BuildEntityView(IEntity entity)
    {
        var entityView = new EntityView
        {
            Position = _registry.ToLocal(entity.Coord).Cell * _cellSize,
            Texture = _enityTextures[entity.Lite.Kind],
            ZIndex = 1,
            ZAsRelative = true,
        };
        return entityView;
    }
}
