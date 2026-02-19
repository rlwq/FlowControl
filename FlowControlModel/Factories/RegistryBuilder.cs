using System.Collections.Generic;
using System.Diagnostics;
using FlowControlModel.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Inventories;
using Godot;

namespace FlowControlModel.Factories;

public partial class Registry
{
    /// <summary>
    /// Implements the Builder pattern to safely construct and configure a <see cref="Registry"/>.
    /// </summary>
    public class RegistryBuilder
    {
        private Registry? _registry = new();

        /// <summary> Initializes a new builder and registers default types like <c>"air"</c>. </summary>
        public RegistryBuilder()
        {
            RegisterEntity("player", new Vector2I(8, 8));
            RegisterMachine("air", new Vector2I(1, 1));
        }

        /// <summary> Registers a new machine type in the registry. </summary>
        public RegistryBuilder RegisterMachine(StringName kind, Vector2I dimensions)
        {
            Debug.Assert(_registry != null, "Registry is already built.");
            Debug.Assert(
                !_registry._machineLites.ContainsKey(kind),
                $"Machine '{kind}' is already registered."
            );

            _registry._machineLites.Add(kind, new MachineLite(kind, dimensions));
            return this;
        }
        
        /// <summary>
        /// Registers a machine logic prototype and associates it with one machine kind.
        /// If a collections of Vector2I is provided (relative offsets), logic is
        /// considered iterative (can interact with other machines).
        /// </summary>
        public RegistryBuilder RegisterMachineLogic(StringName kind, MachineLogic logic, IReadOnlyCollection<Vector2I>? offsets = null)
        {
            Debug.Assert(_registry != null, "Registry is already built.");
            Debug.Assert(
                !_registry._machineLogics.ContainsKey(kind) ||
                !_registry._machineLogicObserverOffsets.ContainsKey(kind),
                $"Machine logic '{kind}' is already registered."
                );
            _registry._machineLogics.Add(kind, logic);
            
            if (offsets == null) return this;
            
            _registry._machineLogicObserverOffsets.Add(kind, new Vector2I[offsets.Count]);
            
            var i = 0;
            foreach (var offset in offsets)
                _registry._machineLogicObserverOffsets[kind][i++] = offset;

            return this;
        }
        
        /// <summary> Registers a new item type in the registry. </summary>
        public RegistryBuilder RegisterItem(StringName kind, int stackSize)
        {
            Debug.Assert(_registry != null, "Registry is already built.");
            Debug.Assert(
                !_registry._itemLites.ContainsKey(kind),
                $"Item '{kind}' is already registered."
            );

            _registry._itemLites.Add(kind, new ItemLite(kind, stackSize));
            return this;
        }

        /// <summary> Registers a new entity type in the registry. </summary>
        public RegistryBuilder RegisterEntity(StringName kind, Vector2 boxSize)
        {
            Debug.Assert(_registry != null, "Registry is already built.");
            Debug.Assert(
                !_registry._entityLites.ContainsKey(kind),
                $"Entity '{kind}' is already registered."
            );

            _registry._entityLites.Add(kind, new EntityLite(kind, boxSize));
            return this;
        }

        /// <summary> Registers a new ground type in the registry. </summary>
        public RegistryBuilder RegisterGround(StringName kind)
        {
            Debug.Assert(_registry != null, "Registry is already built.");
            Debug.Assert(
                !_registry._groundLites.ContainsKey(kind),
                $"Ground '{kind}' is already registered."
            );

            _registry._groundLites.Add(kind, new GroundLite(kind));
            return this;
        }

        /// <summary> Sets the edge length for all chunks in the resulting registry. </summary>
        public RegistryBuilder SetChunkSize(int chunkSize)
        {
            Debug.Assert(_registry != null, "Registry is already built.");

            _registry.ChunkSize = chunkSize;
            return this;
        }

        /// <summary> Finalizes the building process and returns the configured <see cref="Registry"/>. </summary>
        public Registry Build()
        {
            Debug.Assert(_registry != null, "Registry is already built.");

            var result = _registry;
            _registry = null;
            return result;
        }
    }
}
