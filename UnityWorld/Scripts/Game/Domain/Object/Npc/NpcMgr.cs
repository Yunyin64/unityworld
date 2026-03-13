using System.Collections;
using UnityWorld.Game.Common.Math;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC管理器：负责NPC的创建、查找、销毁，以及驱动所有NPC子系统Tick
    /// </summary>
    public class NpcMgr: IDomainMgrBase
    {
        // ── 子系统 ────────────────────────────────────────
        public NpcSystemBio BioSystem { get; } = new();
        public NpcSystemPosition PositionSystem { get; } = new();
        public NpcSystemName NameSystem { get; }
        public NpcSystemRole RoleSystem { get; } = new();
        public NpcSystemTrait   TraitSystem { get; } = new();
        // ── Domain 管理器 ─────────────────────────────────
        public TraitMgr TraitMgr { get; } = new();


        // ── 数据 ─────────────────────────────────────────
        private readonly Dictionary<NpcId, Npc> _allNpcs = new();
        private readonly Rng _rng;


        public static NpcMgr? Instance { get; private set; }


        public NpcMgr(int seed = 12345)
        {
            _rng = new Rng(seed);
            NameSystem = new NpcSystemName(_rng);
            Instance = this;
        }


        /// <summary>注入特质定义数据源，使 TraitMgr 能查询 TraitDefine（初始化时调用）</summary>
        public void SetTraitDefineMgr(IDataMgrBase<TraitDefine> traitDefineMgr) => TraitMgr.SetDefineMgr(traitDefineMgr);


        // ── 创建 ─────────────────────────────────────────

        /// <summary>
        /// 根据配置模板创建一个NPC，并初始化到指定坐标
        /// </summary>
        public Npc Create(NpcDefine define, int x = 0, int y = 0)
        {
            var id = NpcIdGenerator.Generate();
            var npc = new Npc(id);

            // �?随机性别
            var gender = (NpcTypes.Gender)_rng.Range(0, 2); // Male=0 / Female=1

            // �?初始化属性（写入 StatBlock�?         
            float initAge = _rng.Range(define.InitAgeMin, define.InitAgeMax);
            int initCultivation = _rng.Range(define.InitCultivationLevelMin, define.InitCultivationLevelMax + 1);
            npc.Stats.SetBase(StatId.AgeAccumulated, initAge);
            npc.Stats.SetBase(StatId.LifespanMax, define.BaseLifespanMax);
            npc.Stats.SetBase(StatId.TimeFlowRate, 1f);
            npc.Stats.SetBase(StatId.MoveSpeed, define.BaseMoveSpeed);
            npc.Stats.SetBase(StatId.CultivationLevel, initCultivation);

            // �?随机姓名（修�?0则为修士，生成道号）
            bool isCultivator = initCultivation > 0;
            string fullName = isCultivator
                ? NameSystem.RandomCultivatorFullName(gender)
                : NameSystem.RandomName(gender);

            // �?注册到Name系统
            NameSystem.Register(npc, fullName);

            // �?注册到Bio系统
            BioSystem.Register(npc, new NpcBioData
            {
                Name = fullName,
                Gender = gender,
                NpcType = define.NpcType,
            });

            // ⑤ 注册初始角色到 Role 系统
            RoleSystem.Register(npc, define.DefaultRoles);

            // ⑥ 注册到 Position 系统
            PositionSystem.Register(npc , x, y);

            // ⑦ 注册并应用初始 Trait
            TraitMgr.Register(npc.Id);
            if (define.InitialTraits != null)
            {
                foreach (var traitId in define.InitialTraits)
                    TraitMgr.AddTrait(npc, traitId);
            }

            // ⑧ 放入全局表
            _allNpcs[id] = npc;

            return npc;
        }

        // ── 查询 ─────────────────────────────────────────

        public Npc? GetById(NpcId id)
            => _allNpcs.TryGetValue(id, out var npc) ? npc : null;

        public IEnumerable<Npc> GetAll() => _allNpcs.Values;

        public int Count => _allNpcs.Count;

        public string Name => throw new NotImplementedException();

        public string Desc => throw new NotImplementedException();

        // ── Tick ─────────────────────────────────────────


        /// <summary>
        /// 驱动所有NPC的所有子系统Tick（按优先级顺序）
        /// </summary>
        public void Tick(float deltaTime)
        {
            foreach (var npc in _allNpcs.Values)
            {
                BioSystem.OnTick(npc, deltaTime);
                PositionSystem.OnTick(npc, deltaTime);
                TraitMgr.OnTick(npc, deltaTime);
            }
        }

        // ── 工具 ─────────────────────────────────────────

        /// <summary>
        /// 随机获取一个 NPC 配置模板（优先从 NpcDefineMgr 读取，否则回退到内置配置）
        /// </summary>
        public NpcDefine RandomDefine()
        {
            var mgr = NpcDefineMgr.Instance;
            if (mgr != null)
            {
                var random = new Random(_rng.Range(0, int.MaxValue));
                var define = mgr.GetRandom(random);
                if (define != null) return define;
            }
            // 回退：NpcDefineMgr 未初始化或库为空时使用内置模板
            return _rng.Range(0, 2) == 0 ? NpcDefine.HumanFarmer() : NpcDefine.CultivatorWanderer();
        }

        /// <summary>根据 DefineId 获取 NPC 配置模板</summary>
        public NpcDefine? GetDefine(string defineId)
            => NpcDefineMgr.Instance?.Get(defineId);

        public void Init()
        {
            
        }

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }

        public void Render(float dt)
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }

        public IEnumerator Save()
        {
            throw new NotImplementedException();
        }

        public IEnumerator Load()
        {
            throw new NotImplementedException();
        }

    }
}