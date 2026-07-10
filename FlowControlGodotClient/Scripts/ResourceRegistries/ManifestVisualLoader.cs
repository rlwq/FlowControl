using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FlowControlBusiness.Content;
using FlowControlModel.World;
using Godot;

namespace FlowControlGodotClient.ResourceRegistries;

/// <summary>
/// Builds the <see cref="ResourceRegistry"/> from the <c>Visuals/</c> tree, which mirrors
/// the <c>Content/</c> structure (one JSON file per kind, named by the kind): textures for
/// machines, items and entities, atlas coordinates for grounds, global visual settings.
/// </summary>
/// <remarks>
/// Fails fast at startup, in both directions: every core kind must have its visual file
/// (items may fall back to the same-kind machine texture), and every visual file must
/// belong to an existing kind — an orphan visual means a typo or a broken mod.
/// </remarks>
public static class ManifestVisualLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class TextureEntry { public string? Texture { get; set; } }
    private sealed class GroundEntry { public int[]? Atlas { get; set; } }
    private sealed class SettingsEntry { public int CellSize { get; set; } = 32; }

    /// <summary> Builds the visual registry for all kinds known to the content loader. </summary>
    public static ResourceRegistry Build(WorldGrid grid, ContentLoader content, string visualsDir)
    {
        var settings = ReadSettings($"{visualsDir}/settings.json");
        var builder = new ResourceRegistry.ResourceRegistryBuilder(grid)
            .SetCellSize(settings.CellSize);

        // Machines first: items without their own visual fall back to the machine texture
        var machineTextures = new Dictionary<string, Texture2D>();
        foreach (var (kind, json) in ReadKindFiles($"{visualsDir}/machines", content.MachineKinds))
        {
            var texture = LoadTexture(Parse<TextureEntry>(json, kind).Texture, "machine", kind);
            machineTextures[kind] = texture;
            builder.RegisterMachineTexture(kind, texture);
        }
        foreach (var kind in content.MachineKinds)
            if (!machineTextures.ContainsKey(kind))
                throw new ContentException($"Machine '{kind}' has no visual file (Visuals/machines/{kind}.json).");

        foreach (var (kind, json) in ReadKindFiles($"{visualsDir}/grounds", content.GroundKinds))
        {
            var atlas = Parse<GroundEntry>(json, kind).Atlas;
            if (atlas is not { Length: 2 })
                throw new ContentException($"Ground visual '{kind}' needs 'atlas' coordinates [x, y].");
            builder.RegisterGroundTile(kind, new Vector2I(atlas[0], atlas[1]));
        }

        var itemVisuals = new Dictionary<string, string>();
        foreach (var (kind, json) in ReadKindFiles($"{visualsDir}/items", content.ItemKinds))
            itemVisuals[kind] = json;
        foreach (var kind in content.ItemKinds)
        {
            var texture = itemVisuals.TryGetValue(kind, out var json)
                ? LoadTexture(Parse<TextureEntry>(json, kind).Texture, "item", kind)
                : machineTextures.GetValueOrDefault(kind)
                  ?? throw new ContentException(
                      $"Item '{kind}' has no visual file and no machine of that kind to fall back to.");
            builder.RegisterItemTexture(kind, texture);
        }

        // Entities: the built-in "player" needs a visual too
        var entityKinds = new HashSet<string>(content.EntityKinds) { "player" };
        var seenEntities = new HashSet<string>();
        foreach (var (kind, json) in ReadKindFiles($"{visualsDir}/entities", entityKinds))
        {
            seenEntities.Add(kind);
            builder.RegisterEntityTexture(
                kind, LoadTexture(Parse<TextureEntry>(json, kind).Texture, "entity", kind));
        }
        foreach (var kind in entityKinds)
            if (!seenEntities.Contains(kind))
                throw new ContentException($"Entity '{kind}' has no visual file (Visuals/entities/{kind}.json).");

        return builder.Build();
    }

    /// <summary>
    /// Reads all <c>*.json</c> files of one visuals directory, keyed by the kind
    /// (the file name). A file whose kind is unknown to the core is an error.
    /// </summary>
    private static IEnumerable<(string Kind, string Json)> ReadKindFiles(
        string dirPath, IReadOnlyCollection<string> knownKinds)
    {
        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
            yield break; // an empty category is fine as long as its kinds are covered elsewhere

        foreach (var file in dir.GetFiles())
        {
            if (!file.EndsWith(".json"))
                continue;
            var kind = file[..^".json".Length];
            if (!knownKinds.Contains(kind))
                throw new ContentException(
                    $"Orphan visual '{dirPath}/{file}': no such kind is defined in Content/.");
            yield return (kind, FileAccess.GetFileAsString($"{dirPath}/{file}"));
        }
    }

    private static TEntry Parse<TEntry>(string json, string kind) where TEntry : new()
    {
        try
        {
            return JsonSerializer.Deserialize<TEntry>(json, Options) ?? new TEntry();
        }
        catch (JsonException e)
        {
            throw new ContentException($"Visual '{kind}': malformed JSON — {e.Message}");
        }
    }

    private static SettingsEntry ReadSettings(string path) =>
        FileAccess.FileExists(path)
            ? Parse<SettingsEntry>(FileAccess.GetFileAsString(path), "settings")
            : new SettingsEntry();

    /// <summary> Loads a texture, failing with a content-friendly message. </summary>
    private static Texture2D LoadTexture(string? path, string what, string kind)
    {
        if (path == null)
            throw new ContentException($"The {what} visual '{kind}' has no 'texture'.");

        var texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        return texture
               ?? throw new ContentException(
                   $"The {what} '{kind}' points to a missing texture: '{path}'.");
    }
}
