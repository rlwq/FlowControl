using System;
using System.Collections.Generic;

namespace FlowControlModel.Inventories;

/// <summary> Slot counts of an inventory's three sections. </summary>
/// <param name="input"> Number of slots in the input section. </param>
/// <param name="blob"> Number of slots in the general-purpose (blob) section. </param>
/// <param name="output"> Number of slots in the output section. </param>
public readonly struct InventoryDimensions(int input, int blob, int output)
{
    /// <summary> Dimensions of an inventory with no slots at all. </summary>
    public static InventoryDimensions Empty => new(0, 0, 0);

    /// <summary> Number of slots in the input section. </summary>
    public readonly int Input = input;

    /// <summary> Number of slots in the general-purpose (blob) section. </summary>
    public readonly int Blob = blob;

    /// <summary> Number of slots in the output section. </summary>
    public readonly int Output = output;

    /// <summary> Whether all three sections have zero slots. </summary>
    public bool IsEmpty => Input == 0 && Blob == 0 && Output == 0;
}


/// <summary> Names one of the three sections of an <see cref="Inventory"/>. </summary>
public enum InventorySection
{
    /// <summary> The section items are inserted into first. </summary>
    Input,
    /// <summary> The general-purpose section. </summary>
    Blob,
    /// <summary> The section items are extracted from first. </summary>
    Output,
}

/// <summary>
/// A collection of slots where items can be stored.
/// It consists of three sections: <c>input</c>, <c>blob</c>, and <c>output</c>. When inserting, items first
/// arrive in the <c>input</c> section; if it is full, they go into the <c>blob</c> section. When extracting,
/// items are first taken from the <c>output</c> section; if it is empty, they are taken from the <c>blob</c> section.
/// </summary>
public class Inventory
{
    private static ItemStack InsertItemToSlots(ItemStack[] slots, ItemStack item)
    {
        for (var i = 0; i < slots.Length && !item.IsEmpty; i++)
        {
            ref var slot = ref slots[i];
            if (slot.IsEmpty || !slot.Lite.Equals(item.Lite))
                continue;

            var newCount = Math.Min(slot.Count + item.Count, item.Lite.StackSize);
            item.Count -= newCount - slot.Count;
            slot.Count = newCount;
        }

        for (var i = 0; i < slots.Length && !item.IsEmpty; i++)
        {
            ref var slot = ref slots[i];
            if (!slot.IsEmpty)
                continue;

            var newCount = Math.Min(item.Count, item.Lite.StackSize);
            item.Count -= newCount;
            slot = new ItemStack(newCount, item.Lite);
        }
        return item;
    }

    private static ItemStack ExtractItemFromSlots(ItemStack[] slots, ItemStack item)
    {
        var extracted = 0;
        for (var i = slots.Length - 1; i >= 0 && !item.IsEmpty; i--)
        {
            ref var slot = ref slots[i];
            if (slot.IsEmpty || !slot.Lite.Equals(item.Lite))
                continue;

            var extractedLoc = Math.Min(slot.Count, item.Count);
            item.Count -= extractedLoc;
            extracted += extractedLoc;
            slot.Count -= extractedLoc;
        }
        return new ItemStack(extracted, item.Lite);
    }
    
    private static ItemStack ExtractFromSlots(ItemStack[] slots, int count)
    {
        ItemLite? lite = null;
        var extractedCount = 0;
        for (var i = slots.Length - 1; i >= 0 && count > 0; i--)
        {
            ref var slot = ref slots[i];
            if (slot.IsEmpty || lite != null && !lite.Equals(slot.Lite))
                continue;

            var extractedLoc = Math.Min(slot.Count, count);
            count -= extractedLoc;
            extractedCount += extractedLoc;
            slot.Count -= extractedLoc;
            lite = slot.Lite;
        }
        
        return new ItemStack(extractedCount, lite!);
    }

    private readonly ItemStack[] _input;
    private readonly ItemStack[] _blob;
    private readonly ItemStack[] _output;
    
    /// <summary> Builds an inventory from received dimension parameters. </summary>
    public Inventory(int inputSize, int blobSize, int outputSize)
    {
        _input = new ItemStack[inputSize];
        _blob = new ItemStack[blobSize];
        _output = new ItemStack[outputSize];
    }
    
    /// <summary> Builds an inventory from received dimensions object. </summary>
    public Inventory(InventoryDimensions dimensions)
    {
        _input = new ItemStack[dimensions.Input];
        _blob = new ItemStack[dimensions.Blob];
        _output = new ItemStack[dimensions.Output];
    }

    /// <summary>
    /// Inserts a bunch of items. Returns items that could not be inserted.
    /// </summary>
    public ItemStack InsertItem(ItemStack item)
    {
        item = InsertItemToSlots(_input, item);
        item = InsertItemToSlots(_blob, item);
        return item;
    }
    
    /// <summary>
    /// Extracts and returns a bunch of items of the same type as the provided stack,
    /// up to the requested amount (if possible).
    /// </summary>
    public ItemStack ExtractItem(ItemStack item)
    {
        var extractedCount = 0;
        
        var extracted = ExtractItemFromSlots(_output, item);
        item.Count -= extracted.Count;
        extractedCount += extracted.Count;
        
        extracted = ExtractItemFromSlots(_blob, item);
        item.Count -= extracted.Count;
        extractedCount += extracted.Count;
        
        item.Count = extractedCount;
        return item;
    }
    
    /// <summary> Whether the section contains a slot with this index. </summary>
    public bool IsValidSlot(InventorySection section, int slotIndex) =>
        slotIndex >= 0 && slotIndex < SectionSlots(section).Length;

    /// <summary> Returns the stack in the specified slot. </summary>
    internal ItemStack GetSlot(InventorySection section, int slotIndex) =>
        SectionSlots(section)[slotIndex];

    /// <summary> Replaces the stack in the specified slot. </summary>
    internal void SetSlot(InventorySection section, int slotIndex, ItemStack stack) =>
        SectionSlots(section)[slotIndex] = stack;

    private ItemStack[] SectionSlots(InventorySection section) => section switch
    {
        InventorySection.Input => _input,
        InventorySection.Output => _output,
        _ => _blob,
    };

    /// <summary> Read-only view of the input section's slots (including empty ones). </summary>
    public IReadOnlyList<ItemStack> InputSlots => _input;

    /// <summary> Read-only view of the blob section's slots (including empty ones). </summary>
    public IReadOnlyList<ItemStack> BlobSlots => _blob;

    /// <summary> Read-only view of the output section's slots (including empty ones). </summary>
    public IReadOnlyList<ItemStack> OutputSlots => _output;

    /// <summary>
    /// Counts items across all sections: of one kind, or of any kind when
    /// <paramref name="itemKind"/> is <c>null</c>.
    /// </summary>
    public int CountItems(string? itemKind = null)
    {
        var total = 0;
        foreach (var stack in EnumerateStacks())
            if (itemKind == null || stack.Lite.Kind == itemKind)
                total += stack.Count;
        return total;
    }

    /// <summary> Enumerates all non-empty stacks across the input, blob and output sections. </summary>
    public IEnumerable<ItemStack> EnumerateStacks()
    {
        foreach (var slot in _input)
            if (!slot.IsEmpty) yield return slot;
        foreach (var slot in _blob)
            if (!slot.IsEmpty) yield return slot;
        foreach (var slot in _output)
            if (!slot.IsEmpty) yield return slot;
    }

    /// <summary>
    /// Extracts and returns a bunch of items of the same type, up to the requested amount (if possible).
    /// </summary>
    public ItemStack Extract(int count)
    {
        var extractedOutput = ExtractFromSlots(_output, count);
        if (extractedOutput.IsEmpty) return ExtractFromSlots(_blob, count);
        var extractBlobQuery = new ItemStack(count - extractedOutput.Count, extractedOutput.Lite);
        var extractedBlob = ExtractItem(extractBlobQuery);
        extractedOutput.Count += extractedBlob.Count;
        return extractedOutput;
    }
}

/// <summary>
/// Represents a bunch of items of the same type. May represent an inventory slot,
/// a response to a query, etc. The <see cref="Count"/> field may be bigger than
/// <see cref="ItemLite.StackSize"/> when it represents a transfer between two inventories.
/// </summary>
public struct ItemStack(int count, ItemLite lite)
{
    /// <summary>
    /// Retrieves a <see cref="ItemStack"/> with <c><see cref="IsEmpty"/> = true</c>;
    /// </summary>
    public static readonly ItemStack Empty = default;
    
    /// <summary> Number of items in the stack. </summary>
    public int Count = count;

    /// <summary> The item type. </summary>
    public readonly ItemLite Lite = lite;
    
    /// <summary> Tells whether the stack is empty. </summary>
    public readonly bool IsEmpty => Count == 0;
}
