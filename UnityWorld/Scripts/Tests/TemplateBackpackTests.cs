using UnityWorld.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Game.Domain.Combat;

namespace UnityWorld.Tests
{
    /// <summary>正式数据、重复实例、坐标和装配事务验收。</summary>
    public static class TemplateBackpackTests
    {
        private static int _assertions;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        private static void Check(bool value, string detail)
        {
            _assertions++;
            if (!value) throw new InvalidOperationException("模板背包断言失败：" + detail);
        }
        private static NpcDefine Copy(NpcDefine definition) => JsonSerializer.Deserialize<NpcDefine>(JsonSerializer.Serialize(definition, JsonOptions), JsonOptions)!;
        private static NpcDefine Basic() => new()
        {
            ID = "template-contract", MainGongFaId = "jin_fa",
            InitStat = new() { ["ShenShi"] = 12, ["QiXue"] = 10, ["QiGan"] = 20, ["TiPo"] = 10 },
            InitGongFa = [new() { DefineId = "jin_fa", CurrentPoint = 100, PlacementIndex = 1 }],
            InitCardPlacements = [new() { Key = "jin_fa", Value = new(3, 3) }, new() { Key = "card_jin_slash", Value = new(1, 1) }, new() { Key = "card_jin_slash", Value = new(1, 2) }]
        };

        public static void Run()
        {
            _assertions = 0;
            // 配置诊断与属性预览在 tools/template-editor 中验证，此处只覆盖后端装配。
            CoordinatesAndShapes();
            ConfiguredShapes();
            ConfiguredShapeBattle();
            InstanceAndSnapshotIsolation();
            GeneratedAndPlayerCardSources();
            RejectAndRollback();
            FormalTemplates();
            Console.WriteLine($"TEMPLATE_BACKPACK_TESTS_OK assertions={_assertions}");
        }

        private static void ConfiguredShapes()
        {
            var defaultLayout = new CultivationDefine { ID = "default-layout" }.GetBackpackDefinition(1);
            Check(defaultLayout.GetValidCells().Count == 25 && defaultLayout.MainCardCells.SequenceEqual([new(2, 2)]), "功法默认 5×5");
            var second = new BackpackLevelDefine { Level = 2, Width = 6, Height = 4, MainCardCells = [new(1, 1)] };
            var cultivation = new CultivationDefine { ID = "rectangle-layout", BackpackLevels = [defaultLayout, second] };
            var layout = cultivation.GetBackpackDefinition(2);
            Check(layout.GetValidCells().Count == 24 && layout.MainCardCells.SequenceEqual([new(1, 1)]), "指定等级提供矩形与固定格");
            second.ValidCells = second.GetValidCells().Where(cell => cell != new BackpackCell(0, 0)).ToList();
            ExpectInvalid(() => cultivation.GetBackpackDefinition(2), "正式矩形拒绝缺口", "rectangle-layout", "2");

            foreach (var (id, size) in new[] { ("L3", 3), ("Square4", 4), ("L5", 5) })
            {
                var card = JsonSerializer.Deserialize<CardDefine>($$"""
                    { "ID": "shape-{{id}}", "size": {{size}}, "SizeShape": "{{id}}", "keywords": ["Passive"] }
                    """, JsonOptions)!;
                var instance = CardMgr.Instance.InstantiateFromDefine(card);
                Check(instance.GetSize() == size && instance.BaseData.Shape.SequenceEqual(ShapeDefineMgr.Instance.Get(id).Cells),
                    $"正式 {id} 配置解析为独立占格并计入 Size");
                instance.BaseData.Shape.Clear();
                Check(card.GetShape().Count == size, "修改卡实例占格不污染定义");
            }

            var fallback = JsonSerializer.Deserialize<CardDefine>("""{"ID":"default-one"}""", JsonOptions)!;
            Check(fallback.Size == 1 && fallback.GetShape().SequenceEqual([new(0, 0)]), "Size 和 SizeShape 缺省为单格");
            fallback.Size = 3;
            fallback.SizeShape = "";
            Check(fallback.GetShape().SequenceEqual([new(0, 0), new(1, 0), new(2, 0)]), "空 SizeShape 使用横条");
            fallback.SizeShape = "missing-shape";
            ExpectInvalid(() => fallback.GetShape(), "未知形状被拒绝", "default-one", "missing-shape");
            ExpectInvalid(() => fallback.GetShape(id => new ShapeDefine { ID = id, Cells = [new(0, 0), new(0, 0), new(1, 0)] }),
                "重复格子被拒绝", "missing-shape");
            ExpectInvalid(() => fallback.GetShape(id => new ShapeDefine { ID = id, Cells = [new(0, 0)] }),
                "格数不符被拒绝", "Size 3");

            var file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, """[{"ID":"duplicate"},{"ID":"duplicate"}]""");
                ExpectInvalid(() => ShapeDefineMgr.Instance.Load(file), "重复形状 ID 被拒绝", "duplicate");
            }
            finally
            {
                File.Delete(file);
                ShapeDefineMgr.Instance.Load();
            }
        }

        private static void ConfiguredShapeBattle()
        {
            var card = CardDefineMgr.Instance.Get("card_jin_slash");
            var old = (card.Size, card.SizeShape, card.ManaCost);
            var template = Basic();
            template.InitCardPlacements = [new() { Key = "jin_fa", Value = new(3, 3) }, new() { Key = card.ID, Value = new(2, 1) }];
            try
            {
                card.Size = 3;
                card.ManaCost = new() { ["Jin"] = 3 };
                card.SizeShape = "bad-reference";
                ExpectInvalid(() => NpcMgr.Instance.Assemble(template), "模板预检拒绝形状引用", card.ID, "bad-reference");
                card.SizeShape = "L3";
                template.InitCardPlacements[1].Value = new(5, 1);
                ExpectInvalid(() => NpcMgr.Instance.Assemble(template), "模板预检拒绝异形越界", card.ID);
                template.InitCardPlacements[1].Value = new(3, 2);
                ExpectInvalid(() => NpcMgr.Instance.Assemble(template), "形状实际占格覆盖主功法", card.ID);
                template.InitCardPlacements[1].Value = new(2, 1);
                var npc = NpcMgr.Instance.Assemble(template);
                var opponent = BackpackModelTests.CreateNpc();
                var scene = new CombatScene();
                scene.Init(7481, [(npc, CombatTeam.TeamA), (opponent, CombatTeam.TeamB)], 100);
                try
                {
                    scene.PreStart();
                    scene.Start();
                    var owner = scene.GetCombatants()[npc.Id];
                    var attack = owner.GetField().Single(c => c.DefineId == card.ID);
                    Check(npc.GetFieldSize() == 4 && owner.Backpack.GetCardCells(attack.Id).SequenceEqual([new(1, 0), new(1, 1), new(2, 1)]),
                        "模板真实装配与战斗使用相同异形占格");
                    owner.Backpack.RemoveMana(int.MaxValue, null, scene.Soul);
                    var notch = new BackpackCell(2, 0);
                    owner.Backpack.PlaceMana(notch, ElementType.Jin);
                    Check(owner.Backpack.TryAssign(notch, attack), "异形缺角可容纳灵元");
                    for (int i = 0; i < 2; i++)
                    {
                        owner.Backpack.PlaceMana(notch, ElementType.Jin);
                        Check(owner.Backpack.TryAssign(notch, attack), "3格卡蓄满三枚灵元");
                    }
                    Check(owner.SlideCombatCard(attack, CombatCardSlideType.Right)
                        && owner.Backpack.GetCardCells(attack.Id).SequenceEqual([new(3, 0), new(3, 1), new(4, 1)]),
                        "整张 L 形按实际占格滑到矩形边缘");
                    for (int i = 0; i < 10; i++) scene.Tick();
                    Check(owner.PendingSlot == null, "跨行卡第一行不会发动");
                    for (int i = 0; i < 10; i++) scene.Tick();
                    Check(owner.PendingSlot?.SourceCard == attack, "跨行卡扫到第二行最后一格才发动");
                }
                finally { scene.Cleanup(); }
            }
            finally { (card.Size, card.SizeShape, card.ManaCost) = old; }
        }

        private static void CoordinatesAndShapes()
        {
            Check(BackpackCoordinates.ToCell(new(1, 1)) == new BackpackCell(0, 0), "公开(1,1)转换");
            Check(BackpackCoordinates.ToPosition(new(4, 4)) == new BackpackPosition(5, 5), "公开(5,5)转换");
            var npc = NpcMgr.Instance.Assemble(Basic());
            var horizontal = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "size-three-geometry", Size = 3, Keywords = ["Passive"] });
            npc.GainCard(horizontal);
            npc.PlaceBackpackCard(horizontal.Id, 2, 4);
            Check(npc.GetBackpackCardPosition(horizontal.Id) == new BackpackPosition(2, 4), "装配保存公开坐标");
            Check(horizontal.BaseData.Shape.SequenceEqual(new[] { new BackpackCell(0, 0), new(1, 0), new(2, 0) }), "Size自动横条");
            Check(npc.GetBackpackPlacements()[horizontal.Id].SequenceEqual(new[] { new BackpackCell(1, 3), new(2, 3), new(3, 3) }), "横条派生占格");
            var before = npc.GetBackpackPlacements()[horizontal.Id].ToList();
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 4, 2), "越界");
            Check(npc.GetBackpackCardPosition(horizontal.Id) == new BackpackPosition(2, 4) && npc.GetBackpackPlacements()[horizontal.Id].SequenceEqual(before), "失败不修改装配与派生索引");
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 0, 4), "公开零坐标");
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 2, 3), "覆盖主功法");
            var one = npc.GainCard("card_jin_slash");
            npc.PlaceBackpackCard(one.Id, 5, 5);
            Check(npc.GetBackpackCardPosition(one.Id) == new BackpackPosition(5, 5), "右下角摆放");
            var corrupt = npc.GetBackpackSnapshot();
            corrupt.PlacedCards.Single(entry => entry.CardId == horizontal.Id).Position = new(4, 4);
            ExpectInvalid(() => NpcSystemBackpack.Validate(corrupt, npc.GetCombatCardSnapshot()), "非法候选位置");
            Check(npc.GetBackpackCardPosition(horizontal.Id) == new BackpackPosition(2, 4), "布局候选位置独立");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "bad-vertical", Size = 2, Shape = [new(0, 0), new(0, 1)], Keywords = ["Passive"] }), "正式竖条");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "bad-zero", Size = 0, Keywords = ["Passive"] }), "零格不得兜底");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "missing-main", Keywords = ["Item"] }), "创建卡前拒绝零 Main", "missing-main", "缺少 Main");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "two-main", Keywords = ["FaShu", "FaBao"] }), "创建卡前拒绝多 Main", "two-main", "FaShu", "FaBao");
            var passive = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "passive-size-three", Size = 3, Keywords = ["Passive"] });
            NpcSystemCard.ValidateCard(passive);
            Check(!passive.BaseData.HasActiveAbility && passive.GetManaCost().Count == 0, "纯被动不强制主动费用");
            var eventOnly = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "event-fee", Size = 2, Keywords = ["FaBao"], ManaCost = new() { ["Jin"] = 1 } });
            NpcSystemCard.ValidateCard(eventOnly);
            eventOnly.GetManaCost().Clear();
            ExpectInvalid(() => NpcSystemCard.ValidateCard(eventOnly), "主动卡费用不能为空");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine
            {
                ID = "bad-key", Keywords = ["FaBao"], ManaCost = new() { ["None"] = 1, ["Hou"] = 1 }
            }), "未知键不能覆盖通用费", "bad-key", "Hou");
            var legacyKeyword = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "mislabelled-legacy", Keywords = ["Passive", "ManaAdj_Jin"] });
            ExpectInvalid(() => NpcSystemCard.ValidateCard(legacyKeyword), "误标Migrated仍拒绝废弃Keyword");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "legacy-cost", Keywords = ["FaShu"], ManaCost = new() { ["Min"] = 1 } }), "废弃需求不降级无效元素", "Deprecated");
        }

        private static void InstanceAndSnapshotIsolation()
        {
            var definition = Basic();
            var roundtrip = Copy(definition);
            Check(roundtrip.InitCardPlacements.Count == 3 && roundtrip.InitCardPlacements[1].Key == roundtrip.InitCardPlacements[2].Key, "JSON列表保留重复key");
            var a = NpcMgr.Instance.Assemble(roundtrip);
            var b = NpcMgr.Instance.Assemble(roundtrip);
            var duplicates = a.GetAllCards().Where(c => c.DefineId == "card_jin_slash").ToArray();
            Check(duplicates.Length == 2 && duplicates[0].Id != duplicates[1].Id, "同定义独立实例");
            Check(a.GetBackpackCardPosition(duplicates[0].Id) == new BackpackPosition(1, 1) && a.GetBackpackCardPosition(duplicates[1].Id) == new BackpackPosition(1, 2), "重复key独立位置");
            var sa = CombatNpc.CreateCombatNpc(a); sa.InitDeck();
            var sb = CombatNpc.CreateCombatNpc(a); sb.InitDeck();
            var ca = sa.GetField().Single(c => c.Id == duplicates[0].Id);
            var cb = sb.GetField().Single(c => c.Id == duplicates[0].Id);
            var privateLayout = sa.Backpack.GetLayoutSnapshot();
            privateLayout.PlacedCards.Single(entry => entry.CardId == ca.Id).Position = new(5, 5);
            Check(sb.Backpack.GetCardPosition(cb.Id) == new BackpackPosition(1, 1) && a.GetBackpackCardPosition(duplicates[0].Id) == new BackpackPosition(1, 1), "同源战斗布局快照独立");
            a.PlaceBackpackCard(duplicates[0].Id, 5, 1);
            Check(b.GetBackpackCardPosition(b.GetAllCards()[1].Id) == new BackpackPosition(1, 1) && definition.InitCardPlacements[1].Value == new BackpackPosition(1, 1), "装配不改其他NPC或模板");
            var manaCell = sa.Backpack.EmptyCells()[0];
            sa.Backpack.PlaceMana(manaCell, ElementType.Jin); sa.Backpack.TryAssign(manaCell, ca);
            Check(ca.LitCount == 1 && cb.LitCount == 0 && sa.GetField().Single(c => c.Id == duplicates[1].Id).LitCount == 0, "重复实例及同源战斗点亮隔离");
        }

        private static void GeneratedAndPlayerCardSources()
        {
            var generated = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "generated-no-template", InitGongFa = [new() { DefineId = "jin_fa", CurrentPoint = 25 }] });
            Check(generated.GetAllCards().Count(c => c.DefineId == "jin_fa") == 1 && generated.GetAllCards().Any(c => c.DefineId == "card_jin_slash"), "非模板功法解锁发牌保留");
            var player = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "player-no-template" }, NpcAssemblySource.Player);
            CultivationMgr.Instance.AddCultivation(player, "jin_fa", 25);
            Check(player.GetAllCards().Any(c => c.DefineId == "card_jin_slash"), "玩家获得功法发牌保留");
            var equipped = generated.GainEquip("card_fabao_jian", "long_sword"); generated.EquipFaBao(equipped.Id); generated.AssignAllToField();
            generated.EquipFaBao(equipped.Id);
            Check(generated.GetAllCards().Count(c => c.Id != equipped.Id && c.ParentCardId == equipped.Id) == equipped.GetEquip().FormList.Count,
                "非模板装备重复装配只复用本件装备的自带招式");
        }

        private static void RejectAndRollback()
        {
            foreach (var scenario in new (string Name, Action<NpcDefine> Change)[]
            {
                ("缺主功法", d => d.MainGongFaId = ""),
                ("主功法无实例", d => d.MainGongFaId = "huo_fa"),
                ("主功法多实例", d => d.InitCardPlacements.Add(new() { Key = "jin_fa", Value = new(5, 5) })),
                ("主功法错位", d => d.InitCardPlacements[0].Value = new(1, 5)),
                ("缺坐标", d => d.InitCardPlacements[1].Value = null),
                ("缺定义", d => d.InitCardPlacements[1].Key = "does-not-exist"),
                ("重叠", d => d.InitCardPlacements[1].Value = new(3, 3)),
                ("越界", d => d.InitCardPlacements[1].Value = new(6, 2)),
                ("SP不足", d => d.InitStat["ShenShi"] = 2),
                ("SP超空间", d => d.InitStat["ShenShi"] = 26),
                ("混用旧卡组", d => d.InitCardDeck = ["card_jin_slash"]),
                ("待定卡", d => d.InitCardPlacements[1].Key = "card_tu_stun"),
                ("绑定歧义", d => d.InitEquip.Add(new() { CardID = "card_jin_slash", EquipID = "long_sword" })),
                ("绑定序号错误", d => d.InitEquip.Add(new() { CardID = "card_jin_slash", EquipID = "long_sword", PlacementIndex = 1 })),
                ("绑定成功后布局失败", d => { d.InitEquip.Add(new() { CardID = "card_jin_slash", EquipID = "long_sword", PlacementIndex = 2 }); d.InitCardPlacements[2].Value = new(1, 1); })
            })
            {
                var bad = Basic(); scenario.Change(bad);
                var cardIds = CardMgr.Instance.GetAll().Select(c => c.Id).ToHashSet();
                var gongIds = GongFaMgr.Instance.GetAll().Select(c => c.Id).ToHashSet();
                var equipIds = EquipMgr.Instance.GetAll().Select(c => c.Id).ToHashSet();
                var npcIds = NpcMgr.Instance.GetAll().Select(c => c.Id).ToHashSet();
                var registrations = RegistrationCounts();
                try { NpcMgr.Instance.Assemble(bad); throw new Exception("应拒绝 " + scenario.Name); }
                catch (InvalidOperationException ex) { Check(ex.Message.Contains(bad.ID), scenario.Name + "诊断含模板ID"); }
                Check(cardIds.SetEquals(CardMgr.Instance.GetAll().Select(c => c.Id)) && gongIds.SetEquals(GongFaMgr.Instance.GetAll().Select(c => c.Id)) && equipIds.SetEquals(EquipMgr.Instance.GetAll().Select(c => c.Id)) && npcIds.SetEquals(NpcMgr.Instance.GetAll().Select(c => c.Id)), scenario.Name + "撤销全部全局注册");
                Check(registrations.SequenceEqual(RegistrationCounts()), scenario.Name + "撤销全部NPC和Card子系统注册");
            }
        }

        private static int[] RegistrationCounts() => [NpcMgr.Instance.BioSystem.RegisteredCount, NpcMgr.Instance.CardSystem.RegisteredCount,
            NpcMgr.Instance.CultivationSystem.RegisteredCount, NpcMgr.Instance.PositionSystem.RegisteredCount,
            NpcMgr.Instance.TraitSystem.RegisteredCount, NpcMgr.Instance.PersonalitySystem.RegisteredCount,
            NpcMgr.Instance.BehaviorSystem.RegisteredCount, CardMgr.Instance.DataSystem.RegisteredCount,
            CardMgr.Instance.GongFaSystem.RegisteredCount, CardMgr.Instance.EquipSystem.RegisteredCount, CardMgr.Instance.ItemSystem.RegisteredCount];

        private static void FormalTemplates()
        {
            Check(CardDefineMgr.Instance.GetAll().All(c => c.Size > 0), "所有真实旧Size0已明确迁移");
            foreach (var definition in NpcDefineMgr.Instance.GetAll())
            {
                var npc = NpcMgr.Instance.Assemble(definition);
                var layout = npc.ValidateBackpackLayout();
                var expectedLayout = CultivationDefineMgr.Instance.Get(definition.MainGongFaId).GetBackpackDefinition(1);
                Check(layout.Width == expectedLayout.Width && layout.Height == expectedLayout.Height &&
                    layout.GetValidCells().ToHashSet().SetEquals(expectedLayout.GetValidCells()), definition.ID + "装配空间符合主功法配置");
                Check(npc.GetAllCards().Count == definition.InitCardPlacements.Count && npc.GetReserveIds().Count == 0, definition.ID + "仅显式卡上场");
                var combat = CombatNpc.CreateCombatNpc(npc); combat.InitDeck();
                Check(combat.Backpack.Width == expectedLayout.Width && combat.Backpack.Height == expectedLayout.Height &&
                    combat.Backpack.GetCardCells(combat.Backpack.MainCardId).ToHashSet().SetEquals(expectedLayout.MainCardCells),
                    definition.ID + "战斗空间和主功法占格符合配置");
                Check(combat.GetField().Count == definition.InitCardPlacements.Count, definition.ID + "无需辅助布置的真实战斗快照");
                if (definition.ID == "test_npc_a")
                {
                    var sword = combat.GetField().Single(c => c.DefineId == "card_fabao_jian");
                    var forms = combat.GetField().Where(c => c.Id != sword.Id && c.ParentCardId == sword.Id).ToArray();
                    var requiredForms = EquipDefineMgr.Instance.Get("long_sword").FormListBase;
                    Check(forms.Select(c => c.DefineId).OrderBy(id => id, StringComparer.Ordinal)
                        .SequenceEqual(requiredForms.OrderBy(id => id, StringComparer.Ordinal)), "示例武修的长剑招式全部绑定到同一装备实例");
                }
                var free = combat.Backpack.EmptyCells().ToHashSet();
                Check(free.Any(c => free.Contains(new(c.X + 1, c.Y))), definition.ID + "保留完整两格伤口位置");
                Console.WriteLine($"TEMPLATE_LAYOUT_OK id={definition.ID} cards={npc.GetAllCards().Count} sp={npc.GetFieldSize()}/{npc.GetSpMax()} cells={layout.GetValidCells().Count}");
            }
        }

        private static void ExpectInvalid(Action action, string detail, params string[] diagnostic)
        {
            try { action(); }
            catch (InvalidOperationException ex) { Check(diagnostic.All(ex.Message.Contains), detail + "，实际错误：" + ex.Message); return; }
            throw new InvalidOperationException("应拒绝：" + detail);
        }
    }
}
