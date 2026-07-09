using System;

namespace FlowControlModel.Inventories;

/// <summary> Contains the intrinsic properties of an item type. </summary>
public class ItemLite(string kind, int stackSize) : IEquatable<ItemLite>
{
    /// <summary> A unique string identifier for the item. </summary>
    public readonly string Kind = kind;

    /// <summary> The amount of items of that type that can be placed in a single inventory slot. </summary>
    public readonly int StackSize = stackSize;

    /// <summary> Returns true if the Kind fields are equal. </summary>
    public bool Equals(ItemLite? other)
    {
        if (other is null)
            return false;
        return Kind == other.Kind;
    }
    
    /// <summary> Casts object to <see cref="ItemLite"/> and checks if they are the same. </summary>
    public override bool Equals(object? obj) => Equals(obj as ItemLite);
    
    /// <summary> Retrieves kind's string name hash code </summary>
    public override int GetHashCode() => Kind.GetHashCode();
}
