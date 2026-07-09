using FlowControlModel;
using FlowControlBusiness.Entities;
using FlowControlBusiness.Machines;
using FlowControlBusiness.WorldGeneration;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient;

[GlobalClass]
public partial class GameScene : Node
{
    [Export]
    public int TickRate = 20;

    /// <summary>
    /// Upper bound of catch-up ticks per frame: below it the simulation compensates
    /// slow frames, beyond it the backlog is dropped to avoid the spiral of death.
    /// </summary>
    private const int MaxTicksPerFrame = 5;

    private double _lastTick = 0;
    private double _clock = 0;

    [Export]
    public World.ChunkManagerView ChunkManagerView = null!;

    [Export]
    public Camera2D Camera = null!;

    [Export]
    public InputHandler InputHandler = null!;

    [Export]
    public Gizmo Gizmo = null!;

    [Export]
    public Gui.Hud Hud = null!;

    [Export]
    public Gui.PlacementGhost PlacementGhost = null!;

    [Export]
    public int WorldSeed = 20260708;

    /// <summary> Whether the simulation is paused (rendering and camera keep working). </summary>
    public bool Paused { get; set; }

    /// <summary> Simulation speed factor applied to <see cref="TickRate"/>. </summary>
    public float SpeedMultiplier { get; set; } = 1f;

    private WorldGrid _grid = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private WorldSim _world = null!;

    public override void _Ready()
    {
        _grid = new WorldGrid(20);

        var regBuilder = new Registry.RegistryBuilder();
        regBuilder
            .RegisterItem("iron_bar", 16)
            .RegisterItem("iron_ore", 32)
            .RegisterItem("chest", 8)
            .RegisterItem("manipulator", 8)
            .RegisterItem("oven", 8)
            .RegisterItem("door", 8)
            .RegisterGround("stone")
            .RegisterGround("grass")
            .RegisterGround("iron_deposit", spawnsItemKind: "iron_ore", spawnPeriodTicks: 300)
            .RegisterMachine("chest", new Vec2I(2, 1), new InventoryDimensions(0, 8, 0))
            .RegisterMachine("manipulator", new Vec2I(1, 1), new InventoryDimensions(0, 1, 0))
            .RegisterMachine("oven", new Vec2I(2, 2), new InventoryDimensions(1, 2, 1))
            .RegisterMachine("door", new Vec2I(1, 1))
            .RegisterEntity("cow", new Vec2(1, 1))
            .RegisterEntityLogic("cow", new Wanderer(0.06f))
            .RegisterMachineLogic("chest", new Dumb())
            .RegisterMachineLogic("door", new Dumb())
            .RegisterMachineLogic("oven", new Oven())
            .RegisterMachineLogic("manipulator", new Manipulator(), [new Vec2I(-1, 0), new Vec2I(0, 1)]);
        var registry = regBuilder.Build();

        var chestTexture = GD.Load<Texture2D>("res://Sprites/Machines/chest.png");
        var manipulatorTexture = GD.Load<Texture2D>("res://Sprites/Machines/manipulator.png");
        var ovenTexture = GD.Load<Texture2D>("res://Sprites/Machines/oven.png");
        var doorTexture = GD.Load<Texture2D>("res://Sprites/Machines/door.png");

        var resRegBuilder = new ResourceRegistry.ResourceRegistryBuilder(_grid);
        resRegBuilder
            .SetCellSize(32)
            .RegisterGroundTile("stone", new Vector2I(0, 0))
            .RegisterGroundTile("grass", new Vector2I(1, 0))
            .RegisterGroundTile("iron_deposit", new Vector2I(2, 0))
            .RegisterMachineTexture("chest", chestTexture)
            .RegisterMachineTexture("manipulator", manipulatorTexture)
            .RegisterMachineTexture("oven", ovenTexture)
            .RegisterMachineTexture("door", doorTexture)
            .RegisterEntityTexture("cow", GD.Load<Texture2D>("res://Sprites/Entities/cow.png"))
            .RegisterEntityTexture("player", GD.Load<Texture2D>("res://Sprites/Entities/player.png"))
            .RegisterItemTexture("iron_bar", GD.Load<Texture2D>("res://Sprites/Items/iron_bar.png"))
            .RegisterItemTexture("iron_ore", GD.Load<Texture2D>("res://Sprites/Items/iron_ore.png"))
            // Dropped machines reuse their machine textures as item sprites
            .RegisterItemTexture("chest", chestTexture)
            .RegisterItemTexture("manipulator", manipulatorTexture)
            .RegisterItemTexture("oven", ovenTexture)
            .RegisterItemTexture("door", doorTexture);
        _resourceRegistry = resRegBuilder.Build();

        var generator = new NoiseWorldGenerator(registry, WorldSeed);
        _world = new WorldSim(registry, _grid, generator);

        ChunkManagerView.Setup(_world.ChunkManager, _resourceRegistry);

        SetupDemoScene();

        var addPlayer = new PlaceEntityAt("player", new Vec2(7.5f, 7.5f));
        _world.ReceiveCommand(addPlayer);
        _world.Tick();
        var playerId = addPlayer.EntityId!.Value;
        StockPlayer(playerId);

        Hud.Setup(_world, registry, _resourceRegistry, this, playerId);
        PlacementGhost.Setup(_world, registry, _resourceRegistry, playerId);
        InputHandler.Setup(_world, playerId, _resourceRegistry.CellSize, Hud, PlacementGhost);
        Gizmo.Setup(_world, playerId, _resourceRegistry.CellSize);

        // The camera starts at the player; load the surrounding chunks right away
        CameraMoved();

        if (OS.GetEnvironment("FC_GUI_SMOKE") == "1")
        {
            _world.Tick(); // deliver the starting kit before taking an item in hand
            // Take the chest stack (inventory slot 0) into the hand through the command queue
            _world.ReceiveCommand(new ExchangeSlotWithHand(
                playerId, FlowControlModel.Inventories.InventorySection.Blob, 0));
            _world.Tick();
            Input.WarpMouse(GetViewport().GetVisibleRect().Size / 2 + new Vector2(120, 60));
            Hud.ToggleInventory();
            if (_world.ChunkManager.GetMachineAt(new Vec2I(2, 3)) is { } demoChest)
                Hud.MachineWindow.Open(demoChest);
        }
    }

    /// <summary>
    /// Populates the world with a small demonstration layout:
    /// two chests connected by a manipulator, an oven, a door, a few cows,
    /// some items on the ground and a stocked player.
    /// </summary>
    private void SetupDemoScene()
    {
        // The manipulator's input observer (-1, 0) points at the first chest
        // and its output observer (0, 1) points at the second one.
        _world.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(2, 3)));
        _world.ReceiveCommand(new PlaceMachineAt("manipulator", new Vec2I(4, 3)));
        _world.ReceiveCommand(new PlaceMachineAt("chest", new Vec2I(4, 4)));

        _world.ReceiveCommand(new PlaceMachineAt("oven", new Vec2I(8, 2)));
        _world.ReceiveCommand(new PlaceMachineAt("door", new Vec2I(7, 5)));

        _world.ReceiveCommand(new PlaceEntityAt("cow", new Vec2(11.5f, 6.5f)));
        _world.ReceiveCommand(new PlaceEntityAt("cow", new Vec2(13.2f, 4.3f)));
        _world.ReceiveCommand(new PlaceEntityAt("cow", new Vec2(11.3f, 2.8f)));

        // A stone path along the workshop
        for (var x = 2; x <= 9; x++)
            _world.ReceiveCommand(new SetGroundAt("stone", new Vec2I(x, 5)));

        // Stock the first chest so the manipulator has something to move
        _world.ReceiveCommand(new InsertItemAt("iron_bar", 8, new Vec2I(2, 3)));

        // A few items lying on the ground to pick up
        _world.ReceiveCommand(new DropItemAt("iron_bar", 3, new Vec2(5.4f, 6.6f)));
        _world.ReceiveCommand(new DropItemAt("iron_ore", 2, new Vec2(9.6f, 6.4f)));
    }

    /// <summary> Gives the freshly spawned player their starting kit. </summary>
    private void StockPlayer(uint playerId)
    {
        _world.ReceiveCommand(new GiveItemsTo(playerId, "chest", 4));
        _world.ReceiveCommand(new GiveItemsTo(playerId, "manipulator", 4));
        _world.ReceiveCommand(new GiveItemsTo(playerId, "oven", 1));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _clock += delta;
        if (Paused)
        {
            _lastTick = _clock; // no catch-up burst on unpause
            return;
        }
        var tickPeriod = 1.0 / (TickRate * SpeedMultiplier);

        // Fixed-timestep catch-up loop: run every tick the wall clock owes us,
        // up to MaxTicksPerFrame per frame
        var ticks = 0;
        while (_clock - _lastTick >= tickPeriod && ticks < MaxTicksPerFrame)
        {
            _lastTick += tickPeriod;
            InputHandler.ProcessTick();
            _world.Tick();
            ticks++;
        }

        // Still behind after the cap: drop the backlog instead of spiraling
        if (_clock - _lastTick >= tickPeriod)
            _lastTick = _clock;

        if (ticks > 0)
            CameraMoved();
    }

    private void CameraMoved()
    {
        var cameraRect = GetViewport().GetVisibleRect().Size / Camera.Zoom;
        var cameraNw = Camera.Position - cameraRect / 2;
        var cameraSe = Camera.Position + cameraRect / 2;
        var chunkNw = (cameraNw / _resourceRegistry.CellSize / _grid.ChunkSize).Floor();
        var chunkSe = (cameraSe / _resourceRegistry.CellSize / _grid.ChunkSize).Floor();
        var rect = (Rect2I)new Rect2(chunkNw, chunkSe - chunkNw).Grow(1);
        ChunkManagerView.MaintainRect(rect);
    }
}
