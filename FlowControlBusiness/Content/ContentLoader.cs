using System.Collections.Generic;
using System.Linq;
using FlowControlModel;
using FlowControlModel.Factories;
using FlowControlModel.Inventories;

namespace FlowControlBusiness.Content;

/// <summary>
/// Accumulates per-kind content entries (one JSON file per kind; the file name is the kind)
/// and builds a <see cref="Registry"/> from them. Engine-free and IO-free: the client walks
/// the <c>Content/</c> directories and feeds the file texts in; mods are just extra files.
/// </summary>
/// <remarks>
/// Validation is fail-fast: unknown logic names, machines without a matching item kind
/// (every building must be placeable from the inventory), malformed dimension arrays and
/// redefined kinds all throw at load or build time, never corrupting a running game.
/// </remarks>
public sealed class ContentLoader(LogicCatalog catalog)
{
    private readonly Dictionary<string, ContentEntries.Ground> _grounds = [];
    private readonly Dictionary<string, ContentEntries.Item> _items = [];
    private readonly Dictionary<string, ContentEntries.Machine> _machines = [];
    private readonly Dictionary<string, ContentEntries.Entity> _entities = [];

    /// <summary> All loaded ground kinds. </summary>
    public IReadOnlyCollection<string> GroundKinds => _grounds.Keys;

    /// <summary> All loaded item kinds. </summary>
    public IReadOnlyCollection<string> ItemKinds => _items.Keys;

    /// <summary> All loaded machine kinds. </summary>
    public IReadOnlyCollection<string> MachineKinds => _machines.Keys;

    /// <summary> All loaded entity kinds. </summary>
    public IReadOnlyCollection<string> EntityKinds => _entities.Keys;

    /// <summary> Loads one ground kind from its file text. </summary>
    public ContentLoader AddGround(string kind, string json) =>
        Store(_grounds, kind, ContentEntries.Parse<ContentEntries.Ground>(json, kind));

    /// <summary> Loads one item kind from its file text. </summary>
    public ContentLoader AddItem(string kind, string json) =>
        Store(_items, kind, ContentEntries.Parse<ContentEntries.Item>(json, kind));

    /// <summary> Loads one machine kind from its file text. </summary>
    public ContentLoader AddMachine(string kind, string json) =>
        Store(_machines, kind, ContentEntries.Parse<ContentEntries.Machine>(json, kind));

    /// <summary> Loads one entity kind from its file text. </summary>
    public ContentLoader AddEntity(string kind, string json) =>
        Store(_entities, kind, ContentEntries.Parse<ContentEntries.Entity>(json, kind));

    /// <summary> Builds a <see cref="Registry"/> from every loaded entry. </summary>
    public Registry BuildRegistry()
    {
        ValidateMachineItems();

        var builder = new Registry.RegistryBuilder();

        foreach (var (kind, item) in _items)
            builder.RegisterItem(kind, item.StackSize);

        foreach (var (kind, ground) in _grounds)
            builder.RegisterGround(
                kind, ground.SpawnsItem, ground.SpawnPeriodTicks,
                ground.Passable, ground.SpeedModifier);

        foreach (var (kind, machine) in _machines)
        {
            builder.RegisterMachine(
                kind,
                ToVec2I(machine.Dimensions, kind, "dimensions"),
                ToInventoryDims(machine.Inventory, kind),
                machine.PlayerBuildable,
                machine.Indestructible);

            if (machine.Logic == null)
            {
                if (machine.ObserverOffsets != null)
                    throw new ContentException($"Machine '{kind}' has observer offsets but no logic.");
                continue;
            }

            var offsets = machine.ObserverOffsets
                ?.Select(offset => ToVec2I(offset, kind, "observerOffsets"))
                .ToArray();
            builder.RegisterMachineLogic(
                kind, catalog.CreateMachineLogic(machine.Logic, machine.LogicParams), offsets);
        }

        foreach (var (kind, entity) in _entities)
        {
            if (entity.BoxSize is not { Length: 2 })
                throw new ContentException($"Entity '{kind}' needs a boxSize of [width, height].");

            builder.RegisterEntity(
                kind,
                new Vec2(entity.BoxSize[0], entity.BoxSize[1]),
                ToInventoryDims(entity.Inventory, kind));

            if (entity.Logic != null)
                builder.RegisterEntityLogic(
                    kind, catalog.CreateEntityLogic(entity.Logic, entity.LogicParams));
        }

        return builder.Build();
    }

    private ContentLoader Store<TEntry>(Dictionary<string, TEntry> target, string kind, TEntry entry)
    {
        if (!target.TryAdd(kind, entry))
            throw new ContentException($"The kind '{kind}' is defined twice.");
        return this;
    }

    /// <summary>
    /// Every player-buildable machine kind must also exist as an item kind: buildings are
    /// placed from the inventory, so a machine without an item could never be built.
    /// Non-buildable machines (e.g. the Hub) are exempt.
    /// </summary>
    private void ValidateMachineItems()
    {
        foreach (var (kind, machine) in _machines)
            if (machine.PlayerBuildable && !_items.ContainsKey(kind))
                throw new ContentException(
                    $"Machine '{kind}' has no matching item: it could never be built. "
                    + $"Add Content/items/{kind}.json or mark it \"playerBuildable\": false.");
    }

    private static Vec2I ToVec2I(int[]? pair, string kind, string field) =>
        pair is { Length: 2 }
            ? new Vec2I(pair[0], pair[1])
            : throw new ContentException($"'{kind}': '{field}' must be a pair of integers.");

    private static InventoryDimensions? ToInventoryDims(int[]? dims, string kind) => dims switch
    {
        null => null,
        { Length: 3 } => new InventoryDimensions(dims[0], dims[1], dims[2]),
        _ => throw new ContentException($"'{kind}': 'inventory' must be [input, blob, output]."),
    };
}
