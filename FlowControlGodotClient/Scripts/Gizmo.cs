using System.Linq;
using System.Text;
using FlowControlModel;
using Godot;

namespace FlowControlGodotClient;

/// <summary>
/// Explicit on-screen debug tool. Toggled with the <c>toggle_gizmo</c> action (F3).
/// Shows FPS, the player's position, inventory and placement rotation, plus everything
/// known about the tile under the cursor: ground kind, machine and its inventory,
/// items lying on the ground.
/// </summary>
[GlobalClass]
public partial class Gizmo : CanvasLayer
{
    [Export]
    public Camera2D Camera = null!;

    [Export]
    public InputHandler InputHandler = null!;

    private WorldSim _world = null!;
    private uint _playerId;
    private int _cellSize;
    private Label _label = null!;

    /// <summary> Initializes the gizmo with the simulation to inspect. </summary>
    public void Setup(WorldSim world, uint playerId, int cellSize)
    {
        _world = world;
        _playerId = playerId;
        _cellSize = cellSize;
    }

    public override void _Ready()
    {
        _label = new Label { Position = new Vector2(8, 8) };
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        _label.AddThemeConstantOverride("shadow_offset_x", 1);
        _label.AddThemeConstantOverride("shadow_offset_y", 1);
        AddChild(_label);

        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionPressed("toggle_gizmo"))
            Visible = !Visible;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!Visible || _world == null)
            return;
        _label.Text = BuildText();
    }

    private string BuildText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"FPS: {Engine.GetFramesPerSecond()}");

        var player = _world.ChunkManager.GetEntityById(_playerId);
        sb.AppendLine($"Player: {player.Coord.X:0.00}, {player.Coord.Y:0.00}");

        var bag = string.Join(", ", player.Inventory.EnumerateStacks()
            .Select(s => $"{s.Lite.Kind} x{s.Count}"));
        sb.AppendLine($"Bag: {(bag.Length > 0 ? bag : "empty")}");
        sb.AppendLine($"Place rotation: {InputHandler.PlaceRotation}"
            + (InputHandler.IsFreeCamera ? " | free camera" : ""));

        var tile = MouseTileCoord();
        sb.AppendLine($"Tile: {tile.X}, {tile.Y} ({_world.ChunkManager.GetGroundLiteAt(tile).Kind})");

        if (_world.ChunkManager.GetMachineAt(tile) is { } machine)
        {
            sb.AppendLine($"Machine: {machine.Lite.Kind} #{machine.Id} ({machine.Rotation})");
            foreach (var stack in machine.Inventory.EnumerateStacks())
                sb.AppendLine($"  {stack.Lite.Kind} x{stack.Count}");
        }

        var chunkCoord = _world.ChunkManager.Grid.ToLocalI(tile).Chunk;
        foreach (var item in _world.ChunkManager.GetGroundItemsInChunk(chunkCoord))
            if (item.Coord.FloorToI() == tile)
                sb.AppendLine($"On ground: {item.Stack.Lite.Kind} x{item.Stack.Count}");

        return sb.ToString();
    }

    /// <summary> The world tile currently under the mouse cursor. </summary>
    private Vec2I MouseTileCoord()
    {
        var coord = Camera.GetGlobalMousePosition();
        var tile = (coord / _cellSize).Floor();
        return new Vec2I((int)tile.X, (int)tile.Y);
    }
}
