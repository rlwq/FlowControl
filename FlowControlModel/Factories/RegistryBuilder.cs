using System;
using System.Collections.Generic;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Inventories;
using FlowControlModel.World;

namespace FlowControlModel.Factories;

public partial class Registry
{
    /// <summary>
    /// Implements the Builder pattern to safely construct and configure a <see cref="Registry"/>.
    /// Misuse (duplicate registrations, use after <see cref="Build"/>, dangling references)
    /// throws immediately instead of corrupting the registry.
    /// </summary>
    public class RegistryBuilder
    {
        private Registry? _registry = new();
        private readonly Dictionary<string, (string ItemKind, int PeriodTicks)> _pendingSpawners = [];

        /// <summary> The registry under construction. Throws if <see cref="Build"/> was already called. </summary>
        private Registry Registry =>
            _registry ?? throw new InvalidOperationException("Registry is already built.");

        /// <summary> Registers a new machine type in the registry. </summary>
        public RegistryBuilder RegisterMachine(
            string kind, Vec2I dimensions, InventoryDimensions? invDims = null,
            bool playerBuildable = true, bool indestructible = false)
        {
            if (Registry._machineLites.ContainsKey(kind))
                throw new ArgumentException($"Machine '{kind}' is already registered.", nameof(kind));

            Registry._machineLites.Add(
                kind, new MachineLite(kind, dimensions, invDims, playerBuildable, indestructible));
            return this;
        }

        /// <summary>
        /// Registers a machine logic prototype and associates it with one machine kind.
        /// If a collections of Vec2I is provided (relative offsets), logic is
        /// considered iterative (can interact with other machines).
        /// </summary>
        public RegistryBuilder RegisterMachineLogic(string kind, MachineLogic logic, IReadOnlyCollection<Vec2I>? offsets = null)
        {
            if (!Registry._machineLites.ContainsKey(kind))
                throw new ArgumentException($"Machine '{kind}' is not registered.", nameof(kind));
            if (Registry._machineLogics.ContainsKey(kind))
                throw new ArgumentException($"Machine logic '{kind}' is already registered.", nameof(kind));

            Registry._machineLogics.Add(kind, logic);

            if (offsets == null) return this;

            Registry._machineLogicObserverOffsets.Add(kind, [.. offsets]);
            return this;
        }

        /// <summary> Registers a new item type in the registry. </summary>
        public RegistryBuilder RegisterItem(string kind, int stackSize)
        {
            if (Registry._itemLites.ContainsKey(kind))
                throw new ArgumentException($"Item '{kind}' is already registered.", nameof(kind));

            Registry._itemLites.Add(kind, new ItemLite(kind, stackSize));
            return this;
        }

        /// <summary> Registers a new entity type in the registry. </summary>
        public RegistryBuilder RegisterEntity(string kind, Vec2 boxSize, InventoryDimensions? invDims = null)
        {
            if (Registry._entityLites.ContainsKey(kind))
                throw new ArgumentException($"Entity '{kind}' is already registered.", nameof(kind));

            Registry._entityLites.Add(kind, new EntityLite(kind, boxSize, invDims));
            return this;
        }

        /// <summary> Registers an entity logic prototype and associates it with one entity kind. </summary>
        public RegistryBuilder RegisterEntityLogic(string kind, EntityLogic logic)
        {
            if (!Registry._entityLites.ContainsKey(kind))
                throw new ArgumentException($"Entity '{kind}' is not registered.", nameof(kind));
            if (Registry._entityLogics.ContainsKey(kind))
                throw new ArgumentException($"Entity logic '{kind}' is already registered.", nameof(kind));

            Registry._entityLogics.Add(kind, logic);
            return this;
        }

        /// <summary>
        /// Registers a new ground type in the registry. When <paramref name="spawnsItemKind"/>
        /// is provided, every tile of this ground spawns that item every
        /// <paramref name="spawnPeriodTicks"/> ticks (e.g. ore deposits). The item kind is
        /// resolved when <see cref="Build"/> is called, so it may be registered later.
        /// </summary>
        public RegistryBuilder RegisterGround(
            string kind, string? spawnsItemKind = null, int spawnPeriodTicks = 0,
            bool passable = true, float speedModifier = 1f)
        {
            if (Registry._groundLites.ContainsKey(kind))
                throw new ArgumentException($"Ground '{kind}' is already registered.", nameof(kind));
            if (spawnsItemKind != null && spawnPeriodTicks <= 0)
                throw new ArgumentException(
                    $"Ground '{kind}' spawns '{spawnsItemKind}' but its spawn period is not positive.",
                    nameof(spawnPeriodTicks));
            if (speedModifier <= 0)
                throw new ArgumentException(
                    $"Ground '{kind}' has a non-positive speed modifier.", nameof(speedModifier));

            Registry._groundLites.Add(kind, new GroundLite(kind, passable, speedModifier));
            if (spawnsItemKind != null)
                _pendingSpawners.Add(kind, (spawnsItemKind, spawnPeriodTicks));
            return this;
        }

        /// <summary> Finalizes the building process and returns the configured <see cref="Registry"/>. </summary>
        public Registry Build()
        {
            var result = Registry;

            foreach (var (groundKind, (itemKind, periodTicks)) in _pendingSpawners)
            {
                if (!result._itemLites.TryGetValue(itemKind, out var itemLite))
                    throw new InvalidOperationException(
                        $"Ground '{groundKind}' spawns item '{itemKind}', which is not registered.");
                result._groundLites[groundKind].Spawner = new ItemSpawner(itemLite, periodTicks);
            }

            _registry = null;
            return result;
        }
    }
}
