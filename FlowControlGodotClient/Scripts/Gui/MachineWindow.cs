using System;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Inventories;
using FlowControlModel.Machines;
using Godot;

namespace FlowControlGodotClient.Gui;

/// <summary>
/// The machine inspection window: shows the machine's kind, rotation, logic state
/// and inventory sections. Opened by clicking a machine with an empty hand;
/// closes itself when the machine is removed from the world.
/// Slot clicks are reported outward so the HUD can exchange items with the hand.
/// </summary>
public partial class MachineWindow : PanelContainer
{
    private ResourceRegistry _resourceRegistry = null!;
    private Action<uint, InventorySection, int> _onSlotClick = null!;
    private IMachine? _machine;

    private Label _title = null!;
    private Label _state = null!;
    private VBoxContainer _sections = null!;

    /// <summary> The machine currently on display, or <c>null</c> when the window is closed. </summary>
    public IMachine? Machine => _machine;

    /// <summary> Initializes the window's widget tree (hidden by default). </summary>
    /// <param name="resourceRegistry"> Visual assets for slot icons. </param>
    /// <param name="onSlotClick"> Called with (machineId, section, slotIndex) when a slot is clicked. </param>
    public void Setup(ResourceRegistry resourceRegistry, Action<uint, InventorySection, int> onSlotClick)
    {
        _resourceRegistry = resourceRegistry;
        _onSlotClick = onSlotClick;

        var layout = new VBoxContainer();
        AddChild(layout);

        var header = new HBoxContainer();
        layout.AddChild(header);

        _title = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(_title);

        var close = new Button { Text = "✕" };
        close.Pressed += Close;
        header.AddChild(close);

        _state = new Label();
        layout.AddChild(_state);

        _sections = new VBoxContainer();
        layout.AddChild(_sections);

        Visible = false;
    }

    /// <summary> Opens the window for the specified machine. </summary>
    public void Open(IMachine machine)
    {
        if (_machine?.Id == machine.Id)
            return;
        Close();
        _machine = machine;

        AddSection("Input", InventorySection.Input, () => machine.Inventory.InputSlots);
        AddSection("Blob", InventorySection.Blob, () => machine.Inventory.BlobSlots);
        AddSection("Output", InventorySection.Output, () => machine.Inventory.OutputSlots);

        Visible = true;
    }

    /// <summary> Closes the window. </summary>
    public void Close()
    {
        _machine = null;
        Visible = false;
        foreach (var child in _sections.GetChildren())
        {
            _sections.RemoveChild(child);
            child.QueueFree();
        }
    }

    /// <summary> Closes the window if the removed machine is the one on display. </summary>
    public void OnMachineRemoved(IMachine machine)
    {
        if (_machine?.Id == machine.Id)
            Close();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_machine == null)
            return;

        _title.Text = $"{_machine.Lite.Kind} #{_machine.Id} ({_machine.Rotation})";
        _state.Text = _machine.LogicState ?? "";
        _state.Visible = _machine.LogicState != null;
    }

    /// <summary> Adds a labeled slot grid for one inventory section (skipped when empty). </summary>
    private void AddSection(string name, InventorySection section,
        Func<System.Collections.Generic.IReadOnlyList<ItemStack>> slots)
    {
        if (slots().Count == 0)
            return;

        _sections.AddChild(new Label { Text = name });
        var grid = new InventoryGrid();
        grid.Setup(_resourceRegistry, slots);
        grid.SlotClicked += (index, _) =>
        {
            if (_machine != null)
                _onSlotClick(_machine.Id, section, index);
        };
        _sections.AddChild(grid);
    }
}
