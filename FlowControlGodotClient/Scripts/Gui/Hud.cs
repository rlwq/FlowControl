using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel;
using FlowControlModel.Inventories;
using Godot;

namespace FlowControlGodotClient.Gui;

/// <summary>
/// The in-game GUI layer: player inventory panel, machine inspection window,
/// the cursor slot display (the model-side "hand" carrying any item stack)
/// and pause / simulation speed controls.
/// </summary>
/// <remarks>
/// The hand itself lives in the model (<c>IEntity.HandStack</c>); the HUD only displays it
/// and translates slot clicks into <see cref="ExchangeSlotWithHand"/> commands, so items
/// move between inventories transactionally.
/// </remarks>
[GlobalClass]
public partial class Hud : CanvasLayer
{
    private WorldSim _world = null!;
    private FlowControlModel.Factories.Registry _registry = null!;
    private ResourceRegistry _resourceRegistry = null!;
    private GameScene _gameScene = null!;
    private uint _playerId;

    private PanelContainer _inventoryPanel = null!;
    private InventoryGrid _inventoryGrid = null!;
    private MachineWindow _machineWindow = null!;
    private TextureRect _handIcon = null!;
    private Label _handCount = null!;
    private Label _simLabel = null!;

    /// <summary> The stack currently held in the player's hand (cursor slot). </summary>
    public ItemStack HandStack => _world.ChunkManager.GetEntityById(_playerId).HandStack;

    /// <summary>
    /// The machine kind held in the hand, or <c>null</c> when the hand is empty
    /// or holds a non-building item. Drives the placement ghost.
    /// </summary>
    public string? HandBuildingKind
    {
        get
        {
            var hand = HandStack;
            return !hand.IsEmpty && _registry.HasMachineKind(hand.Lite.Kind) ? hand.Lite.Kind : null;
        }
    }

    /// <summary> The machine inspection window. </summary>
    public MachineWindow MachineWindow => _machineWindow;

    /// <summary> Initializes the HUD and builds its widget tree. </summary>
    public void Setup(WorldSim world, FlowControlModel.Factories.Registry registry,
        ResourceRegistry resourceRegistry, GameScene gameScene, uint playerId)
    {
        _world = world;
        _registry = registry;
        _resourceRegistry = resourceRegistry;
        _gameScene = gameScene;
        _playerId = playerId;

        BuildInventoryPanel();
        BuildMachineWindow();
        BuildSimControls();
        BuildHandIcon();

        world.ChunkManager.MachineRemoved += _machineWindow.OnMachineRemoved;
    }

    /// <summary> Sends the hand stack back into the player's inventory. </summary>
    public void ReturnHand() => _world.ReceiveCommand(new ReturnHand(_playerId));

    /// <summary> Toggles the player inventory panel. </summary>
    public void ToggleInventory() => _inventoryPanel.Visible = !_inventoryPanel.Visible;

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_world == null)
            return;

        // The cursor slot display follows the mouse
        var hand = HandStack;
        _handIcon.Visible = !hand.IsEmpty;
        if (!hand.IsEmpty)
        {
            _handIcon.Position = _handIcon.GetViewport().GetMousePosition() + new Vector2(12, 12);
            _handIcon.Texture = _resourceRegistry.FindItemTexture(hand.Lite.Kind);
            _handCount.Text = $"x{hand.Count}";
        }

        _simLabel.Text = _gameScene.Paused
            ? "⏸ paused"
            : $"▶ {_gameScene.SpeedMultiplier:0.##}x";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (_world == null)
            return;

        if (@event.IsActionPressed("toggle_inventory"))
            ToggleInventory();
        if (@event.IsActionPressed("pause_sim"))
            _gameScene.Paused = !_gameScene.Paused;
        if (@event.IsActionPressed("sim_speed_up"))
            _gameScene.SpeedMultiplier = Mathf.Clamp(_gameScene.SpeedMultiplier * 2, 0.25f, 4f);
        if (@event.IsActionPressed("sim_speed_down"))
            _gameScene.SpeedMultiplier = Mathf.Clamp(_gameScene.SpeedMultiplier / 2, 0.25f, 4f);

        if (@event.IsActionPressed("ui_cancel") || @event.IsActionPressed("clear_hand"))
        {
            if (!HandStack.IsEmpty) ReturnHand();
            else if (_machineWindow.Machine != null) _machineWindow.Close();
            else if (_inventoryPanel.Visible) _inventoryPanel.Visible = false;
        }
    }

    private Inventory PlayerInventory() =>
        _world.ChunkManager.GetEntityById(_playerId).Inventory;

    private void BuildInventoryPanel()
    {
        _inventoryPanel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = -170, OffsetRight = 170, OffsetTop = -140, OffsetBottom = -40,
        };
        AddChild(_inventoryPanel);

        var layout = new VBoxContainer();
        _inventoryPanel.AddChild(layout);
        layout.AddChild(new Label { Text = "Inventory" });

        _inventoryGrid = new InventoryGrid();
        _inventoryGrid.Setup(_resourceRegistry, () => PlayerInventory().BlobSlots);
        _inventoryGrid.SlotClicked += (index, _) =>
            _world.ReceiveCommand(new ExchangeSlotWithHand(_playerId, InventorySection.Blob, index));
        layout.AddChild(_inventoryGrid);

        _inventoryPanel.Visible = false;
    }

    private void BuildMachineWindow()
    {
        _machineWindow = new MachineWindow
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 0f,
            OffsetLeft = -280, OffsetRight = -16, OffsetTop = 16,
            GrowHorizontal = Control.GrowDirection.Begin,
        };
        _machineWindow.Setup(_resourceRegistry, (machineId, section, slotIndex) =>
            _world.ReceiveCommand(new ExchangeSlotWithHand(_playerId, section, slotIndex, machineId)));
        AddChild(_machineWindow);
    }

    private void BuildSimControls()
    {
        var bar = new HBoxContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f,
            OffsetLeft = -80, OffsetRight = 80, OffsetTop = 8,
        };
        AddChild(bar);

        var slower = new Button { Text = "−" };
        slower.Pressed += () =>
            _gameScene.SpeedMultiplier = Mathf.Clamp(_gameScene.SpeedMultiplier / 2, 0.25f, 4f);
        bar.AddChild(slower);

        var pause = new Button { Text = "⏯" };
        pause.Pressed += () => _gameScene.Paused = !_gameScene.Paused;
        bar.AddChild(pause);

        var faster = new Button { Text = "+" };
        faster.Pressed += () =>
            _gameScene.SpeedMultiplier = Mathf.Clamp(_gameScene.SpeedMultiplier * 2, 0.25f, 4f);
        bar.AddChild(faster);

        _simLabel = new Label { VerticalAlignment = VerticalAlignment.Center };
        bar.AddChild(_simLabel);
    }

    private void BuildHandIcon()
    {
        _handIcon = new TextureRect
        {
            CustomMinimumSize = new Vector2(28, 28),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            Size = new Vector2(28, 28),
        };
        AddChild(_handIcon);

        _handCount = new Label
        {
            Position = new Vector2(14, 14),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _handIcon.AddChild(_handCount);
    }
}
