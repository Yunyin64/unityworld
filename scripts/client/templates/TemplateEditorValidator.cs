using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

/// <summary>模板预检的只读定义入口；缺失定义返回 null，不能创建运行时实例。</summary>
public interface ITemplateEditorDefinitions
{
    CardDefine? GetCard(string id);
    CultivationDefine? GetCultivation(string id);
    EquipDefine? GetEquip(string id);
    StatDefine? GetStat(string id);
    IEnumerable<StatDefine> GetStats();
}

/// <summary>客户端编辑器的配置检查与属性预览；只读取定义，不创建角色或调用后端装配。</summary>
public static class TemplateEditorValidator
{
    private static readonly HashSet<string> GeneratedAffinities = new(StringComparer.Ordinal)
        { "AffinityJin", "AffinityMu", "AffinityShui", "AffinityHuo", "AffinityTu" };

    public static TemplateValidationReport Validate(NpcDefine? template, ITemplateEditorDefinitions definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var issues = new List<TemplateIssue>();
        void Error(string code, string message, string field, int? index = null, IEnumerable<BackpackPosition>? cells = null)
            => issues.Add(new(code, TemplateIssueSeverity.Error, message, field, index,
                Array.AsReadOnly((cells ?? []).ToArray())));
        void Warning(string code, string message, string field, int? index = null)
            => issues.Add(new(code, TemplateIssueSeverity.Warning, message, field, index, Array.Empty<BackpackPosition>()));

        if (template == null)
        {
            Error("template.missing", "模板为空。", "");
            return new() { Issues = issues.AsReadOnly() };
        }
        if (string.IsNullOrWhiteSpace(template.ID)) Error("template.id", "模板 ID 不能为空。", "ID");
        if (!Enum.IsDefined(template.Gender)) Error("template.gender", "性别值无效。", "Gender");
        if (!Enum.IsDefined(template.NpcType)) Error("template.type", "种族值无效。", "NpcType");
        if (template.Surname == null || template.GivenName == null || template.DaoTitle == null)
            Error("template.name", "姓名和道号字段不能为 null，可使用空字符串。", "Surname");
        if (template.InitCardDeck == null) Error("template.deck.null", "InitCardDeck 不能为 null。", "InitCardDeck");
        else if (template.InitCardDeck.Length != 0)
            Error("template.legacy-deck", "模板不得混用 InitCardDeck 与 InitCardPlacements；每个上场实例必须有明确位置。", "InitCardDeck");

        var placements = template.InitCardPlacements ?? [];
        if (template.InitCardPlacements == null) Error("placements.null", "位置列表不能为 null。", "InitCardPlacements");
        var cards = new CardDefine?[placements.Count];
        var cellsByEntry = new Dictionary<int, List<BackpackCell>>();
        long occupied = 0;
        bool completeSizes = true;
        BackpackLevelDefine? layout = null;
        HashSet<BackpackCell>? validCells = null;
        var mainIndices = new List<int>();
        if (string.IsNullOrWhiteSpace(template.MainGongFaId))
            Error("main.missing", "缺少 MainGongFaId。", "MainGongFaId");
        else
        {
            mainIndices = placements.Select((entry, index) => (entry, index))
                .Where(pair => pair.entry?.Key == template.MainGongFaId).Select(pair => pair.index).ToList();
            if (mainIndices.Count != 1)
                Error("main.ambiguous", $"主功法 {template.MainGongFaId} 必须唯一对应位置列表实例，实际 {mainIndices.Count} 张。", "MainGongFaId");
            var cultivation = definitions.GetCultivation(template.MainGongFaId);
            if (cultivation == null) Error("main.cultivation-missing", $"主功法 {template.MainGongFaId} 没有功法定义。", "MainGongFaId");
            else
            {
                try { layout = cultivation.GetBackpackDefinition(1); validCells = layout.GetValidCells(); }
                catch (Exception exception) { layout = null; Error("main.layout", exception.Message, "MainGongFaId"); }
            }
        }

        var occupiedBy = new Dictionary<BackpackCell, int>();
        for (int i = 0; i < placements.Count; i++)
        {
            int index = i + 1;
            string field = $"InitCardPlacements[{i}]";
            var entry = placements[i];
            if (entry == null) { completeSizes = false; Error("placement.null", "位置条目为空。", field, index); continue; }
            bool validPosition = entry.Value is { X: >= 1, Y: >= 1 };
            if (entry.Value == null) Error("placement.missing-position", "卡牌尚未放置。", field + ".Value", index);
            else if (!validPosition) Error("placement.coordinate", "公开坐标必须从 (1,1) 起。", field + ".Value", index, [entry.Value.Value]);
            var card = string.IsNullOrWhiteSpace(entry.Key) ? null : definitions.GetCard(entry.Key);
            cards[i] = card;
            if (card == null) { completeSizes = false; Error("card.missing", $"卡牌定义不存在：{entry.Key}。", field + ".Key", index); continue; }
            if (card.Size > 0) occupied += card.Size; else completeSizes = false;
            issues.AddRange(ValidateCard(card).Select(issue => issue with { FieldPath = field + ".Key", PlacementIndex = index }));
            List<BackpackCell> shape;
            try { shape = card.GetShape(); }
            catch (Exception) { continue; } // ValidateCard 已给出形状原因；不猜测未知占格。
            if (!validPosition || entry.Value is not BackpackPosition position) continue;
            var origin = BackpackCoordinates.ToCell(position);
            var cells = shape.Select(offset => new BackpackCell(origin.X + offset.X, origin.Y + offset.Y)).ToList();
            cellsByEntry[i] = cells;
            if (validCells != null)
            {
                var outside = cells.Where(cell => !validCells.Contains(cell)).ToList();
                if (outside.Count > 0) Error("placement.outside", $"卡 {entry.Key} 越出背包有效空间。", field + ".Value", index, outside.Select(BackpackCoordinates.ToPosition));
            }
            var overlap = cells.Where(occupiedBy.ContainsKey).Distinct().ToList();
            if (overlap.Count > 0) Error("placement.overlap", $"卡 {entry.Key} 与其他条目重叠。", field + ".Value", index, overlap.Select(BackpackCoordinates.ToPosition));
            foreach (var cell in cells) occupiedBy.TryAdd(cell, index);
        }
        if (mainIndices.Count == 1 && layout != null && cellsByEntry.TryGetValue(mainIndices[0], out var mainCells) &&
            !mainCells.ToHashSet().SetEquals(layout.MainCardCells))
            Error("main.position", "主功法必须完整占用功法定义的固定格，不能移动。", $"InitCardPlacements[{mainIndices[0]}].Value", mainIndices[0] + 1,
                layout.MainCardCells.Select(BackpackCoordinates.ToPosition));

        int? Resolve(string cardId, int? bindingIndex, string field)
        {
            if (string.IsNullOrWhiteSpace(cardId)) { Error("binding.card-id", "绑定卡牌 ID 不能为空。", field); return null; }
            if (bindingIndex is int index)
            {
                if (index < 1 || index > placements.Count || placements[index - 1]?.Key != cardId)
                { Error("binding.index", $"绑定 {cardId} 的 PlacementIndex={index} 不匹配位置条目。", field, index); return null; }
                return index;
            }
            var matches = placements.Select((entry, i) => (entry, i)).Where(pair => pair.entry?.Key == cardId).ToArray();
            if (matches.Length != 1) { Error("binding.ambiguous", $"绑定 {cardId} 匹配 {matches.Length} 张卡，重复定义必须指定 PlacementIndex。", field); return null; }
            return matches[0].i + 1;
        }

        if (template.InitGongFa == null) Error("gongfa.null", "功法绑定列表不能为 null。", "InitGongFa");
        var gongEntries = template.InitGongFa ?? [];
        var gongBindings = new HashSet<int>();
        for (int i = 0; i < gongEntries.Count; i++)
        {
            var entry = gongEntries[i]; string field = $"InitGongFa[{i}]";
            int? index = Resolve(entry.DefineId, entry.PlacementIndex, field);
            if (index.HasValue && !gongBindings.Add(index.Value)) Error("gongfa.duplicate", "同一功法实例被重复绑定。", field, index);
            var cultivation = string.IsNullOrWhiteSpace(entry.DefineId) ? null : definitions.GetCultivation(entry.DefineId);
            if (cultivation == null) Error("gongfa.missing", $"功法定义不存在：{entry.DefineId}。", field + ".DefineId", index);
            else if (entry.CurrentPoint < 0 || entry.CurrentPoint > cultivation.MaxPoint)
                Warning("gongfa.progress-clamped", $"功法进度将在装配时夹紧至 0–{cultivation.MaxPoint}。", field + ".CurrentPoint", index);
        }
        for (int i = 0; i < cards.Length; i++)
            if (cards[i] is { } placedCard && definitions.GetCultivation(placedCard.ID) is { MaxPoint: < 0 })
                Error("gongfa.max-point", "功法 MaxPoint 不能为负数。", $"InitCardPlacements[{i}].Key", i + 1);

        if (template.InitEquip == null) Error("equip.null", "装备绑定列表不能为 null。", "InitEquip");
        var equipEntries = template.InitEquip ?? [];
        var equipBindings = new HashSet<int>();
        for (int i = 0; i < equipEntries.Count; i++)
        {
            var entry = equipEntries[i]; string field = $"InitEquip[{i}]";
            int? index = Resolve(entry.CardID, entry.PlacementIndex, field);
            var equip = string.IsNullOrWhiteSpace(entry.EquipID) ? null : definitions.GetEquip(entry.EquipID);
            if (equip == null) Error("equip.missing", $"装备定义不存在：{entry.EquipID}。", field + ".EquipID", index);
            if (index.HasValue && equip != null && !equipBindings.Add(index.Value))
                Error("equip.duplicate", "同一实例被重复绑定装备。", field, index);
        }
        for (int i = 0; i < placements.Count; i++)
        {
            if (placements[i]?.ParentPlacementIndex is not int parent) continue;
            if (parent < 1 || parent > placements.Count || parent == i + 1 || !equipBindings.Contains(parent))
                Error("parent.invalid", $"父装备序号 {parent} 无效、引用自身或没有有效装备绑定。", $"InitCardPlacements[{i}].ParentPlacementIndex", i + 1);
        }

        Warning("field.age-unused", "当前出生流程未消费 InitAge；原值保留，运行时年龄仍从 0 初始化。", "InitAge");
        Warning("field.realm-unused", "当前出生流程未消费 InitCultivationLevel；不能据此预览境界加成。", "InitCultivationLevel");
        if (template.InitialTraits?.Length > 0) Warning("field.traits-unused", "当前特质系统尚未装配 InitialTraits；原列表保留。", "InitialTraits");
        Warning("field.path-unused", "当前模板装配不按主功法写回角色道途。", "MainGongFaId");
        var projection = ProjectStats(template, definitions, issues);
        if (!projection.Sp.HasValue) Error("sp.unavailable", "SP 上限不可可靠计算，容量校验未通过。", "InitStat");
        else
        {
            if (completeSizes && occupied > projection.Sp.Value)
                Error("sp.exceeded", $"上场占格 {occupied} 超过 SP 上限 {projection.Sp.Value}。", "InitCardPlacements");
            if (validCells != null && projection.Sp.Value > validCells.Count)
                Error("sp.capacity", $"SP 上限 {projection.Sp.Value} 超过背包有效格数 {validCells.Count}。", "InitStat.ShenShi");
        }
        if (occupied > int.MaxValue) Error("sp.overflow", "占格总量超出支持范围。", "InitCardPlacements");
        return new()
        {
            Issues = issues.AsReadOnly(), Layout = layout?.Clone(), OccupiedSp = (int)Math.Min(occupied, int.MaxValue),
            HpMax = projection.Hp, MpMax = projection.Mp, SpMax = projection.Sp
        };
    }

    /// <summary>供目录兼容提示与模板预检使用；不读取 Lua、不需要运行实体。</summary>
    public static IReadOnlyList<TemplateIssue> ValidateCard(CardDefine? card)
    {
        if (card == null) return [new("card.missing", TemplateIssueSeverity.Error, "卡牌定义不存在。", "")];
        List<BackpackCell> shape;
        try { shape = card.GetShape(); }
        catch (Exception exception) { return [new("card.shape", TemplateIssueSeverity.Error, exception.Message, "Shape")]; }
        return ValidateCardProperties(card.Size, shape, card.Keywords, card.ManaCost, card.ScanActivatable,
            card.CompatibilityStatus, card.CompatibilityReason);
    }

    /// <summary>编辑器的卡牌配置诊断，供卡库提示与模板检查使用。</summary>
    private static IReadOnlyList<TemplateIssue> ValidateCardProperties(int size, IReadOnlyList<BackpackCell>? shape,
        IReadOnlyList<string>? keywords, IReadOnlyDictionary<string, int>? mana, bool scanActivatable,
        string compatibility, string compatibilityReason)
    {
        var issues = new List<TemplateIssue>();
        void Add(string code, string message, string field) => issues.Add(new(code, TemplateIssueSeverity.Error, message, field));
        if (compatibility != "Migrated") Add("card.compatibility", $"不可装配：{compatibility}，{compatibilityReason}", "CompatibilityStatus");
        if (keywords == null || mana == null) { Add("card.fields-null", "Keywords 和 ManaCost 不能为 null。", ""); return issues.AsReadOnly(); }
        if (keywords.Any(k => k is "Min" or "Max" || k?.StartsWith("ManaAdj", StringComparison.Ordinal) == true) || mana.Keys.Any(k => k is "Min" or "Max"))
            Add("card.deprecated", "依赖 Deprecated 的 Min/Max/ManaAdj，禁止装配。", "Keywords");
        if (shape == null || size < 1 || shape.Count != size || shape.Distinct().Count() != size ||
            shape.Any(p => p.X < 0 || p.Y < 0) || shape.Count > 0 && (shape.Min(p => p.X) != 0 || shape.Min(p => p.Y) != 0))
            Add("card.shape", "形状必须规范化、无重复且占格数等于正数 Size。", "Shape");
        bool active = scanActivatable || keywords.Contains("FaShu") || keywords.Contains("ZhaoShi") || keywords.Contains("FaBao");
        if (active && (mana.Values.Any(n => n <= 0) || mana.Values.Sum(n => (long)n) != size))
            Add("card.mana-size", "主动卡的激活灵元总数必须等于 Size，且各需求为正数。", "ManaCost");
        return issues.AsReadOnly();
    }

    private static (float? Hp, float? Mp, float? Sp) ProjectStats(NpcDefine template,
        ITemplateEditorDefinitions definitions, List<TemplateIssue> issues)
    {
        void Warning(string code, string message, string field) => issues.Add(new(code, TemplateIssueSeverity.Warning, message, field));
        StatBlock block;
        try
        {
            block = new StatBlock(definitions.GetStats());
            block.InitType("Npc");
        }
        catch (Exception exception)
        {
            Warning("stat.catalog", "属性目录无法初始化：" + exception.Message, "InitStat");
            return (null, null, null);
        }
        // 亲和将由 Soul 生成，预览不使用默认 0 或手填值假装知道生成结果。
        foreach (var affinity in GeneratedAffinities) block.GetFinalCache().Remove(affinity);
        var failedInputs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in template.InitStat ?? [])
        {
            string field = "InitStat." + pair.Key;
            var stat = definitions.GetStat(pair.Key);
            if (stat == null) { Warning("stat.unknown", $"未知属性 {pair.Key} 将被运行时忽略。", field); continue; }
            if (GeneratedAffinities.Contains(pair.Key)) { Warning("stat.affinity-overwritten", $"{pair.Key} 将被 Soul 生成的亲和值覆盖。", field); continue; }
            if (stat.BaseType != StatBaseType.Primary) { Warning("stat.derived-ignored", $"{pair.Key} 不是 Primary，手填基础值不会生效。", field); continue; }
            try { block.SetBase(pair.Key, pair.Value); }
            catch (Exception exception)
            {
                failedInputs.Add(pair.Key);
                block.GetFinalCache().Remove(pair.Key);
                Warning("stat.input-unavailable", $"{pair.Key} 无法按出生顺序求值：{exception.Message}", field);
            }
        }
        float? Read(string id)
        {
            try
            {
                if (definitions.GetStat(id) == null || failedInputs.Contains(id)) throw new InvalidOperationException("定义缺失或配置求值失败。");
                float value = block.GetReadOnly(id, float.NaN);
                if (!float.IsFinite(value) || value < int.MinValue || (double)value > int.MaxValue)
                    throw new InvalidOperationException("结果不是有限的有效整数范围数值。");
                return (int)value; // 与 Npc.GetHpMax/GetMpMax/GetSpMax 相同。
            }
            catch (Exception exception) { Warning("stat.preview-unavailable", $"{id} 预览不可用：{exception.Message}", "InitStat." + id); return null; }
        }
        return (Read("HpMax"), Read("MpMax"), Read("SpMax"));
    }
}
