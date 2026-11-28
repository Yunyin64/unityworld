using System.Collections;
using UnityWorld.Game.Data;
using UnityWorld.Core;
using UnityWorld.Game.World;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC管理器：负责NPC的创建、查找、销毁，以及驱动所有NPC子系统Tick
    /// </summary>
    public class NpcMgr: DomainMgrBase<Npc>, ISoulBase
    {
        /// <summary>当前角色管理器</summary>
        public static NpcMgr Instance { get; private set; }

        // ── 子系统 ────────────────────────────────────────
        /// <summary>角色生理系统</summary>
        public NpcSystemBio BioSystem { get; } = new();
        /// <summary>角色修行系统</summary>
        public NpcSystemCultivation CultivationSystem { get; } = new();
        /// <summary>角色持卡系统</summary>
        public NpcSystemCard CardSystem { get; } = new();
        /// <summary>角色备战背包系统</summary>
        public NpcSystemBackpack BackpackSystem { get; } = new();
        /// <summary>角色位置系统</summary>
        public NpcSystemPosition PositionSystem { get; } = new();
        /// <summary>角色行为系统</summary>
        public NpcSystemBehavior BehaviorSystem { get; } = new();
        /// <summary>角色特质系统</summary>
        public NpcSystemTrait   TraitSystem { get; } = new();
        /// <summary>角色性格系统</summary>
        public NpcSystemPersonality   PersonalitySystem { get; } = new();


        /// <summary>以种子初始化基础实体数据并登记管理器</summary>
        public NpcMgr(int seed = 12345)
        {
            Soul = new SoulData(seed);
            Instance = this;
        }

        /// <summary>管理器的基础实体数据</summary>
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
                BackpackSystem.OnTick(npc, deltaTime);
                CultivationSystem.OnTick(npc, deltaTime);
                BehaviorSystem.OnTick(npc, deltaTime);

            }
        }
        /// <summary>保留管理器初始化入口</summary>
        public override void Init()
        {
            
        }

        /// <summary>保留世界启动回调</summary>
        public override void Begin()
        {
            
        }

        /// <summary>保留帧更新回调</summary>
        public override void Update()
        {
            
        }

        /// <summary>保留渲染回调</summary>
        public override void Render(float dt)
        {
            
        }

        /// <summary>注销全部角色，清理位置、行为和背包数据</summary>
        public override void End()
        {
            // 包括失败创建和未落地对象；新世界不继承旧位置或周移动。
            foreach (var id in _allEntities.Keys.ToArray()) RemoveCore(id);
            PositionSystem.Clear(); BehaviorSystem.Clear();
            BackpackSystem.Clear();
        }

    /// <summary>结束存档流程，当前无写入</summary>
    public override IEnumerator Save()
        {
            yield break;
        }

    /// <summary>结束读档流程，当前无读取</summary>
    public override IEnumerator Load()
        {
            yield break;
        }

        /// <summary>
        /// 从 NpcDefine 模板直接组装 NPC（无因果，不走 Birth/GlyphMgr）。
        /// 适用于妖兽等模板实体，战斗结束后可直接 Remove。
        /// </summary>
        public Npc Assemble(NpcDefine define, Dictionary<string, string>? appearance = null)
        {
            Npc npc = null;
            try
            {
                ValidateTemplateGeometry(define);
                var ctx = NpcDefineMgr.ToBirthContext(define);
                // 试战的冻结选择只覆盖本次装配，无需改写正式模板。
                if (appearance != null) ctx.Set("Appearance", appearance);
                ctx.AssemblySource = NpcAssemblySource.Template;
                npc = ctx.MainNpc = new Npc(Soul.NewId());
                InitializeSystems(ctx);
                AssembleTemplateBackpack(npc, define);
                return npc;
            }
            catch (Exception ex)
            {
                if (npc != null) CleanupFailedAssembly(npc);
                throw new InvalidOperationException($"NPC 模板 {define.ID} 装配失败：{ex.Message}", ex);
            }
        }

        /// <summary>玩家/生成 NPC 及专用测试的显式非模板入口，保留旧自动发牌能力。</summary>
        public Npc AssembleGenerated(NpcDefine define, NpcAssemblySource source = NpcAssemblySource.Generated)
        {
            if (source == NpcAssemblySource.Template) throw new InvalidOperationException("模板必须通过 Assemble 装配。");
            var ctx = NpcDefineMgr.ToBirthContext(define);
            ctx.AssemblySource = source;
            ctx.MainNpc = new Npc(Soul.NewId());
            try
            {
                InitializeSystems(ctx);
                return ctx.MainNpc;
            }
            catch
            {
                CleanupFailedAssembly(ctx.MainNpc);
                throw;
            }
        }

        /// <summary>依次初始化角色的生理、位置及其余领域数据</summary>
        private void InitializeSystems(BirthContext ctx)
        {
            BioSystem.OnEntityBorn(ctx); PositionSystem.OnEntityBorn(ctx);
            BackpackSystem.OnEntityBorn(ctx);
            TraitSystem.OnEntityBorn(ctx); CardSystem.OnEntityBorn(ctx);
            CultivationSystem.OnEntityBorn(ctx); PersonalitySystem.OnEntityBorn(ctx);
            BehaviorSystem.OnEntityBorn(ctx);
        }

        // 在分配实体 ID 之前，用与卡实例和编辑器一致的解析检查模板几何。
        /// <summary>检查模板的摆放定义及主功法配置</summary>
        private static void ValidateTemplateGeometry(NpcDefine define)
        {
            if (define.InitCardDeck.Length != 0)
                throw new InvalidOperationException("模板不得混用 InitCardDeck 与 InitCardPlacements；每个上场实例必须有明确位置。");
            if (string.IsNullOrWhiteSpace(define.MainGongFaId)) throw new InvalidOperationException("缺少 MainGongFaId。");
            if (define.InitCardPlacements.Count(entry => entry?.Key == define.MainGongFaId) != 1)
                throw new InvalidOperationException($"主功法 {define.MainGongFaId} 必须唯一对应位置列表实例。");
            var cultivation = CultivationDefineMgr.Instance.Get(define.MainGongFaId)
                ?? throw new InvalidOperationException($"不存在功法 {define.MainGongFaId}。");
            var layout = cultivation.GetBackpackDefinition(1);
            var valid = layout.GetValidCells();
            var occupied = new HashSet<BackpackCell>();
            foreach (var (entry, index) in define.InitCardPlacements.Select((entry, index) => (entry, index)))
            {
                if (entry?.Value is not BackpackPosition position)
                    throw new InvalidOperationException($"位置条目 {index + 1} ({entry?.Key}) 缺少坐标。");
                var card = CardDefineMgr.Instance.Get(entry.Key)
                    ?? throw new InvalidOperationException($"位置条目 {index + 1} 引用不存在的卡 {entry.Key}。");
                var cells = BackpackCoordinates.GetCells(position, card.GetShape());
                if (entry.Key == define.MainGongFaId && !layout.MainCardCells.ToHashSet().SetEquals(cells))
                    throw new InvalidOperationException($"主功法 {entry.Key} 位置 {position} 与固定格冲突。");
                if (cells.Any(cell => !valid.Contains(cell)))
                    throw new InvalidOperationException($"卡 {entry.Key} 位置 {position} 越出背包有效空间。");
                if (cells.Any(cell => !occupied.Add(cell)))
                    throw new InvalidOperationException($"卡 {entry.Key} 位置 {position} 与其他卡重叠。");
            }
        }

        /// <summary>按模板创建卡牌和占格，绑定主功法及装备</summary>
        private void AssembleTemplateBackpack(Npc npc, NpcDefine define)
        {
            var data = CardSystem.GetData(npc.Id);
            var backpack = BackpackSystem.GetData(npc.Id);
            foreach (var entry in define.InitCardPlacements)
            {
                var card = npc.GainCard(entry.Key);
                // JSON 仍按原格式读取，坐标写入装配，卡实例只保存自身属性。
                backpack.PlacedCards.Add(new() { CardId = card.Id, Position = entry.Value });
                data.Field.Add(card.Id);
                NpcSystemCard.ValidateCard(card);
            }
            var mains = data.AllCards.Where(c => c.DefineId == define.MainGongFaId).ToList();
            if (mains.Count != 1) throw new InvalidOperationException($"主功法 {define.MainGongFaId} 必须唯一对应位置列表实例，实际 {mains.Count} 张。");

            // 按摆放下标或唯一的卡定义解析绑定对象
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
                // 模板中的招式实例由显式位置条目创建，装备登记不再额外发牌。
                data.EquippedFaBao.Add(card.Id);
            }
            for (int i = 0; i < define.InitCardPlacements.Count; i++)
            {
                if (define.InitCardPlacements[i].ParentPlacementIndex is not int parentIndex) continue;
                if (parentIndex < 1 || parentIndex > data.AllCards.Count || parentIndex == i + 1 ||
                    EquipMgr.Instance.GetById(data.AllCards[parentIndex - 1].Id) == null)
                    throw new InvalidOperationException($"位置条目 {i + 1}（{data.AllCards[i].DefineId}）的父装备序号 {parentIndex} 无效。");
                data.AllCards[i].ParentCardId = data.AllCards[parentIndex - 1].Id;
            }
            BackpackSystem.InitializeTemplateLayout(npc, mains[0].Id);
            npc.ValidateBackpackLayout();
        }

        /// <summary>清理模板装配失败留下的角色及系统数据</summary>
        private void CleanupFailedAssembly(Npc npc)
            => RemoveCore(npc.Id, npc);

        /// <summary>清理世界角色出生失败留下的组件和索引</summary>
        internal void CleanupFailedWorldBirth(Npc npc) => RemoveCore(npc.Id, npc);

        /// <summary>检查世界可修改后注销指定角色</summary>
        public override void Remove(int id)
        {
            WorldMgr.EnsureMutable();
            RemoveCore(id);
        }

        /// <summary>解除角色登记并清理持卡、位置和各系统数据</summary>
        private void RemoveCore(int id, Npc knownNpc = null)
        {
            // 正常注销和失败装配共用清理，不再只删除实体字典中的壳。
            bool hadData = knownNpc != null || GetById(id) != null || BioSystem.Contains(id) || PositionSystem.Contains(id);
            if (CardSystem.Contains(id))
                foreach (var card in CardSystem.GetData(id).AllCards)
                    CardMgr.Instance?.UnregisterCard(card.Id);
            // 探索获得的行为卡由持卡管理器保存，角色注销时一并释放。
            BehaviorCardMgr.Instance?.RemoveNpcCards(id);
            BioSystem.Unregister(id); PositionSystem.Unregister(id); TraitSystem.Unregister(id);
            CardSystem.Unregister(id); CultivationSystem.Unregister(id);
            BackpackSystem.Unregister(id);
            PersonalitySystem.Unregister(id); BehaviorSystem.Unregister(id);
            if (hadData) StatMgr.Instance.RemoveBlock(id, typeof(Npc));
            base.Remove(id);
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
            BackpackSystem.OnEntityBorn(ctx);
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
