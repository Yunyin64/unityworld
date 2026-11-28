using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

namespace UnityWorld.Game.Data
{
    /// <summary>按骨架和插槽登记可装配的组件。底图通过 Core 资源目录取得。</summary>
    public sealed class NpcAppearanceCatalog
    {
        /// <summary>外部图片的局部变换、动画附件键和可选绘制深度。深度越大越靠前。</summary>
        public sealed record Attachment(string Image, float X, float Y, float Width, float Height,
            float Rotation = 0, string Key = "base", int? Depth = null);

        /// <summary>一个指定骨架和插槽可消费的部件。</summary>
        public sealed record Part(string Slot, string Address, string Label, string SlotLabel, string SourceId,
            bool Dyeable, string Skin, IReadOnlyList<Attachment> Attachments);

        /// <summary>启用包的 Game 配置根，资源根由 AssetMgr 单独挂载。</summary>
        public sealed record DataSource(string Id, string Root);

        /// <summary>可定位到模板字段的外观校验结果。</summary>
        public sealed record Issue(string Slot, string Message, bool Error);

        private sealed record WardrobeGroup(string Id, string Label, bool Dyeable, WardrobeOption[] Options);
        private sealed record WardrobeOption(string Id, string Label);
        private sealed record Wardrobe(WardrobeGroup[] Groups);
        private sealed record ExternalPart(string Slot, string Address, string Label,
            Dictionary<string, Attachment[]> Rigs, bool Dyeable = true);
        private sealed record RigSlot(string Name, HashSet<string> AttachmentKeys, float OffsetX, float OffsetY);

        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        // A missing component can follow its neighbor's outfit. Explicit slots are resolved first.
        private static readonly Dictionary<string, string> OutfitNeighbor = new(StringComparer.Ordinal)
        {
            ["armFront"] = "robe", ["armBack"] = "robe", ["elbowFront"] = "armFront", ["elbowBack"] = "armBack",
            ["handFront"] = "elbowFront", ["handBack"] = "elbowBack",
            ["robeTails"] = "robe", ["cape"] = "robe", ["shinFront"] = "bootFront", ["shinBack"] = "bootBack"
        };
        private static readonly Lazy<NpcAppearanceCatalog> BuiltIn = new(() => new NpcAppearanceCatalog(null));
        private readonly Dictionary<string, Dictionary<string, Part>> byRig = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, RigSlot>> rigSlots = new(StringComparer.Ordinal);

        /// <summary>共享只含内置组件的目录，用于没有宿主资源的用例。</summary>
        public static NpcAppearanceCatalog Default => BuiltIn.Value;

        /// <summary>外部配置中无法装配的部件及原因。</summary>
        public IReadOnlyList<string> Diagnostics { get; private set; } = Array.Empty<string>();

        private NpcAppearanceCatalog(AssetMgr? assets)
        {
            using var wardrobe = ReadBuiltin("wardrobe.json", assets);
            var groups = wardrobe.RootElement.Deserialize<Wardrobe>(Json)!.Groups;
            foreach (string rig in new[] { "male", "female" })
            {
                var parts = byRig[rig] = new(StringComparer.Ordinal);
                using var skeleton = ReadBuiltin($"duel_{rig}.json", assets);
                rigSlots[rig] = ReadSlots(skeleton.RootElement);
                var skins = skeleton.RootElement.GetProperty("skins").EnumerateArray().ToDictionary(
                    node => node.GetProperty("name").GetString()!,
                    node => node.TryGetProperty("attachments", out var attachments)
                        ? attachments.EnumerateObject().Select(slot => slot.Name).ToArray() : Array.Empty<string>(),
                    StringComparer.Ordinal);

                // 旧换装选项可能覆盖多个槽。用实际骨架数据展开，槽间不再联动。
                foreach (var group in groups)
                {
                    string[] slots = group.Id == "stance" ? ["stance"] : group.Options
                        .SelectMany(option => skins.GetValueOrDefault(group.Id + "/" + option.Id) ?? Array.Empty<string>())
                        .Distinct(StringComparer.Ordinal).ToArray();
                    foreach (string slot in slots)
                    foreach (var option in group.Options)
                    {
                        string skin = group.Id + "/" + option.Id;
                        string address = slot switch
                        {
                            "hairFront" => "hair/front/" + option.Id,
                            "hairRear" => "hair/rear/" + option.Id,
                            _ => slot == group.Id ? skin : group.Id + "/" + slot + "/" + option.Id
                        };
                        string label = slot switch
                        {
                            "hairFront" => "前发", "hairRear" => "后发", "robeTails" => "衣摆", "cape" => "披风",
                            "armFront" => "近侧大臂袖", "armBack" => "远侧大臂袖",
                            "elbowFront" => "近侧小臂袖", "elbowBack" => "远侧小臂袖",
                            "handFront" => "近侧手", "handBack" => "远侧手",
                            "pantsFront" => "前腿裤", "pantsBack" => "后腿裤", "shinFront" => "近侧护腿", "shinBack" => "远侧护腿",
                            "bootFront" => "前脚鞋", "bootBack" => "后脚鞋", _ => group.Label
                        };
                        parts.Add(address, new(slot, address, option.Label, label, "game",
                            group.Dyeable && option.Id != "none", skin, Array.Empty<Attachment>()));
                    }
                }
            }
        }

        private static Dictionary<string, RigSlot> ReadSlots(JsonElement skeleton)
        {
            var slots = skeleton.GetProperty("slots").EnumerateArray().Select(slot =>
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                if (slot.TryGetProperty("attachment", out var setup) && setup.GetString() is { } key) keys.Add(key);
                return new RigSlot(slot.GetProperty("name").GetString()!, keys,
                    slot.TryGetProperty("attachmentOffsetX", out var x) ? x.GetSingle() : 0,
                    slot.TryGetProperty("attachmentOffsetY", out var y) ? y.GetSingle() : 0);
            }).ToDictionary(slot => slot.Name, StringComparer.OrdinalIgnoreCase);

            // 外部部件只能占用骨架安装位和已有动画实际切换的附件键。
            foreach (var clip in skeleton.GetProperty("animations").EnumerateObject())
            {
                if (!clip.Value.TryGetProperty("slots", out var animatedSlots)) continue;
                foreach (var slot in animatedSlots.EnumerateObject())
                {
                    if (!slots.TryGetValue(slot.Name, out var target) ||
                        !slot.Value.TryGetProperty("attachment", out var frames)) continue;
                    foreach (var frame in frames.EnumerateArray())
                        if (frame.TryGetProperty("name", out var attachment) && attachment.GetString() is { } key)
                            target.AttachmentKeys.Add(key);
                }
            }
            return slots;
        }

        /// <summary>加载当前包的 Data/Appearance 声明，按包顺序覆盖同址组件。</summary>
        public static NpcAppearanceCatalog Load(AssetMgr assets, string gameRoot, IEnumerable<DataSource>? mods = null,
            Func<string, bool>? canDecode = null)
        {
            var catalog = new NpcAppearanceCatalog(assets);
            var diagnostics = new List<string>();
            foreach (var source in new[] { new DataSource("local", gameRoot) }.Concat(mods ?? []))
            {
                string directory = Path.Combine(source.Root, "Data", "Appearance");
                if (!Directory.Exists(directory)) continue;
                var declared = new HashSet<(string Rig, string Address)>();
                foreach (string file in Directory.EnumerateFiles(directory, "*.json", new EnumerationOptions
                    { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }).OrderBy(file => file, StringComparer.Ordinal))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(File.ReadAllBytes(file));
                        ExternalPart?[] definitions = document.RootElement.ValueKind == JsonValueKind.Array
                            ? document.RootElement.Deserialize<ExternalPart[]>(Json)! : [document.RootElement.Deserialize<ExternalPart>(Json)];
                        foreach (var definition in definitions)
                        {
                            if (definition?.Rigs == null || AssetMgr.NormalizeAddress(definition.Address) is not { } address)
                            {
                                diagnostics.Add($"[{source.Id}] {file}：部件地址或骨架列表无效。");
                                continue;
                            }
                            foreach (var (rig, attachments) in definition.Rigs)
                            {
                                var parts = catalog.byRig.GetValueOrDefault(rig);
                                var target = definition.Slot == null ? null :
                                    catalog.rigSlots.GetValueOrDefault(rig)?.GetValueOrDefault(definition.Slot);
                                if (parts == null || target == null || !declared.Add((rig, address)) ||
                                    parts.TryGetValue(address, out var previous) && previous.Slot != target.Name ||
                                    attachments is not { Length: > 0 } || attachments.Any(attachment =>
                                        attachment == null || !target.AttachmentKeys.Contains(attachment.Key) ||
                                        !float.IsFinite(attachment.X) || !float.IsFinite(attachment.Y) || !float.IsFinite(attachment.Rotation) ||
                                        !float.IsFinite(attachment.Width) || attachment.Width <= 0 ||
                                        !float.IsFinite(attachment.Height) || attachment.Height <= 0) ||
                                    attachments.Select(attachment => attachment.Key).Distinct(StringComparer.Ordinal).Count() != attachments.Length)
                                {
                                    diagnostics.Add($"[{source.Id}] {file}：部件 {address} 无法装入 {rig}/{definition.Slot}，请检查骨架、插槽和附件声明。");
                                    continue;
                                }
                                var missing = attachments.FirstOrDefault(attachment =>
                                    !assets.Find(attachment.Image).Any(asset => IsPng(asset.Bytes.Span)) ||
                                    canDecode?.Invoke(attachment.Image) == false);
                                if (missing != null)
                                {
                                    diagnostics.Add($"[{source.Id}] {file}：部件 {address} 的图片 {missing.Image} 缺失或无法解码，跳过 {rig}/{target.Name}。");
                                    continue;
                                }
                                string label = parts.Values.FirstOrDefault(part => part.Slot == target.Name)?.SlotLabel ?? target.Name;
                                // Rebase authored positions for slots moved onto a new joint, so older Mod art stays aligned.
                                var fitted = attachments.Select(attachment => attachment with
                                {
                                    X = attachment.X + target.OffsetX,
                                    Y = attachment.Y + target.OffsetY
                                }).ToArray();
                                parts[address] = new(target.Name, address, definition.Label ?? address, label,
                                    source.Id, definition.Dyeable, "", Array.AsReadOnly(fitted));
                            }
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                    {
                        diagnostics.Add($"[{source.Id}] {file}：{ex.Message}");
                    }
                }
            }
            catalog.Diagnostics = diagnostics.AsReadOnly();
            return catalog;
        }

        /// <summary>返回指定骨架下可用部件，包含显示名称和来源。</summary>
        public IEnumerable<Part> Parts(string rig) => byRig.TryGetValue(rig, out var parts) ? parts.Values : Array.Empty<Part>();

        /// <summary>按骨架查找规范资源地址。</summary>
        public Part? Find(string rig, string address) => AssetMgr.NormalizeAddress(address) is { } key &&
            byRig.TryGetValue(rig, out var parts) ? parts.GetValueOrDefault(key) : null;

        /// <summary>读取旧部件 ID 所覆盖的可用插槽地址。</summary>
        public IEnumerable<Part> LegacyParts(string rig, string group, string choice)
        {
            if (group == "weapon" && choice == "flame") choice = "talisman";
            string skin = group + "/" + choice;
            return Default.Parts(rig).Where(part => part.Skin == skin);
        }

        /// <summary>已知骨架与目标模板对应的运行时骨架 ID。</summary>
        public static string RigFor(NpcTypes.NpcType type, NpcTypes.Gender gender, string template, int seed)
        {
            if (type != NpcTypes.NpcType.Human) return "";
            return gender switch
            {
                NpcTypes.Gender.Male => "male",
                NpcTypes.Gender.Female => "female",
                _ => ((string.IsNullOrEmpty(template) ? seed : StableSeed(template)) & 1) == 0 ? "male" : "female"
            };
        }

        /// <summary>用固定哈希派生模板预览种子。</summary>
        public static int StableSeed(string id)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char character in id) hash = (hash ^ character) * 16777619;
                return hash;
            }
        }

        /// <summary>优先装配显式选择，缺项使用独立随机流填充一次。</summary>
        public NpcAppearanceData Resolve(string rig, IReadOnlyDictionary<string, string>? configured, int seed, Action<Issue>? report = null)
        {
            var result = new NpcAppearanceData { Rig = rig };
            var slots = Parts(rig).GroupBy(part => part.Slot, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.OrderBy(part => part.Address, StringComparer.Ordinal).ToArray(),
                    StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in configured ?? new Dictionary<string, string>())
            {
                // Old looks can still carry the removed neck choice. The robe now supplies the throat artwork.
                if (key.Equals("neck", StringComparison.OrdinalIgnoreCase) && !slots.ContainsKey(key)) continue;
                if (!slots.TryGetValue(key, out var candidates) || !seen.Add(key) ||
                    !TryRead(value, out string? address, out string? dye))
                {
                    report?.Invoke(new(key, $"插槽重复、未知或地址/染色格式无效：{value}", true));
                    continue;
                }
                if (address == null) continue;
                var part = candidates.FirstOrDefault(part => part.Address == address);
                if (part == null)
                    report?.Invoke(new(key, $"部件缺失或与骨架不兼容：{address}", false));
                else if (dye != null && !part.Dyeable)
                    report?.Invoke(new(key, $"该部件不支持染色：{address}", true));
                else
                    result.Parts.Add(part.Slot, new(part.Address, part.SourceId, dye));
            }

            // 使用独立种子，新增外观槽不会消耗 NPC 生理或战斗的随机序列。
            var rng = new Rng(seed);
            foreach (var (slot, candidates) in slots.OrderBy(pair => OutfitOrder(pair.Key)).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (result.Parts.ContainsKey(slot)) continue;
                var part = candidates[rng.Range(0, candidates.Length)];
                if (OutfitNeighbor.TryGetValue(slot, out string? neighbor) &&
                    result.Parts.TryGetValue(neighbor, out var selected) &&
                    Find(rig, selected.Address) is { Skin.Length: > 0 } reference &&
                    candidates.FirstOrDefault(candidate => candidate.Skin == reference.Skin) is { } matching)
                    part = matching;
                result.Parts.Add(slot, new(part.Address, part.SourceId));
            }
            return result;
        }

        private static int OutfitOrder(string slot) => slot switch
        {
            "elbowFront" or "elbowBack" => 2,
            "handFront" or "handBack" => 3,
            _ => OutfitNeighbor.ContainsKey(slot) ? 1 : 0
        };

        private static bool TryRead(string value, out string? address, out string? dye)
        {
            address = dye = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            value = value.Trim();
            int suffix = value.IndexOf('[');
            if (suffix >= 0)
            {
                if (!value.EndsWith(']')) return false;
                string color = value[(suffix + 1)..^1];
                if (color.Length > 0)
                {
                    if (color.Length != 7 || color[0] != '#' || !color[1..].All(Uri.IsHexDigit)) return false;
                    dye = color.ToUpperInvariant();
                }
                value = value[..suffix];
            }
            return !value.Contains(']') && (address = AssetMgr.NormalizeAddress(value)) != null;
        }

        private static bool IsPng(ReadOnlySpan<byte> bytes) =>
            bytes.Length >= 33 && bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        private static JsonDocument ReadBuiltin(string name, AssetMgr? assets)
        {
            var packed = assets?.Find("spine/" + name).FirstOrDefault(asset => asset.SourceId == "game");
            if (packed != null) return JsonDocument.Parse(packed.Bytes);
            using var file = typeof(NpcAppearanceCatalog).Assembly.GetManifestResourceStream("UnityWorld.Appearance." + name)
                ?? throw new InvalidDataException("缺少内置骨架元数据：" + name);
            return JsonDocument.Parse(file);
        }
    }
}
