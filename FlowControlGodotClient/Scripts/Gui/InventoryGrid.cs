using System;
using System.Collections.Generic;
using System.Text;
using FlowControlGodotClient.ResourceRegistries;
using FlowControlModel.Inventories;
using Godot;

namespace FlowControlGodotClient.Gui;

/// <summary>
/// A reusable grid of inventory slots. Renders a list of <see cref="ItemStack"/>s
/// (including empty slots) and reports clicks on them.
/// </summary>
public partial class InventoryGrid : GridContainer
{
    /// <summary> Pixel size of one slot. </summary>
    private const int SlotSize = 40;

    private ResourceRegistry _resourceRegistry = null!;
    private Func<IReadOnlyList<ItemStack>> _slotsSource = null!;
    private readonly List<Button> _slotButtons = [];
    private string _signature = "";

    /// <summary> Emitted when a slot is clicked; carries the slot index and its current stack. </summary>
    public event Action<int, ItemStack>? SlotClicked;

    /// <summary> Initializes the grid over a live list of slots (re-read every frame). </summary>
    public void Setup(ResourceRegistry resourceRegistry, Func<IReadOnlyList<ItemStack>> slotsSource, int columns = 8)
    {
        _resourceRegistry = resourceRegistry;
        _slotsSource = slotsSource;
        Columns = columns;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_slotsSource == null)
            return;
        Refresh();
    }

    /// <summary> Rebuilds the slot widgets if the underlying inventory changed. </summary>
    private void Refresh()
    {
        var slots = _slotsSource();

        var sb = new StringBuilder();
        foreach (var stack in slots)
            sb.Append(stack.IsEmpty ? "|" : $"{stack.Lite.Kind}:{stack.Count}|");
        var signature = sb.ToString();
        if (signature == _signature)
            return;
        _signature = signature;

        while (_slotButtons.Count < slots.Count)
        {
            var index = _slotButtons.Count;
            var button = new Button
            {
                CustomMinimumSize = new Vector2(SlotSize, SlotSize),
                IconAlignment = HorizontalAlignment.Center,
                ExpandIcon = true,
            };
            button.Pressed += () => SlotClicked?.Invoke(index, _slotsSource()[index]);
            AddChild(button);
            _slotButtons.Add(button);
        }
        while (_slotButtons.Count > slots.Count)
        {
            var button = _slotButtons[^1];
            _slotButtons.RemoveAt(_slotButtons.Count - 1);
            RemoveChild(button);
            button.QueueFree();
        }

        for (var i = 0; i < slots.Count; i++)
        {
            var stack = slots[i];
            _slotButtons[i].Icon = stack.IsEmpty ? null : _resourceRegistry.FindItemTexture(stack.Lite.Kind);
            _slotButtons[i].Text = stack.IsEmpty ? "" : $"x{stack.Count}";
            _slotButtons[i].TooltipText = stack.IsEmpty ? "" : stack.Lite.Kind;
        }
    }
}
