using System;
using FlowControlModel.Inventories;

namespace FlowControlModel.Entities;

/// <summary>
/// Defines the contract for looking up an <see cref="Entity"/>'s state but not modifying it.
/// </summary>
public interface IEntity
{
    /// <summary> Static definition data for this entity type. Immutable. </summary>
    EntityLite Lite { get; }

    /// <summary> Global position (center). Immutable. </summary>
    Vec2 Coord { get; }

    /// <summary> The collision box in world coordinates. Immutable. </summary>
    Rect Box { get; }

    /// <summary> Entity's unique identifier. </summary>
    uint Id { get; }

    /// <summary> Entity's inventory. Empty for entity kinds registered without one. </summary>
    Inventory Inventory { get; }
}

/// <summary>
/// An Entity instance which can be placed in the world or used as a prototype.
/// </summary>
internal class Entity(uint id, EntityLite lite, Vec2 coord, EntityLogic? logic) : IEntity, IDisposable
{
    /// <summary> Entity's unique identifier. </summary>
    public uint Id { get; } = id;

    /// <summary> Static definition data for this entity type. </summary>
    public EntityLite Lite { get; } = lite;

    /// <summary> Global position (center). Must be set exclusively by <c>ChunkManager.MoveEntity</c>. </summary>
    public Vec2 Coord { get; set; } = coord;

    /// <summary> The collision box in world coordinates. </summary>
    public Rect Box => new(Coord - Lite.BoxSize / 2, Lite.BoxSize);

    /// <summary> Entity's inventory. Empty for entity kinds registered without one. </summary>
    public Inventory Inventory { get; } = new(lite.InventoryDimensions);

    /// <summary> The autonomous behavior of the entity. <c>null</c> for controlled entities. </summary>
    public EntityLogic? Logic { get; } = logic;

    /// <summary> Prepares to be deleted. </summary>
    public void Dispose()
    {
        Logic?.Dispose();
        GC.SuppressFinalize(this);
    }
}
