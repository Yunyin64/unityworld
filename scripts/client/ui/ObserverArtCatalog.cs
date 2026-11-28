using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.UI;

/// <summary>客户端图片映射；只按定义或元素查图，不持有战斗对象。</summary>
public sealed class ObserverArtCatalog
{
    private readonly Dictionary<string, (string Path, Rect2? Crop)> _cards = new();
    private readonly Dictionary<string, Texture2D> _mana = new();
    private readonly Dictionary<string, string> _casts = new();
    private readonly HashSet<string> _missing = new();
    private readonly HashSet<string> _transposed = new();

    /// <summary>加载客户端映射，错误只影响图片，不影响模拟。</summary>
    public ObserverArtCatalog()
    {
        Load("res://assets/presentation/card_visuals.json", "cards",
            (id, address, crop) => _cards.Add(id, (address, crop)), _transposed, _casts);
        Load("res://assets/presentation/mana_visuals.json", "elements", (id, address, crop) =>
        {
            if (!ResourceLoader.Exists(address)) return;
            var texture = GD.Load<Texture2D>(address);
            if (texture != null)
                _mana[id] = crop is { } rect ? new AtlasTexture { Atlas = texture, Region = rect } : texture;
        });
    }

    internal static string CardName(string? name, string? defineId) =>
        !string.IsNullOrWhiteSpace(name) ? name : !string.IsNullOrWhiteSpace(defineId) ? defineId : "卡牌名称缺失";

    internal static string ContestName(string kind) => kind switch
    {
        "Zhan" => "斩击", "Da" => "打击", "Ci" => "刺击", "SheJi" => "射击",
        "Shield" => "护盾", "Block" => "格挡", "Dodge" => "闪避", _ => kind
    };

    /// <summary>返回卡图，缺图返回空并保留文字回退。</summary>
    public Texture2D? Card(string id)
    {
        if (_cards.TryGetValue(id, out var card) && GameBootstrap.Assets.Texture(card.Path, card.Crop) is { } texture)
            return texture;
        if (_missing.Add("card:" + id)) GD.Print($"[ObserverArt] 缺图，使用名称回退：card/{id} ({card.Path})");
        return null;
    }

    /// <summary>映射指定的显示转置仅影响绘制，不改原图。</summary>
    public bool CardTranspose(string id) => _transposed.Contains(id);

    /// <summary>返回灵元图标，未知元素返回空。</summary>
    public Texture2D? Mana(string id) => Find(_mana, "mana", id);

    // 每张卡单独选择施法表现。已有拼点和被动触发沿用自己的演出。
    internal string? CastElement(CombatPresentationEvent fact) =>
        fact is { ContestId: null, Applied.IsPassive: false, Card: { } card } ? _casts.GetValueOrDefault(card.DefineId) : null;

    /// <summary>元素形色提示，名称仍由 UI 显示。</summary>
    public static Color ElementColor(string element) => element switch
    {
        "Jin" => new Color("ecd486"), "Mu" => new Color("83cf9a"),
        "Shui" => new Color("85cce9"), "Huo" => new Color("f19872"),
        "Tu" => new Color("c8aa75"), _ => new Color("d4dbdf")
    };

    /// <summary>费用键 None 在费用栏中显示为通用。</summary>
    public static string CostName(string key) => key == "None" ? "通用" : ElementName(key);

    /// <summary>元素标签，无映射时保留原 ID。</summary>
    public static string ElementName(string element) => element switch
    {
        "Jin" => "金", "Mu" => "木", "Shui" => "水", "Huo" => "火", "Tu" => "土", _ => element
    };

    private Texture2D? Find(Dictionary<string, Texture2D> map, string kind, string id)
    {
        if (map.TryGetValue(id, out var texture)) return texture;
        if (_missing.Add(kind + ":" + id)) GD.Print($"[ObserverArt] 缺图，使用名称回退：{kind}/{id}");
        return null;
    }

    private static void Load(string path, string key, Action<string, string, Rect2?> add, HashSet<string>? transposed = null,
        Dictionary<string, string>? casts = null)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            GD.Print($"[ObserverArt] 映射尚不可用：{path}");
            return;
        }
        try
        {
            using var doc = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(path));
            foreach (var property in doc.RootElement.GetProperty(key).EnumerateObject())
            {
                var item = property.Value;
                if (casts != null && item.TryGetProperty("castElement", out var cast) && cast.ValueKind == JsonValueKind.String &&
                    cast.GetString() is { Length: > 0 } element)
                    casts[property.Name] = element;
                if (!item.TryGetProperty("texture", out var texturePath) || texturePath.ValueKind != JsonValueKind.String) continue;
                var resourcePath = texturePath.GetString()!;
                Rect2? crop = null;
                if (item.TryGetProperty("region", out var region) && region.ValueKind == JsonValueKind.Array && region.GetArrayLength() == 4)
                    crop = new Rect2(region[0].GetSingle(), region[1].GetSingle(), region[2].GetSingle(), region[3].GetSingle());
                add(property.Name, resourcePath, crop);
                if (item.TryGetProperty("transpose", out var flip) && flip.ValueKind == JsonValueKind.True) transposed?.Add(property.Name);
            }
        }
        catch (Exception exception) { GD.PushWarning($"[ObserverArt] {path}: {exception.Message}"); }
    }
}
