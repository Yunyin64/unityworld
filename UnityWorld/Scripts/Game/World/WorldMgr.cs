using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using UnityWorld.Core;

namespace UnityWorld.Game.World
{
    /// <summary>
    /// World管理器：统一Tick入口，持有所有顶层子系统
    /// </summary>
    public static class WorldMgr
    {
        public static List<IDomainMgrBase> _domains = new();  // 统一管理列表，方便统一Tick
        public static List<IGameplayMgrBase> _gameplays = new();  // 统一gameplay列表，方便统一Tick

        public static bool IsAdvancingWeek { get; private set; }
        private static bool _weeklyReady;
        private static bool _weeklyFaulted;
        public static WorldWeekResult LastWeekResult { get; private set; }
        public static event Action<WorldWeekResult> WeekCompleted;

        internal static void EnsureMutable()
        {
            if (IsAdvancingWeek || _weeklyFaulted)
                throw new InvalidOperationException("世界正在周结算或已发生提交故障，当前不能修改。");
        }

        /// <summary>明确推进世界一周；日期与NPC一起结算，绝不折算成秒更新。</summary>
        public static WorldWeekResult AdvanceWeek()
        {
            if (!_weeklyReady) throw new InvalidOperationException("世界尚未初始化。");
            EnsureMutable();
            IsAdvancingWeek = true;
            bool committed = false;
            var before = WorldTime.CurrentDate;
            var results = new List<NpcWeekResult>();
            try
            {
                NpcSystemPosition.ValidateExplorationDefinitions();
                long week = checked(before.ElapsedWeeks + 1);
                _ = new WorldDate(week);
                // 世界只快照各位面成员；不建立全局参与名单或逐NPC移动计划。
                var planes = PlaneMgr.Instance.GetAll().OrderBy(plane => plane.Id.Value)
                    .Select(plane => (Plane: plane, Npcs: plane.NpcSystem.GetWeeklyNpcIds())).ToArray();
                var transition = WorldTime.AdvanceWeek();
                committed = true;
                foreach (var item in planes) item.Plane.NpcSystem.AdvanceWeek(week, item.Npcs, results);
                LastWeekResult = new(transition.Before, transition.After, true, results.AsReadOnly(), "");
                var observers = WeekCompleted;
                if (observers != null)
                    foreach (Action<WorldWeekResult> observer in observers.GetInvocationList())
                        try { observer(LastWeekResult); }
                        catch (Exception ex)
                        {
                            try { LogMgr.Instance?.Warn("世界周完成观察者异常，本周已完成：{0}", ex); }
                            catch { /* 诊断回调不能把成功周变成失败。 */ }
                        }
                return LastWeekResult;
            }
            catch (Exception ex) when (committed)
            {
                _weeklyFaulted = true;
                LastWeekResult = new(before, WorldTime.CurrentDate, false, results.AsReadOnly(), ex.ToString());
                return LastWeekResult;
            }
            finally { IsAdvancingWeek = false; }
        }

        // ── 元气系统 ──────────────────────────────────────

        /// <summary>元气天道管理器：持有原初快照、计算五行收支偏差、暴露失衡权重</summary>
        // 只在Initialize中创建。独立位面调用保护方法时，不能替换它已安装的天道实例。
        public static AuraDaoMgr AuraDaoMgr { get; private set; }

        /// <summary>地块元气系统：每 Tick 将 TileModifier 累积到 Tile.CurrentAura 并清理过期修正</summary>
        public static TileSystemAura TileSystemAura { get; private set; } = new();

        /// <summary>
        /// 初始化世界（游戏启动时调用）：
        ///   1. 加载外部数据定义（GameDataMgr）
        ///   2. 初始化全部地图查询、地区、地标及天道依赖
        ///   3. 候选主大陆完成校验、初值与快照后原子发布
        ///   4. 成功后初始化世界时间；任何失败不推进或重置旧时间
        /// </summary>
        public static void Initialize(int seed = 12345, string baseDir = null, string mapProfileId = "china-inspired-v1")
        {
            if (_domains.Count != 0 || _gameplays.Count != 0)
                throw new InvalidOperationException("世界已经初始化，请先调用 WorldMgr.End()。");

            baseDir ??= AppContext.BaseDirectory;
            if (!Directory.Exists(Path.Combine(baseDir, "Data")))
                throw new DirectoryNotFoundException($"找不到核心数据目录：{Path.Combine(baseDir, "Data")}");

            try
            {
                _domains.Add(new LogMgr(seed));
                _domains.Add(new LuaMgr(baseDir));
                _domains.Add(new APIMgr());
                _domains.Add(new EventMgr());


                GameDataMgr.Initialize(baseDir);
                _gameplays.Add(new GlyphMgr(seed, baseDir));
                AuraDaoMgr = new AuraDaoMgr(seed);
                _gameplays.Add(AuraDaoMgr);
                _gameplays.Add(new CombatMgr(seed));
                _gameplays.Add(new CultivationMgr(seed));
            
            
            
                _domains.Add(new StatMgr(seed));
                _domains.Add(new NpcMgr(seed));
                _domains.Add(new TileMgr());
                _domains.Add(new RegionMgr());
                var landMarks = new LandMarkMgr();
                landMarks.SetCatalog(GameDataMgr.Instance.MapCatalog);
                landMarks.SetRng(new Rng(seed));
                _domains.Add(landMarks);
                _domains.Add(new PlaneMgr());

                // ── 叙事系统 ──────────────────────────────────────
                _domains.Add(new StoryMgr(seed));
                _domains.Add(new BehaviorCardMgr());
                _domains.Add(new CardMgr(seed));
                _domains.Add(new GongFaMgr(seed));
                _domains.Add(new EquipMgr());
                _domains.Add(new ItemMgr(seed));

                foreach (var mgr in _domains) mgr.Init();  // 注册依赖，不在 PlaneMgr.Init 中生成主世界。
                foreach (var mgr in _gameplays) mgr.Init();
                PlaneMgr.Instance.CreateMainPlane(seed, mapProfileId);
                // 所有可能失败的加载与地图发布成功之后才建立新世界时间。
                WorldTime.Initialize(seed);
                _weeklyReady = true;
                _weeklyFaulted = false;
            }
            catch
            {
                End();
                throw;
            }
        }

        public static void Start()
        {
            foreach (var mgr in _domains) mgr.Begin();  // 有耦合的初始化
            foreach (var mgr in _gameplays) mgr.Begin();
            
            LogMgr.Instance.Dbg("=== 世界初始化完成 ===\n");
        }

        /// <summary>宿主退出时结束各系统，释放 Lua、事件与日志资源。</summary>
        public static void End()
        {
            if (IsAdvancingWeek)
                throw new InvalidOperationException("世界周结算期间不能销毁世界。");
            // 先让位置系统解绑位面成员，再按逆序销毁位面与管理器。
            _domains.OfType<NpcMgr>().FirstOrDefault()?.PositionSystem.Clear();
            _weeklyReady = false; _weeklyFaulted = false;
            WeekCompleted = null; LastWeekResult = null;
            for (int i = _gameplays.Count - 1; i >= 0; i--)
                _gameplays[i].End();
            for (int i = _domains.Count - 1; i >= 0; i--)
                _domains[i].End();
            _gameplays.Clear();
            _domains.Clear();
        }

        /// <summary>
        /// 全局Tick入口：按优先级顺序驱动所有子系统
        /// 顺序：WorldTime → PlaneMgr → NpcMgr → TileSystemAura → AuraDaoMgr → (后续系统...)
        /// </summary>
        public static void Tick(float deltaTime)
        {
            if (IsAdvancingWeek)
                throw new InvalidOperationException("世界周结算期间不能执行秒更新。");
            // ① 推进世界物理时间
            WorldTime.Advance(deltaTime);

            foreach (var mgr in _domains) mgr.Tick(deltaTime);  // 统一驱动所有注册的子系统
            foreach (var mgr in _gameplays) mgr.Tick(deltaTime);

            // ② 元气系统：先累积 Modifier 到 CurrentAura，再让天道感知最新状态
            var planeMgr = PlaneMgr.Instance;
            if (planeMgr?.MainPlane != null)
            {
                TileSystemAura.Tick(planeMgr.MainPlane, deltaTime);
            }
            AuraDaoMgr.OnTick(deltaTime);
        }
    }
}
