// 重要 TODO：未来建立独立的 NPC 周行动系统时，将本格探索的遭遇配置、事件判定和授卡结算迁入该系统。
#nullable enable
using System.Text.Json;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain;

/// <summary>按真实地形的 EncounterRefs 结算本地探索。获得的行为卡留在持卡列表中。</summary>
internal sealed class ExploreEncounterData
{
    private sealed class Encounter
    {
        public string ID { get; init; } = "";
        // 缺少 Chance 与显式的 0 有不同含义，前者在加载时报告配置错误。
        public double Chance { get; init; } = double.NaN;
        public string? BehaviorCardId { get; init; }
    }

    private readonly Dictionary<string, Encounter> _events = new(StringComparer.Ordinal);

    internal static ExploreEncounterData Load(string path, MapCatalog map, BehaviorCardDataMgr cards)
    {
        var result = new ExploreEncounterData();
        // 旧数据目录可以没有此表。如果已有地形声明了遭遇，下方仍会报出具体无效引用。
        if (File.Exists(path))
        {
            Encounter?[] entries;
            try
            {
                using var file = File.OpenRead(path);
                entries = JsonSerializer.Deserialize<Encounter?[]>(file, MapCatalog.JsonOptions)
                    ?? throw new InvalidDataException($"{path}: expected an array of encounters.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"{path}: {ex.Message}", ex);
            }
            for (int index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.ID))
                    throw new InvalidDataException($"{path}[{index}].ID is required.");
                if (!result._events.TryAdd(entry.ID, entry))
                    throw new InvalidDataException($"{path}: duplicate encounter ID '{entry.ID}'.");
                if (!double.IsFinite(entry.Chance) || entry.Chance < 0 || entry.Chance > 1)
                    throw new InvalidDataException($"{path}: '{entry.ID}'.Chance must be finite and within [0,1], got {entry.Chance}.");
                if (!string.IsNullOrEmpty(entry.BehaviorCardId) && !cards.Contains(entry.BehaviorCardId))
                    throw new InvalidDataException($"{path}: '{entry.ID}'.BehaviorCardId '{entry.BehaviorCardId}' does not exist.");
            }
        }

        foreach (var terrain in map.Tiles.Values)
        {
            if (terrain.EncounterRefs == null)
                throw new InvalidDataException($"TileDefines '{terrain.ID}'.EncounterRefs must be an array.");
            foreach (var id in terrain.EncounterRefs)
                if (string.IsNullOrWhiteSpace(id) || !result._events.ContainsKey(id))
                    throw new InvalidDataException($"TileDefines '{terrain.ID}'.EncounterRefs references unknown encounter '{id}' in {path}.");
        }
        return result;
    }

    internal static (string? EventId, string? GrantedCardId) Resolve(Plane plane, TileId tileId, int npcId, Rng rng)
    {
        var tile = plane.GetTile(tileId) ?? throw new InvalidOperationException($"Missing exploration tile {tileId}.");
        var data = GameDataMgr.Instance;
        var candidates = data.MapCatalog.Tiles[tile.TypeId].EncounterRefs;
        if (candidates.Count == 0) return default;

        // 只抽一个本格候选。即使命中失败，也不能改抽其他事件或邻格。
        string id = candidates[rng.Range(0, candidates.Count)];
        var encounter = data.ExploreEncounters._events[id];
        if (encounter.Chance <= 0 || (encounter.Chance < 1 && rng.Range(0f, 1f) >= encounter.Chance))
            return default;

        string? cardId = encounter.BehaviorCardId;
        if (string.IsNullOrEmpty(cardId)) return (id, null);
        if (BehaviorCardMgr.Instance?.TryGiveCard(npcId, cardId) != true)
            throw new InvalidOperationException($"Encounter '{id}' could not grant behavior card '{cardId}' to NPC {npcId}.");
        return (id, cardId);
    }
}
