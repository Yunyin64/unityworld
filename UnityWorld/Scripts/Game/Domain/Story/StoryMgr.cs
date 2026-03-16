using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 叙事管理器：天地人三池调度与统一触发入口
    /// 天（宿命池）：时间到达后触发预写事件
    /// 地（劫缘池）：周期性权重随机触发环境事件
    /// 人（抉择池）：由 ActionCardMgr 驱动，通过此管理器统一触发
    /// </summary>
    public class StoryMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static StoryMgr? Instance { get; private set; }

        // ── IDomainMgrBase ────────────────────────────────────
        public string Name => "StoryMgr";
        public string Desc => "叙事事件管理器：天地人三池调度与统一触发入口";

        // ── 配置 ──────────────────────────────────────────────
        /// <summary>劫缘池触发周期（游戏时间单位），默认 30 秒</summary>
        public float KarmaTriggerInterval { get; set; } = 30f;

        // ── 内部状态 ──────────────────────────────────────────
        private readonly Dictionary<NpcId, FatePool>  _fatePools  = new();
        private readonly Dictionary<NpcId, KarmaPool> _karmaPools = new();
        private float   _karmaTimer = 0f;
        private float   _currentTime = 0f;
        private Rng?    _rng;

        // ── 全局故事池（用于 TriggerByTag 匹配） ─────────────
        // 所有 StoryDefine 的 Tags 映射，懒加载
        private Dictionary<string, List<string>>? _storyTagsCache;

        // ── 构造 ──────────────────────────────────────────────
        public StoryMgr(int seed = 0)
        {
            Instance = this;
            _rng     = new Rng(seed);
        }

        // ── IDomainMgrBase 生命周期 ───────────────────────────

        /// <summary>无耦合初始化</summary>
        public void Init()
        {
            _karmaTimer  = 0f;
            _currentTime = 0f;
        }

        /// <summary>有耦合初始化：构建 StoryDefine 双向 Option 合并表</summary>
        public void Begin()
        {
            StoryDefineMgr.Instance?.BuildMergedOptions();
            LogMgr.Dbg("[StoryMgr] Begin：双向 Option 合并完成");
        }

        /// <summary>Tick：推进宿命池时间检查 + 劫缘池周期触发</summary>
        public void Tick(float deltaTime)
        {
            _currentTime += deltaTime;

            // ① 宿命池：推进所有主体的 FatePool
            foreach (var (npcId, fatePool) in _fatePools)
                fatePool.Tick(_currentTime);

            // ② 劫缘池：周期触发
            _karmaTimer += deltaTime;
            if (_karmaTimer >= KarmaTriggerInterval)
            {
                _karmaTimer = 0f;
                TriggerAllKarmaPools();
            }
        }

        public void Update()  { }
        public void Render(float dt) { }

        /// <summary>清理</summary>
        public void End()
        {
            _fatePools.Clear();
            _karmaPools.Clear();
            _storyTagsCache = null;
        }

        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        // ── 公共 API：三池管理 ────────────────────────────────

        /// <summary>向指定 NPC 的宿命池添加条目</summary>
        public void AddToFatePool(NpcId npcId, float triggerTime, string storyId)
        {
            if (!_fatePools.TryGetValue(npcId, out var pool))
            {
                pool = new FatePool();
                pool.OnTrigger = (sid, time) => OnFateTrigger(npcId, sid);
                _fatePools[npcId] = pool;
            }
            pool.Add(triggerTime, storyId);
        }

        /// <summary>向指定 NPC 的劫缘池添加条目</summary>
        public void AddToKarmaPool(NpcId npcId, string storyId, float weight, List<StoryCondition>? conditions = null)
        {
            if (!_karmaPools.TryGetValue(npcId, out var pool))
            {
                pool = new KarmaPool();
                _karmaPools[npcId] = pool;
            }
            pool.Add(storyId, weight, conditions);
        }

        // ── 公共 API：统一触发入口 ────────────────────────────

        /// <summary>
        /// 统一触发入口：通过 StoryId 触发一个故事事件
        /// 三池（宿命/劫缘/抉择）均通过此方法触发，屏蔽来源差异
        /// </summary>
        public void TriggerStory(string storyId, object? subject, StoryPoolSource source = StoryPoolSource.Fate, Rng? rng = null)
        {
            var define = StoryDefineMgr.Instance?.Get(storyId);
            if (define == null)
            {
                LogMgr.Warn("[StoryMgr] TriggerStory 找不到 StoryDefine '{0}'", storyId);
                return;
            }

            var ctx = new StoryContext(subject, source, _currentTime, rng ?? _rng, storyId);

            // ① 检查 Conditions
            if (!define.EvaluateConditions(ctx))
            {
                LogMgr.Dbg("[StoryMgr] Story '{0}' Conditions 不满足，跳过触发", storyId);
                return;
            }

            // ② Lua 复杂轨检查（预留，当前降级到简单轨）
            if (!string.IsNullOrEmpty(define.LuaScript))
            {
                LogMgr.Warn("[StoryMgr] Story '{0}' 配置了 LuaScript 但 Lua 尚未集成，降级到简单轨执行", storyId);
            }

            // ③ 执行 Effects（简单轨）
            StoryEffectFunc.ExecuteAll(define.Effects, ctx);

            // ④ 若为显示事件，广播 UI 事件
            if (!define.IsHide)
            {
                LogMgr.Dbg("[StoryMgr] 显示 Story '{0}' 触发，广播 UI 事件", storyId);
                EventMgr.Instance?.TriggerEvent(
                    "story.show",
                    new { StoryId = storyId, Subject = subject, Options = define.MergedOptionIds },
                    (EventScope.Global, "")
                );
            }
            else
            {
                LogMgr.Dbg("[StoryMgr] 隐形 Story '{0}' 触发完成", storyId);
            }
        }

        /// <summary>
        /// 通过 Tags 在全局 StoryPool 中 TagBag 匹配后触发权重最高的 Story
        /// </summary>
        public void TriggerStoryByTags(List<string> tags, object? subject, StoryPoolSource source = StoryPoolSource.Karma, Rng? rng = null)
        {
            BuildStoryTagsCache();
            if (_storyTagsCache == null || _storyTagsCache.Count == 0) return;

            // 简单 Include 匹配：找交集最多的 Story
            string? bestId   = null;
            int     bestScore = -1;
            var     querySet  = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);

            foreach (var (storyId, storyTags) in _storyTagsCache)
            {
                int score = storyTags.Count(t => querySet.Contains(t));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId    = storyId;
                }
            }

            if (bestId != null && bestScore > 0)
                TriggerStory(bestId, subject, source, rng);
            else
                LogMgr.Dbg("[StoryMgr] TriggerStoryByTag 未找到匹配的 Story，tags={0}", string.Join(",", tags));
        }

        // ── 内部方法 ──────────────────────────────────────────

        private void OnFateTrigger(NpcId npcId, string storyId)
        {
            var subject = NpcMgr.Instance?.GetById(npcId);
            TriggerStory(storyId, subject, StoryPoolSource.Fate, _rng);
        }

        private void TriggerAllKarmaPools()
        {
            foreach (var (npcId, karmaPool) in _karmaPools)
            {
                var subject = NpcMgr.Instance?.GetById(npcId);
                var ctx     = new StoryContext(subject, StoryPoolSource.Karma, _currentTime, _rng);
                karmaPool.OnTrigger ??= (sid) => TriggerStory(sid, subject, StoryPoolSource.Karma, _rng);
                karmaPool.TryTrigger(_rng!, ctx);
            }
        }

        private void BuildStoryTagsCache()
        {
            if (_storyTagsCache != null) return;
            _storyTagsCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var all = StoryDefineMgr.Instance?.GetAll();
            if (all == null) return;
            foreach (var s in all)
                _storyTagsCache[s.ID] = s.Tags;
        }
    }
}
