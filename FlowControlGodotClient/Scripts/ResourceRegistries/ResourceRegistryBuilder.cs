using System.Diagnostics;
using FlowControlModel.Factories;
using Godot;

namespace FlowControlGodotClient.ResourceRegistries;

public partial class ResourceRegistry
{
    /// <summary>
    /// Provides a fluent interface for constructing and initializing a <see cref="FlowControlGodotClient.ResourceRegistry.ResourceRegistry"/>.
    /// </summary>
    public class ResourceRegistryBuilder(Registry registry)
    {
        private ResourceRegistry? _resourceRegistry = new(registry);

        /// <summary> Registers atlas coordinates for a terrain tile type. </summary>
        public ResourceRegistryBuilder RegisterGroundTile(StringName kind, Vector2I atlasCoords)
        {
            Debug.Assert(_resourceRegistry != null, "Resource registry is already built.");
            _resourceRegistry._groundTilesAtlasCords.Add(kind, atlasCoords);
            return this;
        }

        /// <summary> Maps a machine type to its visual texture. </summary>
        public ResourceRegistryBuilder RegisterMachineTexture(StringName kind, Texture2D texture)
        {
            Debug.Assert(_resourceRegistry != null, "Resource registry is already built.");
            _resourceRegistry._machineTextures.Add(kind, texture);
            return this;
        }

        /// <summary> Maps an entity type to its visual texture. </summary>
        public ResourceRegistryBuilder RegisterEntityTexture(StringName kind, Texture2D texture)
        {
            Debug.Assert(_resourceRegistry != null, "Resource registry is already built.");
            _resourceRegistry._enityTextures.Add(kind, texture);
            return this;
        }

        /// <summary> Sets the visual scale of a single grid cell in pixels. </summary>
        public ResourceRegistryBuilder SetCellSize(int cellSize)
        {
            Debug.Assert(_resourceRegistry != null, "Resource registry is already built.");
            _resourceRegistry._cellSize = cellSize;
            return this;
        }

        /// <summary> Returns the fully initialized <see cref="FlowControlGodotClient.ResourceRegistry.ResourceRegistry"/> instance. </summary>
        public ResourceRegistry Build()
        {
            Debug.Assert(_resourceRegistry != null, "Resource registry is already built.");
            ResourceRegistry result = _resourceRegistry;
            _resourceRegistry = null;
            return result;
        }
    }
}
