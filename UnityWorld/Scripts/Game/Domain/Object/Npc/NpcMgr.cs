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
            // 构造 BirthContext，从 Define 预设值
            var ctx = NpcDefineMgr.ToBirthContext(define);
            var npc = ctx.MainNpc = new Npc(Soul.NewId());
            
            BioSystem.OnEntityBorn(ctx);
            PositionSystem.OnEntityBorn(ctx);
            TraitSystem.OnEntityBorn(ctx);
            CardSystem.OnEntityBorn(ctx);
            CultivationSystem.OnEntityBorn(ctx);
            PersonalitySystem.OnEntityBorn(ctx);
            BehaviorSystem.OnEntityBorn(ctx);


            return npc;
        }

        /// <summary>
        /// NPC 诞生：造壳 → GlyphMgr 铭刻 → 各系统 OnEntityBorn → 注册到 _allEntities
        /// </summary>
        public Npc Birth(BirthContext ctx)
        {
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