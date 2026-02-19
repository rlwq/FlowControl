using FlowControlModel;
using FlowControlBusiness.Machines;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Factories;
using Godot;

namespace FlowControlGodotClient;

[GlobalClass]
public partial class GameScene : Node
{
    [Export]
    public int TickRate = 20;
    private double _lastTick = 0;
    private double _clock = 0;

    [Export]
    public World.ChunkManagerView ChunkManagerView = null!;

    [Export]
    public Camera2D Camera = null!;

    [Export]
    public Texture2D[] MachineSprites = null!;

    [Export]
    public Texture2D[] EntitySprites = null!;

    private Registry _registry = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private WorldSim _world = null!;

    public override void _Ready()
    {
        var regBuilder = new Registry.RegistryBuilder();
        regBuilder
            .SetChunkSize(20)
            .RegisterGround("stone")
            .RegisterGround("grass")
            .RegisterMachine("chest", new Vector2I(2, 1))
            .RegisterMachine("manipulator", new Vector2I(1, 1))
            .RegisterMachineLogic("chest", new Dumb())
            .RegisterMachineLogic("manipulator", new Manipulator(), [new Vector2I(-1, 0), new Vector2I(0, 1)]);
        _registry = regBuilder.Build();

        var resRegBuilder = new ResourceRegistry.ResourceRegistryBuilder(_registry);
        resRegBuilder
            .SetCellSize(32)
            .RegisterGroundTile("stone", new Vector2I(0, 0))
            .RegisterGroundTile("grass", new Vector2I(1, 0))
            .RegisterMachineTexture("chest", MachineSprites[0])
            .RegisterMachineTexture("manipulator", MachineSprites[1]);
        _resourceRegistry = resRegBuilder.Build();

        _world = new WorldSim(_registry);

        ChunkManagerView.Setup(_world.ChunkManager, _resourceRegistry);

        _world.Tick();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _clock += delta;
        if ((_clock - _lastTick) * TickRate > 1) _lastTick = _clock;
        else return;
        
        // const float speed = 0.5f;
        // if (Input.IsKeyPressed(Key.W))
        //     _world.ReceiveCommand(new EntityStepById(_player, speed, 0));
        // if (Input.IsKeyPressed(Key.S))
        //     _world.ReceiveCommand(new EntityStepById(_player, speed, 1));
        // if (Input.IsKeyPressed(Key.A))
        //     _world.ReceiveCommand(new EntityStepById(_player, speed, 2));
        // if (Input.IsKeyPressed(Key.D))
        //     _world.ReceiveCommand(new EntityStepById(_player, speed, 3));

        _world.Tick();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelDown)
                Camera.Zoom /= 1.07f;
            else if (mouse.ButtonIndex == MouseButton.WheelUp)
                Camera.Zoom *= 1.07f;
            else if (mouse.Pressed)
            {
                var coord = Camera.GetGlobalMousePosition();
                var tileCoordFloat = (coord / _resourceRegistry.CellSize).Floor();
                var tileCoord = new Vector2I((int)tileCoordFloat.X, (int)tileCoordFloat.Y);

                switch (mouse.ButtonIndex)
                {
                    case MouseButton.Left:
                        _world.ReceiveCommand(new PlaceMachineAt("chest", tileCoord));
                        break;
                    case MouseButton.Right:
                        _world.ReceiveCommand(new PlaceMachineAt("manipulator", tileCoord));
                        break;
                    case MouseButton.Middle:
                        _world.ReceiveCommand(new RemoveMachineAt(tileCoord));
                        break;
                }
            }
        }
        CameraMoved();
    }

    private void CameraMoved()
    {
        var cameraRect = GetViewport().GetVisibleRect().Size / Camera.Zoom;
        var cameraNw = Camera.Position - cameraRect / 2;
        var cameraSe = Camera.Position + cameraRect / 2;
        var chunkNw = (cameraNw / _resourceRegistry.CellSize / _registry.ChunkSize).Floor();
        var chunkSe = (cameraSe / _resourceRegistry.CellSize / _registry.ChunkSize).Floor();
        var rect = (Rect2I)new Rect2(chunkNw, chunkSe - chunkNw).Grow(1);
        ChunkManagerView.MaintainRect(rect);
        // _chunk_manager_repr.receive_command(ChunkReprCmd.MaintainRect.new(rect));
    }
}
