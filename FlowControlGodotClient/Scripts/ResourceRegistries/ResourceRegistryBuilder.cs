using System;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.ResourceRegistries;

public partial class ResourceRegistry
{
    /// <summary>
    /// Provides a fluent interface for constructing and initializing a <see cref="ResourceRegistry"/>.
    /// Misuse (duplicate registrations, use after <see cref="Build"/>) throws immediately.
    /// </summary>
    public class ResourceRegistryBuilder(WorldGrid grid)
    {
        private ResourceRegistry? _resourceRegistry = new(grid);

        /// <summary> The registry under construction. Throws if <see cref="Build"/> was already called. </summary>
        private ResourceRegistry Registry =>
            _resourceRegistry ?? throw new InvalidOperationException("Resource registry is already built.");

        /// <summary> Registers atlas coordinates for a terrain tile type. </summary>
        public ResourceRegistryBuilder RegisterGroundTile(string kind, Vector2I atlasCoords)
        {
            Registry._groundTilesAtlasCords.Add(kind, atlasCoords);
            return this;
        }

        /// <summary> Maps a machine type to its visual texture. </summary>
        public ResourceRegistryBuilder RegisterMachineTexture(string kind, Texture2D texture)
        {
            Registry._machineTextures.Add(kind, texture);
            return this;
        }

        /// <summary> Maps an entity type to its visual texture. </summary>
        public ResourceRegistryBuilder RegisterEntityTexture(string kind, Texture2D texture)
        {
            Registry._entityTextures.Add(kind, texture);
            return this;
        }

        /// <summary> Maps an item type to its visual texture. </summary>
        public ResourceRegistryBuilder RegisterItemTexture(string kind, Texture2D texture)
        {
            Registry._itemTextures.Add(kind, texture);
            return this;
        }

        /// <summary> Sets the visual scale of a single grid cell in pixels. </summary>
        public ResourceRegistryBuilder SetCellSize(int cellSize)
        {
            Registry._cellSize = cellSize;
            return this;
        }

        /// <summary> Returns the fully initialized <see cref="ResourceRegistry"/> instance. </summary>
        public ResourceRegistry Build()
        {
            ResourceRegistry result = Registry;
            _resourceRegistry = null;
            return result;
        }
    }
}
