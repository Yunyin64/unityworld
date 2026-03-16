using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 地·劫缘池：带权重和条件的事件条目列表
    /// 每隔固定周期，筛选满足 Conditions 的条目，按 Weight 加权随机触发其中一个
    /// </summary>
    public class KarmaPool
    {
        // ── 内部数据 ──────────────────────────────────────────
        private readonly List<KarmaEntry> _entries = [];

        // ── 回调 ──────────────────────────────────────────────
        /// <summary>筛选后按权重随机命中某条目时的回调，参数为 storyId</summary>
        public Action<string>? OnTrigger { get; set; }

        // ── 操作 ──────────────────────────────────────────────

        /// <summary>向劫缘池添加一个条目</summary>
        public void Add(KarmaEntry entry) => _entries.Add(entry);

        /// <summary>向劫缘池添加一个条目（便捷重载）</summary>
        public void Add(string storyId, float weight, List<StoryCondition>? conditions = null)
            => _entries.Add(new KarmaEntry(storyId, weight, conditions));

        /// <summary>
        /// 尝试触发：筛选满足条件的条目，按 Weight 加权随机选一个触发
        /// 无满足条件的条目时静默跳过
        /// </summary>
        public void TryTrigger(Rng rng, StoryContext ctx)
        {
            // 筛选满足 Conditions 的候选条目
            var candidates = new List<KarmaEntry>();
            foreach (var entry in _entries)
            {
                bool pass = true;
                foreach (var cond in entry.Conditions)
                {
                    if (!cond.Evaluate(ctx)) { pass = false; break; }
                }
                if (pass) candidates.Add(entry);
            }

            if (candidates.Count == 0) return; // 静默跳过

            // 加权随机
            float totalWeight = candidates.Sum(e => e.Weight);
            float roll        = rng.Range(0f, totalWeight);
            float cumulative  = 0f;

            foreach (var entry in candidates)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative)
                {
                    OnTrigger?.Invoke(entry.StoryId);
                    return;
                }
            }

            // 保底触发最后一个（浮点误差兜底）
            OnTrigger?.Invoke(candidates[^1].StoryId);
        }

        /// <summary>移除指定 StoryId 的所有条目</summary>
        public void Remove(string storyId)
            => _entries.RemoveAll(e => e.StoryId == storyId);

        /// <summary>当前池中条目总数</summary>
        public int Count => _entries.Count;

        /// <summary>清空劫缘池</summary>
        public void Clear() => _entries.Clear();
    }
}
