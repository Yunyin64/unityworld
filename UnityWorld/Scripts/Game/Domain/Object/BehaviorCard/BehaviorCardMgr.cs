using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为卡运行时管理器（人·抉择池驱动核心）
    /// 维护所有 NPC 持有的 BehaviorCard 实例，提供发卡、查卡、用卡、移除功能
    /// UseCard 会创建 Behavior 实例并填入行为槽，在行为生命周期中触发 Story
    /// </summary>
    public class BehaviorCardMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static BehaviorCardMgr? Instance { get; private set; }

        // ── IDomainMgrBase ────────────────────────────────────
        public string Name => "BehaviorCardMgr";
        public string Desc => "行为卡运行时管理器：人·抉择池驱动核心";

        // ── 内部数据 ──────────────────────────────────────────
        /// <summary>int → 该 NPC 持有的行为卡列表</summary>
        private readonly Dictionary<int, List<BehaviorCard>> _cardTable = new();

        // ── 构造 ──────────────────────────────────────────────
        public BehaviorCardMgr()
        {
            Instance = this;
        }

        // ── IDomainMgrBase 生命周期 ───────────────────────────
        public void Init()   { }
        public void Begin()  { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }
        public void End()    { _cardTable.Clear(); Instance = null; }
        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            int totalCards = _cardTable.Values.Sum(l => l.Count);
            LogMgr.Dbg("[{0}] {1} | NpcCount={2}, TotalCards={3}", Name, Desc, _cardTable.Count, totalCards);
        }

        // ── 公共 API ──────────────────────────────────────────

        /// <summary>
        /// 给指定 NPC 发放一张行为卡
        /// </summary>
        public void GiveCard(int npcId, string defineId)
        {
            if (BehaviorCardDataMgr.Instance?.Get(defineId) == null)
            {
                LogMgr.Warn("[BehaviorCardMgr] GiveCard 找不到 BehaviorCardDefine '{0}'", defineId);
                return;
            }
            if (!_cardTable.TryGetValue(npcId, out var list))
            {
                list = new List<BehaviorCard>();
                _cardTable[npcId] = list;
            }
            list.Add(new BehaviorCard(defineId, npcId));
            LogMgr.Dbg("[BehaviorCardMgr] GiveCard npc={0} card={1}", npcId, defineId);
        }

        /// <summary>
        /// 移除指定 NPC 的指定行为卡（移除第一个匹配的 DefineId）
        /// </summary>
        public void RemoveCard(int npcId, string defineId)
        {
            if (!_cardTable.TryGetValue(npcId, out var list)) return;
            var card = list.FirstOrDefault(c => c.DefineId == defineId);
            if (card != null)
            {
                list.Remove(card);
                LogMgr.Dbg("[BehaviorCardMgr] RemoveCard npc={0} card={1}", npcId, defineId);
            }
        }

        /// <summary>
        /// 获取指定 NPC 持有的所有行为卡（只读）
        /// </summary>
        public IReadOnlyList<BehaviorCard> GetCards(int npcId)
        {
            return _cardTable.TryGetValue(npcId, out var list)
                ? list.AsReadOnly()
                : (IReadOnlyList<BehaviorCard>)Array.Empty<BehaviorCard>();
        }

        /// <summary>
        /// 使用一张行为卡：
        /// 1. 检查 NPC 是否空闲（主行为槽为空）
        /// 2. 根据 Define.BehaviorId 创建 Behavior 实例
        /// 3. 将 Define 的生命周期 Story 规则转换为 BehaviorStoryEntry 列表
        /// 4. 填入 NPC 行为槽（主/次）
        /// 5. 若 IsConsumable 则移除卡实例
        /// </summary>
        public void UseCard(int npcId, string defineId, Rng? rng = null)
        {
            if (!_cardTable.TryGetValue(npcId, out var list)) return;
            var card = list.FirstOrDefault(c => c.DefineId == defineId);
            if (card == null)
            {
                LogMgr.Warn("[BehaviorCardMgr] UseCard npc={0} 未持有 card={1}", npcId, defineId);
                return;
            }

            var define = BehaviorCardDataMgr.Instance?.Get(defineId);
            if (define == null)
            {
                LogMgr.Warn("[BehaviorCardMgr] UseCard 找不到 BehaviorCardDefine '{0}'", defineId);
                return;
            }

            var behaviorSystem = NpcMgr.Instance?.BehaviorSystem;
            if (behaviorSystem == null)
            {
                LogMgr.Err("[BehaviorCardMgr] UseCard 找不到 BehaviorSystem");
                return;
            }

            // 检查空闲（仅主行为需要）
            if (define.BehaviorIsPrimary && !behaviorSystem.IsIdle(npcId))
            {
                LogMgr.Warn("[BehaviorCardMgr] UseCard npc={0} 非空闲，无法使用主行为卡", npcId);
                return;
            }

            // 创建 Behavior 实例
            var behavior = CreateBehaviorFromDefine(define, rng ?? new Rng(npcId + card.UsageCount));
            if (behavior == null)
            {
                LogMgr.Warn("[BehaviorCardMgr] UseCard 无法创建 Behavior '{0}'", define.BehaviorId);
                return;
            }

            // 填入行为槽
            if (define.BehaviorIsPrimary)
            {
                behaviorSystem.AddPrimary(npcId, behavior);
            }
            else
            {
                behaviorSystem.AddSecondary(npcId, behavior);
            }

            // 记录使用次数
            card.UsageCount++;

            // 消耗型卡移除
            if (define.IsConsumable)
                list.Remove(card);

            LogMgr.Dbg("[BehaviorCardMgr] UseCard npc={0} card={1} → behavior={2}", 
                npcId, defineId, behavior.BehaviorId);
        }

        // ── 私有方法 ──────────────────────────────────────────

        /// <summary>
        /// 根据 BehaviorCardDefine 创建 Behavior 实例
        /// </summary>
        private BehaviorBase? CreateBehaviorFromDefine(BehaviorCardDefine define, Rng rng)
        {
            var behaviorId = string.IsNullOrEmpty(define.BehaviorId) ? "None" : define.BehaviorId;

            

            // 通过工厂创建
            var behavior = BehaviorMgr.Instance.Create(behaviorId, define.BehaviorDuration);
            if (behavior == null) return null;

            // 设置 Story 条目
            SetStoryEntries(behavior, define);

            return behavior;
        }

        /// <summary>
        /// 将 BehaviorCardDefine 的生命周期规则转换为 BehaviorStoryEntry
        /// </summary>
        private void SetStoryEntries(BehaviorBase behavior, BehaviorCardDefine define)
        {
            // OnStart
            if (define.OnStart != null)
            {
                var entries = ConvertToStoryEntries(define.OnStart, BehaviorStoryTrigger.OnStart);
                behavior.StoryEntries.AddRange(entries);
            }

            // OnEnd
            if (define.OnEnd != null)
            {
                var entries = ConvertToStoryEntries(define.OnEnd, BehaviorStoryTrigger.OnEnd);
                behavior.StoryEntries.AddRange(entries);
            }

            // OnInterrupt
            if (define.OnInterrupt != null)
            {
                var entries = ConvertToStoryEntries(define.OnInterrupt, BehaviorStoryTrigger.OnInterrupt);
                behavior.StoryEntries.AddRange(entries);
            }

            // OnTick
            if (define.OnTick != null && define.OnTick.Trigger != null)
            {
                var entry = new BehaviorStoryEntry
                {
                    Trigger = BehaviorStoryTrigger.OnTick,
                    Chance = define.OnTick.Chance,
                    StoryId = define.OnTick.Trigger.StoryIds?.Count > 0 ? define.OnTick.Trigger.StoryIds[0] : "",
                    StoryTags = define.OnTick.Trigger.StoryTags ?? new List<string>(),
                };
                behavior.StoryEntries.Add(entry);
            }

            // OnTimer
            if (define.OnTimer != null && define.OnTimer.Trigger != null)
            {
                var entry = new BehaviorStoryEntry
                {
                    Trigger = BehaviorStoryTrigger.OnTimer,
                    Delay = define.OnTimer.Delay,
                    StoryId = define.OnTimer.Trigger.StoryIds?.Count > 0 ? define.OnTimer.Trigger.StoryIds[0] : "",
                    StoryTags = define.OnTimer.Trigger.StoryTags ?? new List<string>(),
                };
                behavior.StoryEntries.Add(entry);
            }
        }

        /// <summary>
        /// 将 StoryTriggerRule 转换为 BehaviorStoryEntry 列表
        /// </summary>
        private List<BehaviorStoryEntry> ConvertToStoryEntries(StoryTriggerRule rule, BehaviorStoryTrigger trigger)
        {
            var entries = new List<BehaviorStoryEntry>();

            // StoryIds 转换为独立 entry（每个 ID 一条）
            if (rule.StoryIds != null)
            {
                foreach (var storyId in rule.StoryIds)
                {
                    entries.Add(new BehaviorStoryEntry
                    {
                        Trigger = trigger,
                        StoryId = storyId,
                        StoryTags = new List<string>(),
                    });
                }
            }

            // StoryTags 转换为独立 entry
            if (rule.StoryTags != null && rule.StoryTags.Count > 0)
            {
                entries.Add(new BehaviorStoryEntry
                {
                    Trigger = trigger,
                    StoryId = "",
                    StoryTags = new List<string>(rule.StoryTags),
                });
            }

            return entries;
        }
    }
}