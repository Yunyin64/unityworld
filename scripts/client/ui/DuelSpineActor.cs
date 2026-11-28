using Godot;
using Spine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UnityWorld.Game.Data;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>Spine 计算动作，Godot 根据动作层级和素材深度绘制人物。</summary>
internal sealed class DuelSpineActor
{
    private sealed class TextureLoader : Spine.TextureLoader
    {
        public void Load(AtlasPage page, string path) => page.rendererObject = GameBootstrap.Assets.Texture("spine/" + Path.GetFileName(path))
            ?? throw new FileNotFoundException("找不到人物图集。", path);
        public void Unload(object texture) { }
    }

    private sealed record RigData(SkeletonData Skeleton, int[] SlotDepths, Dictionary<RegionAttachment, int> AttachmentDepths);
    private sealed record RigAssets(RigData Male, RigData Female);
    private sealed class ExternalRegion : AtlasRegion { internal int? Depth { get; init; } }
    private readonly record struct DrawLayer(Slot Slot, long Depth, int SpineOrder);
    private static RigAssets? cachedRigs;
    private static Image? atlasPixels;
    private static int cachedGeneration = -1;
    private static readonly Dictionary<string, string> SlotGroups = new(StringComparer.Ordinal)
    {
        ["hairRear"] = "hair", ["hairFront"] = "hair", ["face"] = "face", ["handBack"] = "face", ["handFront"] = "face",
        ["eyes"] = "eyes", ["brows"] = "brows", ["mouth"] = "mouth", ["accessory"] = "accessory", ["robe"] = "robe",
        ["robeTails"] = "robe", ["cape"] = "robe", ["armBack"] = "robe", ["armFront"] = "robe",
        ["elbowBack"] = "robe", ["elbowFront"] = "robe", ["shinBack"] = "boots", ["shinFront"] = "boots", ["pantsBack"] = "pants",
        ["pantsFront"] = "pants", ["bootBack"] = "boots", ["bootFront"] = "boots", ["weapon"] = "weapon"
    };

    private readonly Skeleton _skeleton;
    private readonly RigData rigData;
    private readonly List<DrawLayer> drawLayers = new();
    private readonly float[] _vertices = new float[8];
    private readonly Vector2[] _quad = new Vector2[4];
    private readonly Vector2[] _uv = new Vector2[4];
    private readonly Color[] _color = new Color[1];
    private readonly Dictionary<string, Texture2D> _dyed = new(StringComparer.Ordinal);
    private BattleActorLook? _look;
    private NpcAppearanceCatalog? wardrobe;

    private DuelSpineActor(RigData data)
    {
        rigData = data;
        _skeleton = new(data.Skeleton);
    }
    internal string Rig { get; private init; } = "";
    internal int Generation { get; private init; }
    internal string? SlotArtwork(string slot) => (_skeleton.FindSlot(slot)?.Attachment as RegionAttachment)?.Path;
    internal Vector2? JointPosition(string bone) => _skeleton.FindBone(bone) is { } joint ? new(joint.WorldX, joint.WorldY) : null;

    internal static string ResolveRig(string rig) => rig == "female" ? "female" : "male";

    internal static ClientAssetPool.Lease<DuelSpineActor>? Rent(string rig)
    {
        int generation = GameBootstrap.Assets.Generation;
        if (cachedGeneration != generation)
        {
            GameBootstrap.AssetInstances.Drop<DuelSpineActor>(RigPath("male"));
            GameBootstrap.AssetInstances.Drop<DuelSpineActor>(RigPath("female"));
            cachedRigs = LoadAssets();
            atlasPixels?.Dispose();
            atlasPixels = null;
            cachedGeneration = generation;
        }
        rig = ResolveRig(rig);
        return GameBootstrap.AssetInstances.Rent(RigPath(rig), () => Create(rig), static actor => actor.ResetForPool());
    }

    private static string RigPath(string rig) => "spine/duel_" + rig + ".json";

    private static DuelSpineActor? Create(string rig)
    {
        var data = cachedRigs;
        if (data == null) return null;
        return new(rig == "female" ? data.Female : data.Male) { Rig = rig, Generation = cachedGeneration };
    }

    private static RigAssets? LoadAssets()
    {
        try
        {
            var atlas = new Atlas(new StringReader(PackedText("spine/duel_parts.atlas")), "", new TextureLoader());
            var reader = new SkeletonJson(atlas);
            RigData Read(string rig)
            {
                string text = PackedText(RigPath(rig));
                var skeleton = reader.ReadSkeletonData(new StringReader(text));
                if (skeleton.FindAnimation("Ready") == null) throw new InvalidDataException($"骨架 {rig} 缺少待机动画。");
                using var json = JsonDocument.Parse(text);
                var slots = json.RootElement.GetProperty("slots").EnumerateArray();
                int[] depths = slots.Select((slot, index) => slot.TryGetProperty("depth", out var depth) ? depth.GetInt32() : index * 10).ToArray();
                var overrides = new Dictionary<RegionAttachment, int>();
                foreach (var entry in json.RootElement.GetProperty("skins").EnumerateArray())
                {
                    var skin = skeleton.FindSkin(entry.GetProperty("name").GetString());
                    if (skin == null || !entry.TryGetProperty("attachments", out var attachments)) continue;
                    foreach (var entrySlot in attachments.EnumerateObject())
                    {
                        var slot = skeleton.FindSlot(entrySlot.Name);
                        if (slot == null) continue;
                        foreach (var part in entrySlot.Value.EnumerateObject())
                            if (part.Value.TryGetProperty("depth", out var depth) &&
                                skin.GetAttachment(slot.Index, part.Name) is RegionAttachment attachment)
                                overrides.Add(attachment, depth.GetInt32());
                    }
                }
                return new(skeleton, depths, overrides);
            }
            return new(Read("male"), Read("female"));
        }
        catch (Exception error)
        {
            GD.PushWarning("Spine 人物素材无法读取，改用场景原立绘。" + error.Message);
            return null;
        }
    }

    private static string PackedText(string path) => GameBootstrap.Assets.Catalog.Find(path)
        .FirstOrDefault(asset => asset.SourceId == "game") is { } asset
            ? Encoding.UTF8.GetString(asset.Bytes.Span) : throw new FileNotFoundException("缺少内置 Spine 数据。", path);

    private void ResetForPool()
    {
        _look = null;
        wardrobe = null;
        _dyed.Clear();
        drawLayers.Clear();
        _skeleton.Skin = null;
        _skeleton.SetToSetupPose();
    }

    internal void Equip(BattleActorLook look, NpcAppearanceCatalog? catalog = null)
    {
        catalog ??= DuelWardrobe.Catalog;
        if (ReferenceEquals(wardrobe, catalog) && _look != null && _look.Parts.Count == look.Parts.Count &&
            _look.Parts.All(pair => look.Parts.GetValueOrDefault(pair.Key) == pair.Value)) return;
        wardrobe = catalog;
        _look = look.Copy();
        _dyed.Clear();
        var outfit = new Spine.Skin("outfit");
        if (_skeleton.Data.DefaultSkin is { } body) outfit.AddSkin(body);
        var entries = new List<Spine.Skin.SkinEntry>();
        foreach (var (slot, selected) in _look.Parts)
        {
            if (selected == null || catalog.Find(Rig, selected.Id) is not { } part ||
                part.Slot != slot || _skeleton.FindSlot(slot) is not { } target) continue;
            int index = target.Data.Index;
            if (_skeleton.Data.FindSkin(part.Skin) is { } skin)
            {
                // 整组旧皮肤可以有多个槽，逐槽只复制命中槽的动画附件。
                entries.Clear();
                skin.GetAttachments(index, entries);
                foreach (var entry in entries) outfit.SetAttachment(index, entry.Name, entry.Attachment);
            }
            foreach (var attachment in part.Attachments)
            {
                if (GameBootstrap.Assets.Texture(attachment.Image) is not { } texture) continue;
                var region = new ExternalRegion
                {
                    page = new AtlasPage { rendererObject = texture },
                    name = attachment.Image, u2 = 1, v2 = 1, Depth = attachment.Depth,
                    width = texture.GetWidth(), height = texture.GetHeight(),
                    originalWidth = texture.GetWidth(), originalHeight = texture.GetHeight()
                };
                var visual = new RegionAttachment(attachment.Key)
                {
                    Region = region, Path = attachment.Image, X = attachment.X, Y = attachment.Y, Rotation = attachment.Rotation,
                    Width = attachment.Width, Height = attachment.Height
                };
                visual.UpdateRegion();
                outfit.SetAttachment(index, attachment.Key, visual);
            }
        }
        _skeleton.SetSkin(outfit);
    }

    internal void Sample(string pose, float seconds)
    {
        bool ready = pose == "Ready";
        string clip = ready && _look?.Parts.GetValueOrDefault("stance")?.Id is { } stance
            ? stance[(stance.LastIndexOf('/') + 1)..] : pose;
        var animation = _skeleton.Data.FindAnimation(clip) ?? _skeleton.Data.FindAnimation("Ready");
        _skeleton.SetToSetupPose();
        animation.Apply(_skeleton, -1, seconds, ready, null, 1, MixBlend.Setup, MixDirection.In);
        _skeleton.UpdateWorldTransform(Skeleton.Physics.None);
        ResolveDrawLayers();
    }

    private void ResolveDrawLayers()
    {
        drawLayers.Clear();
        var ordered = _skeleton.DrawOrder;
        for (int index = 0; index < ordered.Count; index++)
        {
            var slot = ordered.Items[index];
            if (slot.Attachment is not RegionAttachment part) continue;
            int setupDepth = rigData.SlotDepths[slot.Data.Index];
            int partDepth = part.Region is ExternalRegion { Depth: int externalDepth }
                ? externalDepth : rigData.AttachmentDepths.GetValueOrDefault(part, setupDepth);

            // Spine moves the whole slot when a pose changes its order. A PNG's depth is relative to that new rank.
            long depth = (long)rigData.SlotDepths[index] + partDepth - setupDepth;
            drawLayers.Add(new(slot, depth, index));
        }
        drawLayers.Sort(static (left, right) => left.Depth == right.Depth
            ? left.SpineOrder.CompareTo(right.SpineOrder) : left.Depth.CompareTo(right.Depth));
    }

    internal int LayerOrder(string slot) => drawLayers.FindIndex(layer => layer.Slot.Data.Name == slot);

    internal sealed record AttachmentGeometry(string Slot, string Image, long Depth, float[] Vertices);

    internal AttachmentGeometry[] InspectAttachments() => drawLayers.Select(layer =>
    {
        var part = (RegionAttachment)layer.Slot.Attachment;
        var vertices = new float[8];
        part.ComputeWorldVertices(layer.Slot, vertices, 0);
        return new AttachmentGeometry(layer.Slot.Data.Name, part.Path, layer.Depth, vertices);
    }).ToArray();

    internal void Draw(CanvasItem canvas, float height, bool mirror, Color tint)
    {
        // The authored rigs use a common 234-unit standing scale and a floor origin.
        float scale = height / 234;
        foreach (var layer in drawLayers)
        {
            var slot = layer.Slot;
            if (slot.Attachment is not RegionAttachment part || part.Region is not AtlasRegion region ||
                region.page.rendererObject is not Texture2D texture) continue;
            part.ComputeWorldVertices(slot, _vertices, 0);
            bool external = region is ExternalRegion;
            var dye = external ? null : DyeFor(slot.Data.Name, region);
            for (int i = 0; i < 4; i++)
            {
                _quad[i] = new(_vertices[i * 2] * scale * (mirror ? -1 : 1), -_vertices[i * 2 + 1] * scale);
                var uv = new Vector2(part.UVs[i * 2], part.UVs[i * 2 + 1]);
                _uv[i] = dye == null ? uv : new((uv.X - region.u) / (region.u2 - region.u), (uv.Y - region.v) / (region.v2 - region.v));
            }
            _color[0] = tint * new Color(slot.R * part.R, slot.G * part.G, slot.B * part.B, slot.A * part.A);
            if (external && _look?.Parts.GetValueOrDefault(slot.Data.Name)?.Dye is { } hex && Color.HtmlIsValid(hex))
                _color[0] *= Color.FromHtml(hex);
            canvas.DrawPolygon(_quad, _color, _uv, dye ?? texture);
        }
    }

    private Texture2D? DyeFor(string slot, AtlasRegion region)
    {
        if (!SlotGroups.TryGetValue(slot, out string? group) || _look?.Parts.GetValueOrDefault(slot)?.Dye is not { Length: > 0 } hex ||
            !Color.HtmlIsValid(hex)) return null;
        string key = slot + "/" + region.name;
        if (_dyed.TryGetValue(key, out var dyed)) return dyed;
        var color = Color.FromHtml(hex);
        atlasPixels ??= GameBootstrap.Assets.Texture("spine/duel_parts.png")?.GetImage();
        if (atlasPixels == null || atlasPixels.IsEmpty()) return null;
        using var regionImage = atlasPixels.GetRegion(new Rect2I(region.x, region.y, region.width, region.height));
        regionImage.Convert(Image.Format.Rgba8);
        var pixels = regionImage.GetData();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0) continue;
            var original = Color.Color8(pixels[i], pixels[i + 1], pixels[i + 2]);
            if (!DyePixel(group, region.name, original)) continue;
            float shade = Math.Min(1.15f, .24f + original.V * 1.04f);
            var shaded = new Color(color.R * shade, color.G * shade, color.B * shade);
            var result = original.Lerp(shaded, Mathf.SmoothStep(.10f, .35f, original.V) * .9f);
            pixels[i] = (byte)Math.Clamp(result.R8, 0, 255);
            pixels[i + 1] = (byte)Math.Clamp(result.G8, 0, 255);
            pixels[i + 2] = (byte)Math.Clamp(result.B8, 0, 255);
        }
        using var painted = Image.CreateFromData(regionImage.GetWidth(), regionImage.GetHeight(), false, Image.Format.Rgba8, pixels);
        painted.GenerateMipmaps();
        dyed = ImageTexture.CreateFromImage(painted);
        _dyed.Add(key, dyed);
        return dyed;
    }

    private static bool DyePixel(string group, string part, Color pixel)
    {
        float hue = pixel.H, saturation = pixel.S;
        bool gold = hue is > .06f and < .16f && saturation > .25f;
        return group switch
        {
            "eyes" => saturation > .17f && pixel.V > .18f || pixel.V is > .25f and < .68f,
            "face" => hue is > .01f and < .16f && saturation > .16f && !gold || pixel.V > .7f && hue < .13f,
            "robe" when part.StartsWith("ref_male_", StringComparison.Ordinal) || part.EndsWith("jade", StringComparison.Ordinal) =>
                hue is > .37f and < .67f && saturation > .08f,
            "robe" when part.StartsWith("ref_female_", StringComparison.Ordinal) || part.EndsWith("cinnabar", StringComparison.Ordinal) =>
                (hue < .09f || hue > .9f) && saturation > .26f,
            "robe" => hue is > .53f and < .96f && saturation > .08f,
            "accessory" or "weapon" => saturation > .15f,
            "mouth" => !pixel.IsEqualApprox(Colors.White) && (saturation > .21f || pixel.V < .36f),
            _ => !gold && pixel.V > .12f
        };
    }
}
