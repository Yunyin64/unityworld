using Godot;
using Spine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xianxia.Client.Battle;
using FileAccess = Godot.FileAccess;

namespace Xianxia.Client.UI;

/// <summary>Spine 负责骨骼、皮肤和关键帧，Godot 按舞台原来的顺序绘制插槽。</summary>
internal sealed class DuelSpineActor
{
    private sealed class TextureLoader : Spine.TextureLoader
    {
        public void Load(AtlasPage page, string path) => page.rendererObject = GD.Load<Texture2D>(path.Replace('\\', '/'))
            ?? throw new FileNotFoundException("找不到人物图集。", path);
        public void Unload(object texture) { }
    }

    private sealed record RigAssets(SkeletonData Male, SkeletonData Female);
    private static readonly Lazy<RigAssets?> Assets = new(LoadAssets);
    private static readonly Lazy<Image> AtlasPixels = new(() => GD.Load<Texture2D>(DuelWardrobe.Directory + "duel_parts.png").GetImage());
    private static readonly Dictionary<string, string> SlotGroups = new(StringComparer.Ordinal)
    {
        ["hairRear"] = "hair", ["hairFront"] = "hair", ["face"] = "face", ["neck"] = "face", ["handBack"] = "face", ["handFront"] = "face",
        ["eyes"] = "eyes", ["brows"] = "brows", ["mouth"] = "mouth", ["accessory"] = "accessory", ["robe"] = "robe",
        ["robeTails"] = "robe", ["cape"] = "robe", ["armBack"] = "robe", ["armFront"] = "robe", ["pantsBack"] = "pants",
        ["pantsFront"] = "pants", ["bootBack"] = "boots", ["bootFront"] = "boots", ["weapon"] = "weapon"
    };

    private readonly Skeleton _skeleton;
    private readonly float[] _vertices = new float[8];
    private readonly Vector2[] _quad = new Vector2[4];
    private readonly Vector2[] _uv = new Vector2[4];
    private readonly Color[] _color = new Color[1];
    private readonly Dictionary<string, Texture2D> _dyed = new(StringComparer.Ordinal);
    private BattleActorLook? _look;

    private DuelSpineActor(SkeletonData data) => _skeleton = new(data);
    internal string Rig { get; private init; } = "";

    internal static string ResolveRig(string rig) => rig == "female" ? "female" : "male";

    internal static ClientAssetPool.Lease<DuelSpineActor>? Rent(string rig)
    {
        rig = ResolveRig(rig);
        return GameBootstrap.AssetInstances.Rent(RigPath(rig), () => Create(rig), static actor => actor.ResetForPool());
    }

    private static string RigPath(string rig) => DuelWardrobe.Directory + "duel_" + rig + ".json";

    private static DuelSpineActor? Create(string rig)
    {
        var data = Assets.Value;
        if (data == null) return null;
        return new(rig == "female" ? data.Female : data.Male) { Rig = rig };
    }

    private static RigAssets? LoadAssets()
    {
        try
        {
            var atlas = new Atlas(new StringReader(FileAccess.GetFileAsString(DuelWardrobe.Directory + "duel_parts.atlas")),
                DuelWardrobe.Directory, new TextureLoader());
            var reader = new SkeletonJson(atlas);
            SkeletonData Read(string rig)
            {
                var skeleton = reader.ReadSkeletonData(new StringReader(
                    FileAccess.GetFileAsString(RigPath(rig))));
                if (skeleton.FindAnimation("Ready") == null) throw new InvalidDataException($"骨架 {rig} 缺少待机动画。");
                return skeleton;
            }
            return new(Read("male"), Read("female"));
        }
        catch (Exception error)
        {
            GD.PushWarning("Spine 人物素材无法读取，改用场景原立绘。" + error.Message);
            return null;
        }
    }

    private void ResetForPool()
    {
        _look = null;
        _dyed.Clear();
        _skeleton.Skin = null;
        _skeleton.SetToSetupPose();
    }

    internal void Equip(BattleActorLook look)
    {
        if (_look != null && _look.Parts.Count == look.Parts.Count &&
            _look.Parts.All(pair => look.Parts.GetValueOrDefault(pair.Key) == pair.Value)) return;
        _look = look.Copy();
        _dyed.Clear();
        var outfit = new Spine.Skin("outfit");
        if (_skeleton.Data.DefaultSkin is { } body) outfit.AddSkin(body);
        foreach (var part in _look.Parts)
            if (part.Value != null && _skeleton.Data.FindSkin(part.Key + "/" + part.Value.Id) is { } skin) outfit.AddSkin(skin);
        _skeleton.SetSkin(outfit);
    }

    internal void Sample(string pose, float seconds)
    {
        bool ready = pose == "Ready";
        string clip = ready ? _look?.Parts.GetValueOrDefault("stance")?.Id ?? pose : pose;
        var animation = _skeleton.Data.FindAnimation(clip) ?? _skeleton.Data.FindAnimation("Ready");
        _skeleton.SetToSetupPose();
        animation.Apply(_skeleton, -1, seconds, ready, null, 1, MixBlend.Setup, MixDirection.In);
        _skeleton.UpdateWorldTransform(Skeleton.Physics.None);
    }

    internal void Draw(CanvasItem canvas, float height, bool mirror, Color tint)
    {
        // The authored rigs use a common 234-unit standing scale and a floor origin.
        float scale = height / 234;
        foreach (var slot in _skeleton.DrawOrder)
        {
            if (slot.Attachment is not RegionAttachment part || part.Region is not AtlasRegion region ||
                region.page.rendererObject is not Texture2D texture) continue;
            part.ComputeWorldVertices(slot, _vertices, 0);
            var dye = DyeFor(slot.Data.Name, region);
            for (int i = 0; i < 4; i++)
            {
                _quad[i] = new(_vertices[i * 2] * scale * (mirror ? -1 : 1), -_vertices[i * 2 + 1] * scale);
                var uv = new Vector2(part.UVs[i * 2], part.UVs[i * 2 + 1]);
                _uv[i] = dye == null ? uv : new((uv.X - region.u) / (region.u2 - region.u), (uv.Y - region.v) / (region.v2 - region.v));
            }
            _color[0] = tint * new Color(slot.R * part.R, slot.G * part.G, slot.B * part.B, slot.A * part.A);
            canvas.DrawPolygon(_quad, _color, _uv, dye ?? texture);
        }
    }

    private Texture2D? DyeFor(string slot, AtlasRegion region)
    {
        if (!SlotGroups.TryGetValue(slot, out string? group) || _look?.Parts.GetValueOrDefault(group)?.Dye is not { Length: > 0 } hex ||
            !Color.HtmlIsValid(hex)) return null;
        if (_dyed.TryGetValue(region.name, out var dyed)) return dyed;
        var color = Color.FromHtml(hex);
        var regionImage = AtlasPixels.Value.GetRegion(new Rect2I(region.x, region.y, region.width, region.height));
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
        dyed = ImageTexture.CreateFromImage(Image.CreateFromData(regionImage.GetWidth(), regionImage.GetHeight(), false, Image.Format.Rgba8, pixels));
        _dyed.Add(region.name, dyed);
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
