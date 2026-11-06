using System.Text.Json;
using Xianxia.Client.Templates;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.TemplateEditorChecks;

/// <summary>编辑器配置检查与预览回归；只加载定义目录，不启动世界或创建运行时实体。</summary>
public static class TemplateEditorValidationChecks
{
    public static void Run(TemplateCatalog catalog)
    {
        int assertions = 0;
        void Check(bool valid, string reason) { assertions++; if (!valid) throw new InvalidOperationException("纯模板预检：" + reason); }
        NpcDefine Basic() => new()
        {
            ID = "pure-validation-smoke", MainGongFaId = "jin_fa",
            InitCardPlacements = [new() { Key = "jin_fa", Value = new(3, 3) },
                new() { Key = "card_jin_slash", Value = new(1, 1) }, new() { Key = "card_jin_slash", Value = new(1, 2) }]
        };
        var definitions = new TestDefinitions(catalog);
        var template = Basic();
        var beforeJson = JsonSerializer.Serialize(template);
        var baseline = TemplateEditorValidator.Validate(template, definitions);
        Check(baseline.IsValid && baseline.OccupiedSp == 3 && baseline.Layout?.Width == 5, "默认装配可预检");
        Check(baseline.HpMax == 20 && baseline.MpMax == 30 && baseline.SpMax == 10, "沿用默认八维与公式");

        var again = TemplateEditorValidator.Validate(template, definitions);
        Check(JsonSerializer.Serialize(baseline) == JsonSerializer.Serialize(again), "重复检查结果一致");
        Check(beforeJson == JsonSerializer.Serialize(template), "检查和预览不改写模板");
        var invalid = Basic(); invalid.MainGongFaId = "";
        Check(TemplateEditorValidator.Validate(invalid, definitions).Issues.Any(i => i.Code == "main.missing"),
            "缺少主功法由编辑器诊断");

        var twoProblems = Basic();
        twoProblems.InitCardPlacements[1].Key = "missing-card";
        twoProblems.InitCardPlacements[2].Value = new(6, 2);
        var issues = TemplateEditorValidator.Validate(twoProblems, definitions).Issues;
        Check(issues.Any(i => i.Code == "card.missing" && i.PlacementIndex == 2) &&
              issues.Any(i => i.Code == "placement.outside" && i.PlacementIndex == 3 && i.Cells?.Contains(new(6, 2)) == true), "引用和越界独立定位");
        var overlap = Basic(); overlap.InitCardPlacements[2].Value = new(1, 1);
        Check(TemplateEditorValidator.Validate(overlap, definitions).Issues.Any(i => i.Code == "placement.overlap"), "重叠可定位");
        var badBinding = Basic(); badBinding.InitEquip.Add(new() { CardID = "card_jin_slash", EquipID = "long_sword" });
        Check(TemplateEditorValidator.Validate(badBinding, definitions).Issues.Any(i => i.Code == "binding.ambiguous"), "重复卡绑定须明确实例");
        var wrongIndex = Basic(); wrongIndex.InitEquip.Add(new() { CardID = "card_jin_slash", EquipID = "long_sword", PlacementIndex = 1 });
        Check(TemplateEditorValidator.Validate(wrongIndex, definitions).Issues.Any(i => i.Code == "binding.index"), "错误绑定序号拒绝");
        var overSp = Basic(); overSp.InitStat["ShenShi"] = 2;
        Check(TemplateEditorValidator.Validate(overSp, definitions).Issues.Any(i => i.Code == "sp.exceeded"), "SP超限");
        var overCapacity = Basic(); overCapacity.InitStat["ShenShi"] = 26;
        Check(TemplateEditorValidator.Validate(overCapacity, definitions).Issues.Any(i => i.Code == "sp.capacity"), "SP本身不得超空间");
        var unsupported = Basic(); unsupported.InitStat["UnknownStat"] = 123; unsupported.InitStat["AffinityHuo"] = 77;
        unsupported.InitialTraits = ["example"];
        var warnings = TemplateEditorValidator.Validate(unsupported, definitions);
        Check(warnings.IsValid && warnings.Issues.Any(i => i.Code == "stat.unknown") && warnings.Issues.Any(i => i.Code == "stat.affinity-overwritten") &&
            warnings.Issues.Any(i => i.Code == "field.traits-unused"), "不消费字段给提示且保留原值");
        definitions.Stats["HpMax"].ExtraBase = "HpMax + 1";
        var cycle = TemplateEditorValidator.Validate(Basic(), definitions);
        Check(cycle.HpMax == null && cycle.SpMax == 10, "循环公式不可用不伪造数值、不阻断独立SP");
        definitions.Stats.Remove("SpMax");
        var unavailable = TemplateEditorValidator.Validate(Basic(), definitions);
        Check(!unavailable.IsValid && unavailable.SpMax == null && unavailable.Issues.Any(i => i.Code == "sp.unavailable"), "SP不可用阻塞");
        Check(TemplateEditorValidator.ValidateCard(new CardDefine { ID = "bad-cost", Size = 2, Keywords = ["FaBao"], ManaCost = new() { ["Jin"] = 1 } })
            .Any(i => i.Code == "card.mana-size"), "卡库共用主动灵元成本检查");
        Console.WriteLine($"TEMPLATE_EDITOR_VALIDATION_OK assertions={assertions}");
    }

    // Formula fault injection uses detached definitions, so it cannot alter the loaded catalog.
    private sealed class TestDefinitions(TemplateCatalog catalog) : ITemplateEditorDefinitions
    {
        public Dictionary<string, StatDefine> Stats { get; } = catalog.Stats
            .ToDictionary(d => d.ID, StringComparer.Ordinal);
        public CardDefine? GetCard(string id) => catalog.GetCard(id);
        public CultivationDefine? GetCultivation(string id) => catalog.GetCultivation(id);
        public EquipDefine? GetEquip(string id) => catalog.GetEquip(id);
        public StatDefine? GetStat(string id) => Stats.GetValueOrDefault(id);
        public IEnumerable<StatDefine> GetStats() => Stats.Values;
    }
}
