using System;

namespace FlowControlModel.Inventories;

public readonly struct InventoryDimensions(int input, int blob, int output)
{
    public static InventoryDimensions Empty => new(0, 0, 0);
    public readonly int Input = input;
    public readonly int Blob = blob;
    public readonly int Output = output;
    
    public bool IsEmpty => Input == 0 && Blob == 0 && Output == 0;
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
    public bool IsEmpty
    {
        readonly get => Count == 0;
        set => Count = value ? 0 : Count;
    }
}
