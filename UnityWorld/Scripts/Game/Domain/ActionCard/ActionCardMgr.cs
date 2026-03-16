using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 行为卡运行时管理器（人·抉择池驱动核心）
    /// 维护所有 NPC 持有的 ActionCard 实例，提供发卡、查卡、用卡、移除功能
    /// UseCard 最终通过 StoryMgr.TriggerStory 统一触发，屏蔽与三池其他来源的差异
    /// </summary>
    public class ActionCardMgr : IDomainMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static ActionCardMgr? Instance { get; private set; }

        // ── IDomainMgrBase ────────────────────────────────────
        public string Name => "ActionCardMgr";
        public string Desc => "行为卡运行时管理器：人·抉择池驱动核心";

        // ── 内部数据 ──────────────────────────────────────────
        /// <summary>NpcId → 该 NPC 持有的行为卡列表</summary>
        private readonly Dictionary<NpcId, List<ActionCard>> _cardTable = new();

        // ── 构造 ──────────────────────────────────────────────
        public ActionCardMgr()
        {
            Instance = this;
        }

        // ── IDomainMgrBase 生命周期 ───────────────────────────
        public void Init()   { }
        public void Begin()  { }
        public void Tick(float deltaTime) { }
        public void Update() { }
        public void Render(float dt) { }
        public void End()    { _cardTable.Clear(); }
        public IEnumerator Save() { yield break; }
        public IEnumerator Load() { yield break; }

        // ── 公共 API ──────────────────────────────────────────

        /// <summary>
        /// 给指定 NPC 发放一张行为卡
        /// </summary>
        public void GiveCard(NpcId npcId, string defineId)
        {
            if (ActionCardDataMgr.Instance?.Get(defineId) == null)
            {
                LogMgr.Warn("[ActionCardMgr] GiveCard 找不到 ActionCardDefine '{0}'", defineId);
                return;
            }
            if (!_cardTable.TryGetValue(npcId, out var list))
            {
                list = new List<ActionCard>();
                _cardTable[npcId] = list;
            }
            list.Add(new ActionCard(defineId, npcId));
            LogMgr.Dbg("[ActionCardMgr] GiveCard npc={0} card={1}", npcId.Value, defineId);
        }

        /// <summary>
        /// 移除指定 NPC 的指定行为卡（移除第一个匹配的 DefineId）
        /// </summary>
        public void RemoveCard(NpcId npcId, string defineId)
        {
            if (!_cardTable.TryGetValue(npcId, out var list)) return;
            var card = list.FirstOrDefault(c => c.DefineId == defineId);
            if (card != null)
            {
                list.Remove(card);
                LogMgr.Dbg("[ActionCardMgr] RemoveCard npc={0} card={1}", npcId.Value, defineId);
            }
        }

        /// <summary>
        /// 获取指定 NPC 持有的所有行为卡（只读）
        /// </summary>
        public IReadOnlyList<ActionCard> GetCards(NpcId npcId)
        {
            return _cardTable.TryGetValue(npcId, out var list)
                ? list.AsReadOnly()
                : (IReadOnlyList<ActionCard>)Array.Empty<ActionCard>();
        }

        /// <summary>
        /// 使用一张行为卡：
        /// 1. 解析 ActionCardDefine
        /// 2. 优先走 StoryIds（确定性） / 否则 Tags 匹配（涌现性）
        /// 3. 通过 StoryMgr.TriggerStory 统一触发
        /// 4. 若 IsConsumable 则移除实例
        /// </summary>
        public void UseCard(NpcId npcId, string defineId, Rng? rng = null)
        {
            if (!_cardTable.TryGetValue(npcId, out var list)) return;
            var card = list.FirstOrDefault(c => c.DefineId == defineId);
            if (card == null)
            {
                LogMgr.Warn("[ActionCardMgr] UseCard npc={0} 未持有 card={1}", npcId.Value, defineId);
                return;
            }

            var define = ActionCardDataMgr.Instance?.Get(defineId);
            if (define == null)
            {
                LogMgr.Warn("[ActionCardMgr] UseCard 找不到 ActionCardDefine '{0}'", defineId);
                return;
            }

            var subject = NpcMgr.Instance?.GetById(npcId);

            // 确定要触发的 StoryId
            if (define.StoryIds.Count > 0)
            {
                // 确定性模式：从列表随机选一个
                var pickRng  = rng ?? new Rng(npcId.Value + card.UsageCount);
                int idx      = pickRng.Range(0, define.StoryIds.Count);
                var storyId  = define.StoryIds[idx];
                StoryMgr.Instance?.TriggerStory(storyId, subject, StoryPoolSource.Will, rng);
            }
            else if (define.StoryTags.Count > 0)
            {
                // 涌现性模式：TagBag 匹配
                StoryMgr.Instance?.TriggerStoryByTags(define.StoryTags, subject, StoryPoolSource.Will, rng);
            }
            else
            {
                LogMgr.Warn("[ActionCardMgr] UseCard '{0}' 既无 StoryIds 也无 StoryTags", defineId);
            }

            // 记录使用次数
            card.UsageCount++;

            // 消耗型卡移除
            if (define.IsConsumable)
                list.Remove(card);
        }
    }
}
