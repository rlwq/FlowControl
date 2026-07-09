using FlowControlGodotClient.Gui;
using FlowControlModel;
using Godot;

namespace FlowControlGodotClient;

/// <summary>
/// Reads user input and translates it into <see cref="WorldSimCommand"/>s.
/// All keys are bound through <c>InputMap</c> actions (see <c>project.godot</c>), so they
/// are rebindable and gamepad-friendly: movement (<c>move_*</c>), building rotation
/// (<c>rotate_building</c>), item pick-up (<c>pick_up</c>).
/// Placement works Factorio-style: a building item is taken from the inventory into the
/// hand (<see cref="Hud.HandKind"/>), previewed by the <see cref="PlacementGhost"/> and
/// placed with the left mouse button; clicking a machine with an empty hand inspects it.
/// Also drives the camera: follow-the-player or free-cam (<c>free_camera</c>),
/// smooth zoom towards the mouse cursor (wheel).
/// </summary>
[GlobalClass]
public partial class InputHandler : Node
{
    /// <summary> Player movement speed, in cells per simulation tick. </summary>
    private const float PlayerSpeed = 0.25f;

    /// <summary> Free camera pan speed, in pixels per second at 1x zoom. </summary>
    private const float FreeCameraSpeed = 600f;

    /// <summary> Camera-to-player interpolation weight per frame. </summary>
    private const float CameraFollowWeight = 0.15f;

    /// <summary> Camera zoom limits and stepping. </summary>
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4f;
    private const float ZoomStep = 1.15f;

    /// <summary> Camera-to-target-zoom interpolation weight per frame. </summary>
    private const float ZoomLerpWeight = 0.2f;

    [Export]
    public Camera2D Camera = null!;

    private WorldSim _world = null!;
    private Hud _hud = null!;
    private PlacementGhost _ghost = null!;
    private uint _playerId;
    private int _cellSize;
    private float _targetZoom = 1f;
    private bool _freeCamera;

    /// <summary> The orientation applied to the next placed machine (cycled with <c>rotate_building</c>). </summary>
    public Rotation PlaceRotation { get; private set; } = Rotation.North;

    /// <summary> Whether the camera is detached from the player (toggled with <c>free_camera</c>). </summary>
    public bool IsFreeCamera => _freeCamera;

    /// <summary> Initializes the handler with the simulation and the player it controls. </summary>
    public void Setup(WorldSim world, uint playerId, int cellSize, Hud hud, PlacementGhost ghost)
    {
        _world = world;
        _playerId = playerId;
        _cellSize = cellSize;
        _hud = hud;
        _ghost = ghost;

        Camera.Position = PlayerPixelPosition();
    }

    /// <summary>
    /// Polls movement input and enqueues player commands.
    /// Must be called exactly once per simulation tick.
    /// </summary>
    public void ProcessTick()
    {
        if (_freeCamera)
            return; // in free-cam mode the movement input pans the camera instead

        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (direction != Vector2.Zero)
            _world.ReceiveCommand(new EntityStepById(_playerId, (direction * PlayerSpeed).ToModel()));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_world == null)
            return;

        if (_freeCamera)
        {
            var pan = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            Camera.Position += pan * FreeCameraSpeed * (float)delta / Camera.Zoom.X;
        }
        else
        {
            Camera.Position = Camera.Position.Lerp(PlayerPixelPosition(), CameraFollowWeight);
        }

        ProcessZoom();

        _ghost.Preview(_hud.HandBuildingKind, MouseTileCoord(), PlaceRotation);
    }

    /// <summary>
    /// Smoothly approaches the target zoom, keeping the world point under the mouse
    /// cursor fixed on screen (zoom towards the cursor).
    /// </summary>
    private void ProcessZoom()
    {
        var oldZoom = Camera.Zoom.X;
        var newZoom = Mathf.Lerp(oldZoom, _targetZoom, ZoomLerpWeight);
        if (Mathf.IsEqualApprox(newZoom, oldZoom))
            return;

        var viewport = GetViewport();
        var cursorFromCenter = viewport.GetMousePosition() - viewport.GetVisibleRect().Size / 2;

        Camera.Zoom = Vector2.One * newZoom;
        Camera.Position += cursorFromCenter * (1 / oldZoom - 1 / newZoom);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (_world == null)
            return;

        if (@event.IsActionPressed("rotate_building"))
            PlaceRotation = PlaceRotation.RotatedCw();
        if (@event.IsActionPressed("pick_up"))
            _world.ReceiveCommand(new PickUpItemAt(PlayerCoord(), _playerId));
        if (@event.IsActionPressed("free_camera"))
            _freeCamera = !_freeCamera;

        if (@event is not InputEventMouseButton mouse || !mouse.Pressed)
            return;

        switch (mouse.ButtonIndex)
        {
            case MouseButton.WheelDown:
                _targetZoom = Mathf.Clamp(_targetZoom / ZoomStep, MinZoom, MaxZoom);
                break;
            case MouseButton.WheelUp:
                _targetZoom = Mathf.Clamp(_targetZoom * ZoomStep, MinZoom, MaxZoom);
                break;
            case MouseButton.Left when _hud.HandBuildingKind is { } buildingKind:
                _world.ReceiveCommand(
                    new PlaceMachineAt(buildingKind, MouseTileCoord(), PlaceRotation, _playerId));
                break;
            case MouseButton.Left:
                InspectMachineUnderCursor();
                break;
            case MouseButton.Right when !_hud.HandStack.IsEmpty:
                _hud.ReturnHand();
                break;
            case MouseButton.Right:
                InspectMachineUnderCursor();
                break;
            case MouseButton.Middle:
                _world.ReceiveCommand(new RemoveMachineAt(MouseTileCoord(), _playerId));
                break;
        }
    }

    /// <summary> Opens the inspection window for the machine under the cursor, if any. </summary>
    private void InspectMachineUnderCursor()
    {
        if (_world.ChunkManager.GetMachineAt(MouseTileCoord()) is { } machine)
            _hud.MachineWindow.Open(machine);
    }

    /// <summary> The controlled player's position in world (cell) coordinates. </summary>
    private Vec2 PlayerCoord() => _world.ChunkManager.GetEntityById(_playerId).Coord;

    /// <summary> The controlled player's position in pixels. </summary>
    private Vector2 PlayerPixelPosition() => PlayerCoord().ToGodot() * _cellSize;

    /// <summary> The world tile currently under the mouse cursor. </summary>
    private Vec2I MouseTileCoord()
    {
        var coord = Camera.GetGlobalMousePosition();
        var tile = (coord / _cellSize).Floor();
        return new Vec2I((int)tile.X, (int)tile.Y);
    }
}
