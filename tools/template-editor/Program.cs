using System.Text.Json;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;
using Xianxia.Client.TemplateEditorChecks;

string workspace = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? Path.Combine(AppContext.BaseDirectory, "../../../../../"));
string root = Path.Combine(workspace, "UnityWorld");
string output = Path.Combine(workspace, ".local-verification", "template-editor", Guid.NewGuid().ToString("N"));
int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
void MustFail(Action action, string message) { bool failed = false; try { action(); } catch (InvalidDataException) { failed = true; } Check(failed, message); }

var catalog = TemplateCatalog.Load(root);
TemplateEditorValidationChecks.Run(catalog);
Check(catalog.GetNpc("test_npc_a") != null && catalog.GetNpc("test_npc_b") != null, "原有对战模板缺失。");
Check(catalog.GetNpc("monster_wolf_0") != null, "灰狼目录丢失。");
catalog.AssertDiskUnchanged();
var detached = catalog.GetNpc("monster_wolf_0")!; detached.DisplayName = "不应污染目录";
Check(catalog.GetNpc("monster_wolf_0")!.DisplayName == "灰狼", "目录返回了共享可变定义。");
var session = new TemplateCatalogSession();
Check(session.Refresh(root), "目录加载失败。"); var previousCatalog = session.Current;
Check(!session.Refresh(Path.Combine(output, "missing")) && ReferenceEquals(previousCatalog, session.Current), "失败刷新未保留目录。");

var draft = TemplateDraft.New(catalog, NpcTypes.NpcType.Human, "personal_roundtrip");
draft.NpcFields.DisplayName = "绑定往返样例";
Check(draft.TryAddCard(catalog, "jin_fa", null, out _), "功法添加失败。");
Check(draft.SetMainCard(catalog, draft.Entries[0].EntryId, out _), "主功法选择失败。"); draft.Entries[0].GongFaProgress = 25;
Check(draft.TryAddCard(catalog, "card_fabao_jian", new(1, 1), out _), "第一把剑添加失败。");
var firstSword = draft.Entries.Last(); firstSword.EquipDefineId = "long_sword";
Check(draft.TryAddCard(catalog, "card_fabao_jian", new(3, 1), out _), "第二把剑添加失败。");
var secondSword = draft.Entries.Last(); secondSword.EquipDefineId = "long_sword";
Check(draft.TryAddCard(catalog, "card_form_jian_ci", new(1, 2), out _), "第一把剑的剑刺添加失败。");
var child = draft.Entries.Last(); child.ParentEntryId = firstSword.EntryId;
Check(draft.TryAddCard(catalog, "card_form_jian_zhan", new(2, 2), out _), "第一把剑的剑斩添加失败。");
draft.Entries.Last().ParentEntryId = firstSword.EntryId;
Check(draft.TryAddCard(catalog, "card_form_jian_ci", new(3, 2), out _), "第二把剑的剑刺添加失败。");
draft.Entries.Last().ParentEntryId = secondSword.EntryId;
Check(draft.TryAddCard(catalog, "card_form_jian_zhan", new(4, 2), out _), "第二把剑的剑斩添加失败。");
draft.Entries.Last().ParentEntryId = secondSword.EntryId;
Check(!draft.TryMoveCard(catalog, firstSword.EntryId, new(5, 1), out _) && firstSword.Position == new BackpackPosition(1, 1), "非法移动改变位置。");
draft.Entries.Reverse();
var report = TemplateDocumentConverter.Validate(draft, catalog);
Check(report.IsValid, string.Join(";", report.Issues.Select(i => i.Message)));
Check(report.Template.HpMax == 20 && report.Template.MpMax == 30 && report.Template.SpMax == 10, "默认属性预览错误。");
var store = new TemplateDraftStore(output, Path.Combine(root, "Data"));
string savedPath = store.Save(draft); draft.MarkDirty(); store.Save(draft);
Check(!draft.Dirty && File.Exists(savedPath + ".bak"), "保存未更新状态/保留备份。");
var loaded = store.Load(draft.DraftId);
Check(loaded.Entries.Single(e => e.EntryId == child.EntryId).ParentEntryId == firstSword.EntryId, "重排保存后父绑定变化。");
string exported = store.Export(loaded, catalog);
var imported = TemplateDraftStore.ImportText(File.ReadAllText(exported)).Single().Draft!;
var restored = TemplateDocumentConverter.ToDefinition(imported);
int childIndex = restored.InitCardPlacements.FindIndex(p => p.Key == "card_form_jian_ci" && p.Value == new BackpackPosition(1, 2));
var parent = restored.InitCardPlacements[restored.InitCardPlacements[childIndex].ParentPlacementIndex!.Value - 1];
Check(parent.Key == "card_fabao_jian" && parent.Value == new BackpackPosition(1, 1), "标准导出/导入串父绑定。");
Check(restored.InitEquip.Count == 2 && restored.InitGongFa.Single().CurrentPoint == 25, "重复绑定或进度丢失。");

loaded.RemoveCard(firstSword.EntryId, out _); store.Save(loaded); loaded = store.Load(loaded.DraftId);
Check(!TemplateDocumentConverter.Validate(loaded, catalog).IsValid && loaded.Entries.Single(e => e.EntryId == child.EntryId).ParentEntryId == firstSword.EntryId, "悬空父关联未持久化为错误。");
MustFail(() => store.Export(loaded, catalog), "无效草稿被正式导出。");
var blank = TemplateDraft.New(catalog, NpcTypes.NpcType.Monster, "unfinished"); store.Save(blank);
Check(store.Load(blank.DraftId).Entries.Count == 0 && !TemplateDocumentConverter.Validate(blank, catalog).IsValid, "无效空草稿无法保存恢复。");
var malformed = catalog.GetNpc("test_npc_a")!;
malformed.InitEquip.Add(new InitEquipEntry { CardID = "card_fabao_jian", EquipID = "long_sword", PlacementIndex = 999 });
var malformedDraft = TemplateDocumentConverter.FromDefinition(malformed); store.Save(malformedDraft);
Check(store.Load(malformedDraft.DraftId).UnresolvedBindings.Count == 1 && !TemplateDocumentConverter.Validate(malformedDraft, catalog).IsValid, "错误原绑定丢失。");
Check(TemplateDraftStore.ImportText("{\"ID\":\"unknown\",\"Mystery\":3}")[0].Error != null, "未知字段被忽略。");
Check(TemplateDraftStore.ImportText("{\"SchemaVersion\":2}")[0].Error != null, "未知版本被忽略。");
var copied = TemplateDocumentConverter.Copy(draft, "copied");
Check(copied.DraftId != draft.DraftId && !copied.Entries.Select(e => e.EntryId).Intersect(draft.Entries.Select(e => e.EntryId)).Any(), "复制未重建身份。");
Check(TemplateDocumentConverter.Validate(copied, catalog).IsValid, "复制重映射绑定失败。");
MustFail(() => store.Export(copied, catalog, Path.Combine(root, "Data", "should-not-write.json")), "允许覆盖正式 Data。");

var newCard = catalog.GetCard("card_jin_slash")!; newCard.ID = "memory_only"; newCard.Keywords.Add("FutureKeyword"); newCard.ManaCost = new() { ["Jin"] = 1, ["Huo"] = 1 };
var cards = catalog.Cards.Concat(new[] { newCard }).ToArray();
var labels = CardLibraryQuery.LoadLabels(Path.Combine(workspace, "assets/presentation/keyword_labels.json"));
Check(CardLibraryQuery.Keywords(cards, labels).Any(k => k.Id == "FutureKeyword" && k.DisplayName == "FutureKeyword"), "新关键词未自动显示原 ID。");
var filter = new CardLibraryFilter { Keywords = new() { "FutureKeyword", "FaShu" }, Element = "Huo" };
Check(CardLibraryQuery.Search(cards, filter).Single().ID == "memory_only", "AND/混合元素检索失败。");
filter.Keywords.Add("RemovedKeyword"); Check(CardLibraryQuery.Reconcile(filter, cards).Count == 1 && !filter.Keywords.Contains("RemovedKeyword"), "刷新筛选消失提示失败。");
filter.Clear(); filter.Element = CardLibraryFilter.NoCost;
Check(CardLibraryQuery.Search(cards, filter).All(c => c.ManaCost.Count == 0), "无灵元需求检索错误。");
VerifyShapes();
Console.WriteLine($"TEMPLATE_EDITOR_DATA_OK checks={checks} templates={catalog.Npcs.Count} cards={catalog.Cards.Count} output={output}");

void VerifyShapes()
{
    var dataRoot = Path.Combine(output, "shape-fixture");
    var data = Path.Combine(dataRoot, "Data");
    foreach (var directory in new[] { "Npc", "Card", "Practice/GongFa", "Equip", "Stat", "Shape" })
        Directory.CreateDirectory(Path.Combine(data, directory));
    var core = catalog.GetCard("jin_fa")!;
    File.WriteAllText(Path.Combine(data, "Card", "Shapes.json"), JsonSerializer.Serialize(new[]
    {
        core,
        new CardDefine { ID = "shape-l3", Size = 3, SizeShape = "L3", Keywords = ["Passive"] },
        new CardDefine { ID = "shape-one", Keywords = ["Passive"] },
        new CardDefine { ID = "shape-bad", Size = 3, SizeShape = "unknown", Keywords = ["Passive"] }
    }, TemplateJson.Options));
    File.WriteAllText(Path.Combine(data, "Shape", "Basic.json"),
        JsonSerializer.Serialize(new[] { catalog.GetShape("L3") }, TemplateJson.Options));
    File.WriteAllText(Path.Combine(data, "Practice", "GongFa", "Core.json"),
        JsonSerializer.Serialize(new[] { catalog.GetCultivation("jin_fa") }, TemplateJson.Options));
    File.WriteAllText(Path.Combine(data, "Stat", "Stats.json"), JsonSerializer.Serialize(catalog.Stats, TemplateJson.Options));

    File.WriteAllText(Path.Combine(data, "ExtraElementDefines.json"), """[{"ID":"smoke-extra"}]""");
    var shapes = TemplateCatalog.Load(dataRoot);
    var modCard = new CardDefine { ID = "mod-cost", Keywords = ["FaShu"], ManaCost = new() { ["smoke-extra"] = 1 } };
    Check(CardLibraryQuery.UnavailableReason(modCard, shapes.GetShape, shapes.HasExtraElement) == null &&
        CardLibraryQuery.UnavailableReason(modCard, catalog.GetShape, catalog.HasExtraElement) != null,
        "独立 Mod 目录的扩展费用可识别且不污染其他目录");
    var fixture = TemplateDraft.New(shapes, NpcTypes.NpcType.Human, "shape-fixture");
    Check(fixture.TryAddCard(shapes, core.ID, null, out _) &&
        fixture.SetMainCard(shapes, fixture.Entries[0].EntryId, out _), "形状样例可选主功法");
    Check(fixture.TryAddCard(shapes, "shape-l3", new(2, 1), out _), "能从独立目录摆放 L 形");
    Check(fixture.TryAddCard(shapes, "shape-one", new(3, 1), out _), "缺角中可放一张单格横条");
    var single = fixture.Entries.Last();
    Check(!fixture.TryMoveCard(shapes, single.EntryId, new(3, 2), out _) && single.Position == new BackpackPosition(3, 1),
        "真实占格重叠时保留原位置");
    Check(!fixture.TryAddCard(shapes, "shape-bad", new(5, 1), out var error) && error.Contains("unknown"),
        "拖入未知形状给出引用诊断");
    Check(TemplateDocumentConverter.Validate(fixture, shapes).IsValid, "L 形装配通过模板预检");
    fixture.Entries.Add(new TemplateCardEntry { CardDefineId = "shape-bad", Position = new(5, 1) });
    var shapeStore = new TemplateDraftStore(Path.Combine(output, "shape-drafts"), data);
    shapeStore.Save(fixture);
    var restored = shapeStore.Load(fixture.DraftId);
    Check(restored.Entries.Last().CardDefineId == "shape-bad" &&
        TemplateDocumentConverter.Validate(restored, shapes).Issues.Any(i => i.Code == "card.shape" && i.Message.Contains("unknown")),
        "无效形状草稿可保存重开并保留结构化诊断");
}
