using System.Text.Json;

namespace FlowControlBusiness.Content;

/// <summary>
/// Deserialized per-kind content files (<c>Content/&lt;category&gt;/&lt;kind&gt;.json</c>).
/// The kind name comes from the file name and is never repeated inside the file.
/// These entries carry <b>core data only</b> — everything visual (textures, atlas
/// coordinates, sounds) lives in the parallel <c>Visuals/</c> tree consumed by the client.
/// </summary>
public static class ContentEntries
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary> Parses one content file into an entry of the given shape. </summary>
    public static TEntry Parse<TEntry>(string json, string kind)
    {
        try
        {
            return JsonSerializer.Deserialize<TEntry>(json, Options)
                   ?? throw new ContentException($"'{kind}': the file is empty.");
        }
        catch (JsonException e)
        {
            throw new ContentException($"'{kind}': malformed JSON — {e.Message}");
        }
    }

    /// <summary> A ground kind: <c>Content/grounds/&lt;kind&gt;.json</c>. </summary>
    public sealed class Ground
    {
        /// <summary> Item kind spawned by tiles of this ground (ore deposits), if any. </summary>
        public string? SpawnsItem { get; set; }

        /// <summary> Ticks between two spawns of one tile (with <see cref="SpawnsItem"/>). </summary>
        public int SpawnPeriodTicks { get; set; }

        /// <summary> Whether entities can walk on (and machines stand on) this ground. </summary>
        public bool Passable { get; set; } = true;

        /// <summary> Movement speed multiplier for entities standing on this ground. </summary>
        public float SpeedModifier { get; set; } = 1f;
    }

    /// <summary> An item kind: <c>Content/items/&lt;kind&gt;.json</c>. </summary>
    public sealed class Item
    {
        /// <summary> How many items fit in one inventory slot. </summary>
        public int StackSize { get; set; } = 1;
    }

    /// <summary> A machine kind: <c>Content/machines/&lt;kind&gt;.json</c>. </summary>
    public sealed class Machine
    {
        /// <summary> Footprint <c>[width, height]</c> in cells (unrotated). </summary>
        public int[] Dimensions { get; set; } = [1, 1];

        /// <summary> Inventory slot counts <c>[input, blob, output]</c>. Omit for no inventory. </summary>
        public int[]? Inventory { get; set; }

        /// <summary> Name of the machine logic in the <see cref="LogicCatalog"/>. </summary>
        public string? Logic { get; set; }

        /// <summary> Extra parameters passed to the logic factory. </summary>
        public JsonElement? LogicParams { get; set; }

        /// <summary> Cell offsets the logic observes, relative to the unrotated footprint. </summary>
        public int[][]? ObserverOffsets { get; set; }

        /// <summary>
        /// Whether players may build this machine. Non-buildable machines (e.g. the Hub)
        /// need no item pair and are placed only by system commands.
        /// </summary>
        public bool PlayerBuildable { get; set; } = true;

        /// <summary> Whether the machine resists removal by players (e.g. the Hub). </summary>
        public bool Indestructible { get; set; }
    }

    /// <summary>
    /// An entity kind: <c>Content/entities/&lt;kind&gt;.json</c>
    /// (the player included — it is an ordinary entity without a logic).
    /// </summary>
    public sealed class Entity
    {
        /// <summary> Collision box <c>[width, height]</c> in cells. </summary>
        public float[]? BoxSize { get; set; }

        /// <summary> Inventory slot counts <c>[input, blob, output]</c>. Omit for no inventory. </summary>
        public int[]? Inventory { get; set; }

        /// <summary> Name of the entity logic in the <see cref="LogicCatalog"/>. </summary>
        public string? Logic { get; set; }

        /// <summary> Extra parameters passed to the logic factory. </summary>
        public JsonElement? LogicParams { get; set; }
    }
}

/// <summary> Raised when content files are malformed or inconsistent. </summary>
public class ContentException(string message) : System.Exception(message);
