using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Xianxia.Client.UI;

/// <summary>客户端图片映射；只按定义或元素查图，不持有战斗对象。</summary>
public sealed class ObserverArtCatalog
{
    private readonly Dictionary<string, Texture2D> _cards = new();
    private readonly Dictionary<string, Texture2D> _mana = new();
    private readonly HashSet<string> _missing = new();
    private readonly HashSet<string> _transposed = new();

    /// <summary>加载客户端映射，错误只影响图片，不影响模拟。</summary>
    public ObserverArtCatalog()
    {
        Load("res://assets/presentation/card_visuals.json", "cards", _cards, _transposed);
        Load("res://assets/presentation/mana_visuals.json", "elements", _mana, null);
    }

    /// <summary>返回卡图，缺图返回空并保留文字回退。</summary>
    public Texture2D? Card(string id) => Find(_cards, "card", id);

    /// <summary>映射指定的显示转置仅影响绘制，不改原图。</summary>
    public bool CardTranspose(string id) => _transposed.Contains(id);

    /// <summary>返回灵元图标，未知元素返回空。</summary>
    public Texture2D? Mana(string id) => Find(_mana, "mana", id);

    /// <summary>元素形色提示，名称仍由 UI 显示。</summary>
    public static Color ElementColor(string element) => element switch
    {
        "Jin" => new Color("ecd486"), "Mu" => new Color("83cf9a"),
        "Shui" => new Color("85cce9"), "Huo" => new Color("f19872"),
        "Tu" => new Color("c8aa75"), _ => new Color("d4dbdf")
    };

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

    private static void Load(string path, string key, Dictionary<string, Texture2D> target, HashSet<string>? transposed)
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
                if (!item.TryGetProperty("texture", out var texturePath) || texturePath.ValueKind != JsonValueKind.String) continue;
                var resourcePath = texturePath.GetString()!;
                if (!ResourceLoader.Exists(resourcePath)) continue;
                Texture2D texture = GD.Load<Texture2D>(resourcePath);
                if (item.TryGetProperty("region", out var region) && region.ValueKind == JsonValueKind.Array && region.GetArrayLength() == 4)
                    texture = new AtlasTexture { Atlas = texture, Region = new Rect2(region[0].GetSingle(), region[1].GetSingle(), region[2].GetSingle(), region[3].GetSingle()) };
                target[property.Name] = texture;
                if (item.TryGetProperty("transpose", out var flip) && flip.ValueKind == JsonValueKind.True) transposed?.Add(property.Name);
            }
            GD.Print($"[ObserverArt] {key}: {target.Count} 张图片已加载");
        }
        catch (Exception exception) { GD.PushWarning($"[ObserverArt] {path}: {exception.Message}"); }
    }
}
