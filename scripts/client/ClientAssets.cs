using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using Xianxia.Client.Exploration;
using FileAccess = Godot.FileAccess;

namespace Xianxia.Client;

/// <summary>把 Godot 随包资源与文件系统内容交给 Core，并共享客户端解码后的纹理。</summary>
internal sealed class ClientAssets
{
    private const string PackedSpine = "res://assets/art/battle/spine/";
    private const string PackedTerrain = "res://assets/art/exploration/terrain/";
    private const string ModArgument = "--asset-mod=";
    private readonly Dictionary<AssetMgr.Asset, Texture2D?> textures = new();
    private readonly Dictionary<(Texture2D Texture, Rect2 Region), AtlasTexture> crops = new();
    private readonly List<NpcAppearanceCatalog.DataSource> enabledMods = new();
    private readonly Dictionary<string, NpcAppearanceCatalog> wardrobes = new(StringComparer.OrdinalIgnoreCase);
    private string installationRoot = "";
    private TerrainArt? terrain;

    internal AssetMgr Catalog { get; } = new();
    internal int Generation { get; private set; }
    internal TerrainArt Terrain => terrain ??= new TerrainArt(this);

    // 管理器保留原始字节。Godot 在同一次挂载中只解码一次，每个裁切也共享一份。
    internal void Preload(string installRoot, IEnumerable<string> arguments)
    {
        installationRoot = installRoot;
        var sources = new List<AssetMgr.Source>
        {
            new("game", () => PackedFiles(PackedSpine, "spine/").Concat(PackedFiles(PackedTerrain, "terrain/")), ReadPacked),
            AssetMgr.FromDirectory("local", Path.Combine(installRoot, "src"))
        };
        enabledMods.Clear();

        foreach (string argument in arguments.Where(arg => arg.StartsWith(ModArgument, StringComparison.Ordinal)))
        {
            string value = argument[ModArgument.Length..];
            int split = value.IndexOf('=');
            if (split <= 0 || split == value.Length - 1)
            {
                GD.PushWarning($"资源包参数应为 {ModArgument}<ID>=<目录>：{argument}");
                continue;
            }
            string id = value[..split];
            if (sources.Any(source => source.Id == id))
            {
                GD.PushWarning($"资源包 ID 重复：{id}");
                continue;
            }
            string root = Path.GetFullPath(value[(split + 1)..]);
            sources.Add(AssetMgr.FromDirectory(id, Path.Combine(root, "src")));
            enabledMods.Add(new(id, root));
        }

        Catalog.Preload(sources, [".png", ".json", ".atlas"]);
        Generation++;
        textures.Clear();
        crops.Clear();
        wardrobes.Clear();
        terrain = null;
        foreach (string message in Catalog.Diagnostics)
            GD.Print($"[AssetMgr] {message}");
        foreach (var asset in Catalog.Assets.Where(asset => asset.Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            Texture(asset.Path);
    }

    // 校验的是外观候选实际依赖的图片与本次启用包。卡图仍可单独替换。
    internal string AppearanceFingerprint(string dataRoot)
    {
        var wardrobe = Appearance(dataRoot);
        var images = wardrobe.Parts("male").Concat(wardrobe.Parts("female"))
            .SelectMany(part => part.Attachments).Select(part => AssetMgr.NormalizeAddress(part.Image))
            .Append("spine/duel_parts.png").ToHashSet(StringComparer.Ordinal);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Add(string value) => hash.AppendData(Encoding.UTF8.GetBytes(value + "\n"));
        void Read(string root, string folder, string extension, Func<string, bool>? include = null)
        {
            var disk = AssetMgr.FromDirectory(folder, Path.Combine(root, folder));
            foreach (string path in disk.List().OrderBy(p => p, StringComparer.Ordinal))
            {
                string? address = AssetMgr.NormalizeAddress(path);
                if (address == null || !address.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                    include?.Invoke(address) == false) continue;
                Add(folder + "/" + address);
                hash.AppendData(SHA256.HashData(disk.Read(path)));
            }
        }
        Add("local");
        Read(dataRoot, "Data/Appearance", ".json");
        Read(installationRoot, "src", ".png", images.Contains);
        foreach (var mod in enabledMods)
        {
            Add(mod.Id);
            Read(mod.Root, "Data/Appearance", ".json");
            Read(mod.Root, "src", ".png", images.Contains);
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    internal NpcAppearanceCatalog Appearance(string dataRoot)
    {
        if (wardrobes.TryGetValue(dataRoot, out var wardrobe)) return wardrobe;
        wardrobe = NpcAppearanceCatalog.Load(Catalog, dataRoot, enabledMods, path => Texture(path) != null);
        foreach (string diagnostic in wardrobe.Diagnostics)
            GD.PushWarning($"[NpcAppearance] {diagnostic}");
        wardrobes.Add(dataRoot, wardrobe);
        return wardrobe;
    }

    internal Texture2D? Texture(string path, Rect2? region = null, Vector2? minimumSize = null)
    {
        // 若覆盖包的图片无法解码，继续查前一来源。
        foreach (var asset in Catalog.Find(path))
        {
            if (!textures.TryGetValue(asset, out var texture))
            {
                using var image = new Image();
                if (image.LoadPngFromBuffer(asset.Bytes.ToArray()) == Error.Ok)
                {
                    // Battle actors use a small on-screen scale, so downsample the painted linework cleanly.
                    if (asset.Path == "spine/duel_parts.png") image.GenerateMipmaps();
                    texture = ImageTexture.CreateFromImage(image);
                }
                else
                    GD.PushWarning($"[AssetMgr] [{asset.SourceId}] {asset.Path} 无法解码为 PNG。");
                textures.Add(asset, texture);
            }
            if (texture == null) continue;
            if (minimumSize is { } required && (texture.GetWidth() < required.X || texture.GetHeight() < required.Y))
            {
                GD.PushWarning($"[AssetMgr] [{asset.SourceId}] {asset.Path} 图集尺寸不足，需要至少 {required}。");
                continue;
            }
            if (region is not { } rect) return texture;
            var key = (texture, rect);
            if (!crops.TryGetValue(key, out var crop))
                crops.Add(key, crop = new AtlasTexture { Atlas = texture, Region = rect });
            return crop;
        }
        return null;
    }

    internal void End()
    {
        textures.Clear();
        crops.Clear();
        wardrobes.Clear();
        terrain = null;
        enabledMods.Clear();
        Catalog.End();
    }

    private static IEnumerable<string> PackedFiles(string folder, string prefix) => DirAccess.GetFilesAt(folder)
        .Concat(ResourceLoader.ListDirectory(folder))
        .Where(file => !file.EndsWith("/", StringComparison.Ordinal))
        .Distinct(StringComparer.Ordinal).Select(file => prefix + file);

    private static byte[] ReadPacked(string path)
    {
        int separator = path.IndexOf('/');
        string folder = path.StartsWith("terrain/", StringComparison.Ordinal) ? PackedTerrain : PackedSpine;
        string resource = folder + path[(separator + 1)..];
        if (FileAccess.FileExists(resource)) return FileAccess.GetFileAsBytes(resource);
        if (resource.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
            ResourceLoader.Exists(resource) && GD.Load<Texture2D>(resource) is { } texture)
        {
            using var image = texture.GetImage();
            if (image != null && !image.IsEmpty()) return image.SavePngToBuffer();
        }
        throw new FileNotFoundException("随包资源不可读。", resource);
    }
}
