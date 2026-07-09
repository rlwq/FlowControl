using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel;
using FlowControlModel.Factories;
using Godot;

namespace FlowControlGodotClient.Gui;

/// <summary>
/// A translucent world-space preview of the building held in the hand:
/// snaps to the hovered tile and turns green or red depending on whether
/// the placement would succeed (region free + within the player's reach).
/// </summary>
[GlobalClass]
public partial class PlacementGhost : Sprite2D
{
    private static readonly Color ValidTint = new(0.5f, 1f, 0.5f, 0.55f);
    private static readonly Color InvalidTint = new(1f, 0.4f, 0.4f, 0.55f);

    private WorldSim _world = null!;
    private Registry _registry = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private uint _playerId;

    /// <summary> Initializes the ghost (hidden until a building is held). </summary>
    public void Setup(WorldSim world, Registry registry, ResourceRegistry resourceRegistry, uint playerId)
    {
        _world = world;
        _registry = registry;
        _resourceRegistry = resourceRegistry;
        _playerId = playerId;

        ZIndex = 3;
        Visible = false;
    }

    /// <summary>
    /// Shows the ghost of the specified kind over a tile
    /// and tints it by placement validity. Pass <c>null</c> to hide.
    /// </summary>
    public void Preview(string? kind, Vec2I tile, Rotation rotation)
    {
        if (kind == null)
        {
            Visible = false;
            return;
        }

        var lite = _registry.GetMachineLite(kind);
        _resourceRegistry.ApplyMachineSprite(this, kind, lite.Dimensions, tile, rotation);
        Modulate = IsPlacementValid(lite, tile, rotation) ? ValidTint : InvalidTint;
        Visible = true;
    }

    /// <summary> Mirrors the checks <c>WorldSim.PlaceMachine</c> performs for an actor. </summary>
    private bool IsPlacementValid(FlowControlModel.Machines.MachineLite lite, Vec2I tile, Rotation rotation)
    {
        var rect = new RectI(tile, RotationM.RotateDims(lite.Dimensions, rotation));
        var player = _world.ChunkManager.GetEntityById(_playerId);

        return rect.GetCenter().DistanceTo(player.Coord) <= WorldSim.PlayerReach
               && _world.ChunkManager.IsBoxFree(rect);
    }
}
