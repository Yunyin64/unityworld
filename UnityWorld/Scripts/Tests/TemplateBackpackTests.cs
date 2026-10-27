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
            CoordinatesAndShapes();
            InstanceAndSnapshotIsolation();
            BindingAndSourceIsolation();
            RejectAndRollback();
            FormalTemplates();
            Console.WriteLine($"TEMPLATE_BACKPACK_TESTS_OK assertions={_assertions}");
        }

        private static void CoordinatesAndShapes()
        {
            Check(BackpackCoordinates.ToCell(new(1, 1)) == new BackpackCell(0, 0), "公开(1,1)转换");
            Check(BackpackCoordinates.ToPosition(new(4, 4)) == new BackpackPosition(5, 5), "公开(5,5)转换");
            var npc = NpcMgr.Instance.Assemble(Basic());
            var horizontal = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "size-three-geometry", Size = 3 });
            npc.GainCard(horizontal);
            npc.PlaceBackpackCard(horizontal.Id, 2, 4);
            Check(horizontal.Position == new BackpackPosition(2, 4), "实例保存公开坐标");
            Check(horizontal.BaseData.Shape.SequenceEqual(new[] { new BackpackCell(0, 0), new(1, 0), new(2, 0) }), "Size自动横条");
            Check(npc.GetCardLoadoutSnapshot().Placements[horizontal.Id].SequenceEqual(new[] { new BackpackCell(1, 3), new(2, 3), new(3, 3) }), "横条派生占格");
            var before = npc.GetCardLoadoutSnapshot().Placements[horizontal.Id].ToList();
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 4, 2), "越界");
            Check(horizontal.Position == new BackpackPosition(2, 4) && npc.GetCardLoadoutSnapshot().Placements[horizontal.Id].SequenceEqual(before), "失败不修改实例与索引");
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 0, 4), "公开零坐标");
            ExpectInvalid(() => npc.PlaceBackpackCard(horizontal.Id, 2, 3), "覆盖主功法");
            var one = npc.GainCard("card_jin_slash");
            npc.PlaceBackpackCard(one.Id, 5, 5);
            Check(one.Position == new BackpackPosition(5, 5), "右下角摆放");
            var corrupt = npc.GetCardLoadoutSnapshot();
            corrupt.AllCards.Single(c => c.Id == horizontal.Id).Position = new(1, 4);
            ExpectInvalid(() => NpcBackpackLayout.Validate(corrupt), "索引与实例不一致");
            Check(horizontal.Position == new BackpackPosition(2, 4), "布局候选位置独立");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "bad-vertical", Size = 2, Shape = [new(0, 0), new(0, 1)] }), "正式竖条");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "bad-zero", Size = 0 }), "零格不得兜底");
            var passive = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "passive-size-three", Size = 3, Keywords = ["Passive"] });
            NpcBackpackLayout.ValidateCard(passive);
            Check(!passive.BaseData.HasActiveAbility && passive.GetManaCost().Count == 0, "纯被动不强制主动费用");
            var eventOnly = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "event-fee", Size = 2, Keywords = ["FaBao"], ManaCost = new() { ["Jin"] = 1 } });
            ExpectInvalid(() => NpcBackpackLayout.ValidateCard(eventOnly), "纯法宝也校验费用等于Size");
            var legacyKeyword = CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "mislabelled-legacy", Keywords = ["ManaAdj_Jin"] });
            ExpectInvalid(() => NpcBackpackLayout.ValidateCard(legacyKeyword), "误标Migrated仍拒绝废弃Keyword");
            ExpectInvalid(() => CardMgr.Instance.InstantiateFromDefine(new CardDefine { ID = "legacy-cost", ManaCost = new() { ["Min"] = 1 } }), "废弃需求不降级无效元素");
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
            Check(duplicates[0].Position == new BackpackPosition(1, 1) && duplicates[1].Position == new BackpackPosition(1, 2), "重复key独立位置");
            var sa = CombatNpc.CreateCombatNpc(a); sa.InitDeck();
            var sb = CombatNpc.CreateCombatNpc(a); sb.InitDeck();
            var ca = sa.GetField().Single(c => c.Id == duplicates[0].Id);
            var cb = sb.GetField().Single(c => c.Id == duplicates[0].Id);
            ca.Position = new(5, 5);
            Check(cb.Position == new BackpackPosition(1, 1) && duplicates[0].Position == new BackpackPosition(1, 1), "同源战斗位置独立");
            a.PlaceBackpackCard(duplicates[0].Id, 5, 1);
            Check(b.GetAllCards()[1].Position == new BackpackPosition(1, 1) && definition.InitCardPlacements[1].Value == new BackpackPosition(1, 1), "实例摆放不改其他NPC或模板");
            var manaCell = sa.Backpack.EmptyCells()[0];
            sa.Backpack.PlaceMana(manaCell, ElementType.Jin); sa.Backpack.TryAssign(manaCell, ca);
            Check(sa.Backpack.AssignedTo(ca.Id).Count == 1 && sb.Backpack.AssignedTo(cb.Id).Count == 0 && sa.Backpack.AssignedTo(duplicates[1].Id).Count == 0, "重复实例及同源战斗点亮隔离");
        }

        private static void BindingAndSourceIsolation()
        {
            var definition = Basic();
            definition.InitCardPlacements.Add(new() { Key = "card_fabao_jian", Value = new(1, 4) });
            definition.InitCardPlacements.Add(new() { Key = "card_fabao_jian", Value = new(4, 4) });
            definition.InitEquip = [new() { CardID = "card_fabao_jian", EquipID = "long_sword", PlacementIndex = 4 }, new() { CardID = "card_fabao_jian", EquipID = "great_sword", PlacementIndex = 5 }];
            var npc = NpcMgr.Instance.Assemble(definition);
            var cards = npc.GetAllCards();
            Check(cards.Count == definition.InitCardPlacements.Count && cards.Count(c => c.DefineId == "jin_fa") == 1, "仅配置清单造卡，无重复主卡/解锁子卡");
            Check(cards[0].GetGongFa().CurrentPoint == 100, "绑定已有主卡修炼进度");
            Check(cards[3].GetEquip().Attack == 4 && cards[4].GetEquip().Attack == 8 && cards[3].GetEquip().Id != cards[4].GetEquip().Id, "同定义装备绑定不同实例属性");
            Check(!cards.Any(c => c.DefineId.StartsWith("card_form_")), "装备FormList不自动造招式");
            var generated = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "generated-no-template", InitGongFa = [new() { DefineId = "jin_fa", CurrentPoint = 25 }] });
            Check(generated.GetAllCards().Count(c => c.DefineId == "jin_fa") == 1 && generated.GetAllCards().Any(c => c.DefineId == "card_jin_slash"), "非模板功法解锁发牌保留");
            var player = NpcMgr.Instance.AssembleGenerated(new NpcDefine { ID = "player-no-template" }, NpcAssemblySource.Player);
            CultivationMgr.Instance.AddCultivation(player, "jin_fa", 25);
            Check(player.GetAllCards().Any(c => c.DefineId == "card_jin_slash"), "玩家获得功法发牌保留");
            var equipped = generated.GainEquip("card_fabao_jian", "long_sword"); generated.EquipFaBao(equipped.Id); generated.AssignAllToField();
            Check(generated.GetAllCards().Any(c => c.DefineId == "card_form_jian_ci" && c.ParentCardId == equipped.Id), "非模板既有装备附带卡路径保留");
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
                ("旧机制卡", d => d.InitCardPlacements[1].Key = "card_jin_needle"),
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
                Check(layout.Width == 5 && layout.Height == 5, definition.ID + "默认5x5");
                Check(npc.GetAllCards().Single(c => c.Id == npc.GetCardLoadoutSnapshot().MainGongFaCardId).Position == new BackpackPosition(3, 3), definition.ID + "主功法中心");
                Check(npc.GetAllCards().Count == definition.InitCardPlacements.Count && npc.GetReserveIds().Count == 0, definition.ID + "仅显式卡上场");
                var combat = CombatNpc.CreateCombatNpc(npc); combat.InitDeck();
                Check(combat.GetField().Count == definition.InitCardPlacements.Count, definition.ID + "无需辅助布置的真实战斗快照");
                var free = combat.Backpack.EmptyCells().ToHashSet();
                Check(free.Any(c => free.Contains(new(c.X + 1, c.Y))), definition.ID + "保留完整两格伤口位置");
                Console.WriteLine($"TEMPLATE_LAYOUT_OK id={definition.ID} cards={npc.GetAllCards().Count} sp={npc.GetFieldSize()}/{npc.GetSpMax()} cells={layout.GetValidCells().Count}");
            }
        }

        private static void ExpectInvalid(Action action, string detail)
        {
            try { action(); }
            catch (InvalidOperationException) { _assertions++; return; }
            throw new InvalidOperationException("应拒绝：" + detail);
        }
    }
}
