using System.Collections;
using UnityWorld.Game.Data;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC管理器：负责NPC的创建、查找、销毁，以及驱动所有NPC子系统Tick
    /// </summary>
    public class NpcMgr: DomainMgrBase<Npc>, ISoulBase
    {
        public static NpcMgr Instance { get; private set; }

        // ── 子系统 ────────────────────────────────────────
        public NpcSystemBio BioSystem { get; } = new();
        public NpcSystemCultivation CultivationSystem { get; } = new();
        public NpcSystemCard CardSystem { get; } = new();
        public NpcSystemPosition PositionSystem { get; } = new();
        public NpcSystemBehavior BehaviorSystem { get; } = new();
        public NpcSystemTrait   TraitSystem { get; } = new();
        public NpcSystemPersonality   PersonalitySystem { get; } = new();


        public NpcMgr(int seed = 12345)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        public SoulData Soul {get;set;}

        // ── Tick ─────────────────────────────────────────


        /// <summary>
        /// 驱动所有NPC的所有子系统Tick（按优先级顺序）
        /// </summary>
        public override void Tick(float deltaTime)
        {
            foreach (var npc in _allEntities.Values)
            {
                BioSystem.OnTick(npc, deltaTime);
                PositionSystem.OnTick(npc, deltaTime);
                TraitSystem.OnTick(npc, deltaTime);
                CardSystem.OnTick(npc, deltaTime);
                CultivationSystem.OnTick(npc, deltaTime);
                BehaviorSystem.OnTick(npc, deltaTime);

            }
        }
        public override void Init()
        {
            
        }

        public override void Begin()
        {
            
        }

        public override void Update()
        {
            
        }

        public override void Render(float dt)
        {
            
        }

        public override void End()
        {
            
        }

    public override IEnumerator Save()
        {
            yield break;
        }

    public override IEnumerator Load()
        {
            yield break;
        }

        /// <summary>
        /// 从 NpcDefine 模板直接组装 NPC（无因果，不走 Birth/GlyphMgr）。
        /// 适用于妖兽等模板实体，战斗结束后可直接 Remove。
        /// </summary>
        public Npc Assemble(NpcDefine define)
        {
            var ctx = NpcDefineMgr.ToBirthContext(define);
            ctx.AssemblySource = NpcAssemblySource.Template;
            var npc = ctx.MainNpc = new Npc(Soul.NewId());
            try
            {
                InitializeSystems(ctx);
                AssembleTemplateBackpack(npc, define);
                return npc;
            }
            catch (Exception ex)
            {
                CleanupFailedAssembly(npc);
                throw new InvalidOperationException($"NPC 模板 {define.ID} 装配失败：{ex.Message}", ex);
            }
        }

        /// <summary>玩家/生成 NPC 及专用测试的显式非模板入口，保留旧自动发牌能力。</summary>
        public Npc AssembleGenerated(NpcDefine define, NpcAssemblySource source = NpcAssemblySource.Generated)
        {
            if (source == NpcAssemblySource.Template) throw new InvalidOperationException("模板必须通过 Assemble 完整校验。");
            var ctx = NpcDefineMgr.ToBirthContext(define);
            ctx.AssemblySource = source;
            ctx.MainNpc = new Npc(Soul.NewId());
            InitializeSystems(ctx);
            return ctx.MainNpc;
        }

        private void InitializeSystems(BirthContext ctx)
        {
            BioSystem.OnEntityBorn(ctx); PositionSystem.OnEntityBorn(ctx);
            TraitSystem.OnEntityBorn(ctx); CardSystem.OnEntityBorn(ctx);
            CultivationSystem.OnEntityBorn(ctx); PersonalitySystem.OnEntityBorn(ctx);
            BehaviorSystem.OnEntityBorn(ctx);
        }

        private void AssembleTemplateBackpack(Npc npc, NpcDefine define)
        {
            if (define.InitCardDeck.Length != 0)
                throw new InvalidOperationException("模板不得混用 InitCardDeck 与 InitCardPlacements；每个上场实例必须有明确位置。");
            if (string.IsNullOrWhiteSpace(define.MainGongFaId)) throw new InvalidOperationException("缺少 MainGongFaId。");
            var data = CardSystem.GetData(npc.Id);
            foreach (var (entry, index) in define.InitCardPlacements.Select((entry, index) => (entry, index)))
            {
                if (entry.Value == null) throw new InvalidOperationException($"位置条目 {index + 1} ({entry.Key}) 缺少坐标。");
                BackpackCoordinates.ToCell(entry.Value.Value);
                if (CardDefineMgr.Instance.Get(entry.Key) == null)
                    throw new InvalidOperationException($"位置条目 {index + 1} 引用不存在的卡 {entry.Key}。");
                var card = npc.GainCard(entry.Key);
                card.Position = entry.Value;
                data.Field.Add(card.Id);
                NpcBackpackLayout.ValidateCard(card);
            }
            var mains = data.AllCards.Where(c => c.DefineId == define.MainGongFaId).ToList();
            if (mains.Count != 1) throw new InvalidOperationException($"主功法 {define.MainGongFaId} 必须唯一对应位置列表实例，实际 {mains.Count} 张。");

            Card ResolveBinding(string cardId, int? placementIndex)
            {
                if (placementIndex is int index)
                {
                    if (index < 1 || index > data.AllCards.Count || data.AllCards[index - 1].DefineId != cardId)
                        throw new InvalidOperationException($"绑定 {cardId} 的 PlacementIndex={index} 不匹配位置条目。");
                    return data.AllCards[index - 1];
                }
                var candidates = data.AllCards.Where(c => c.DefineId == cardId).ToList();
                if (candidates.Count != 1) throw new InvalidOperationException($"绑定 {cardId} 匹配 {candidates.Count} 张卡，重复定义必须指定 PlacementIndex。");
                return candidates[0];
            }

            var progress = new Dictionary<int, int>();
            foreach (var entry in define.InitGongFa)
            {
                var card = ResolveBinding(entry.DefineId, entry.PlacementIndex);
                if (!progress.TryAdd(card.Id, entry.CurrentPoint)) throw new InvalidOperationException($"功法 {entry.DefineId} 的同一实例被重复绑定。");
                if (CultivationDefineMgr.Instance.Get(entry.DefineId) == null) throw new InvalidOperationException($"不存在功法 {entry.DefineId}。");
            }
            foreach (var card in data.AllCards)
            {
                var cultivation = CultivationDefineMgr.Instance.Get(card.DefineId);
                if (cultivation == null) continue;
                var gongfa = GongFaMgr.Instance.InstantiateFromDefine(card.Id, cultivation);
                gongfa.CurrentPoint = Math.Clamp(progress.GetValueOrDefault(card.Id), 0, cultivation.MaxPoint);
                npc.AddGongFa(gongfa);
            }
            foreach (var entry in define.InitEquip)
            {
                var card = ResolveBinding(entry.CardID, entry.PlacementIndex);
                if (EquipMgr.Instance.GetById(card.Id) != null) throw new InvalidOperationException($"装备实例 {card.DefineId}/{card.Id} 被重复绑定。");
                var equipDefine = EquipDefineMgr.Instance.Get(entry.EquipID)
                    ?? throw new InvalidOperationException($"不存在装备定义 {entry.EquipID}。");
                var equip = Equip.FromDefine(equipDefine);
                equip.Id = card.Id;
                EquipMgr.Instance.Add(card.Id, equip);
                npc.EquipFaBao(card.Id);
            }
            for (int i = 0; i < define.InitCardPlacements.Count; i++)
            {
                if (define.InitCardPlacements[i].ParentPlacementIndex is not int parentIndex) continue;
                if (parentIndex < 1 || parentIndex > data.AllCards.Count || parentIndex == i + 1 ||
                    EquipMgr.Instance.GetById(data.AllCards[parentIndex - 1].Id) == null)
                    throw new InvalidOperationException($"位置条目 {i + 1} 的父装备序号 {parentIndex} 无效。");
                data.AllCards[i].ParentCardId = data.AllCards[parentIndex - 1].Id;
            }
            data.MainGongFaCardId = mains[0].Id;
            NpcBackpackLayout.RebuildIndex(data);
            var layout = NpcBackpackLayout.Validate(data);
            int occupied = npc.GetFieldSize(), spMax = npc.GetSpMax(), capacity = layout.GetValidCells().Count;
            if (spMax > capacity || occupied > spMax)
                throw new InvalidOperationException($"SP 容量错误：上场占格 {occupied}，SP 上限 {spMax}，背包 {capacity}。");
        }

        private void CleanupFailedAssembly(Npc npc)
        {
            if (CardSystem.Contains(npc.Id))
                foreach (var card in CardSystem.GetData(npc.Id).AllCards)
                    CardMgr.Instance.UnregisterCard(card.Id);
            BioSystem.Unregister(npc.Id); PositionSystem.Unregister(npc.Id); TraitSystem.Unregister(npc.Id);
            CardSystem.Unregister(npc.Id); CultivationSystem.Unregister(npc.Id);
            PersonalitySystem.Unregister(npc.Id); BehaviorSystem.Unregister(npc.Id);
            StatMgr.Instance.RemoveBlock(npc.Id, typeof(Npc));
            Remove(npc.Id);
        }

        /// <summary>
        /// NPC 诞生：造壳 → GlyphMgr 铭刻 → 各系统 OnEntityBorn → 注册到 _allEntities
        /// </summary>
        public Npc Birth(BirthContext ctx)
        {
            if (ctx.AssemblySource == NpcAssemblySource.Template)
                throw new InvalidOperationException("模板通过 Assemble 进入，Birth 只处理玩家/生成来源。");
            // 1. 造壳：分配唯一 ID，创建 Npc 实例
            ctx.MainNpc = new Npc(Soul.NewId());

            // 2. 玩法系统介入：铭刻姓名、性别等简单键值到 ctx kv
            GlyphMgr.Instance.GeneratorNpc(ctx);

            // 3. 各系统 OnEntityBorn（与 Tick 顺序一致）
            BioSystem.OnEntityBorn(ctx);
            PositionSystem.OnEntityBorn(ctx);
            TraitSystem.OnEntityBorn(ctx);
            CardSystem.OnEntityBorn(ctx);
            CultivationSystem.OnEntityBorn(ctx);
            PersonalitySystem.OnEntityBorn(ctx);
            BehaviorSystem.OnEntityBorn(ctx);

            // 4. 注册到 _allEntities，NPC 正式进入世界接受 Tick
            Add(ctx.MainNpc.Id, ctx.MainNpc);

            return ctx.MainNpc;
        }
    }
}
