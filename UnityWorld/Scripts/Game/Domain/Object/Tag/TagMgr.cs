using System.Collections;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain.Tag
{
    /// <summary>
    /// Tag 管理器
    /// 职责：
    /// 1. 持有所有 TagDefine
    /// 2. 提供 TagBag 匹配算法（TagBagMatcher 的核心实现）
    /// </summary>
    public class TagMgr:IDomainMgrBase
    {
        public static TagMgr? Instance { get; private set; }

        public string Name => throw new NotImplementedException();

        public string Desc => throw new NotImplementedException();

        public TagMgr()
        {
            Instance = this;
        }

        /// <summary>
        /// TagBag 匹配：根据 query TagBag 从候选池中计算每个候选的匹配权重。
        /// 使用 Multiset 交集：重复的 Tag 可被重复计算，体现"浓度"。
        /// </summary>
        /// <param name="query">查询方的 TagBag（List，重复表示浓度）</param>
        /// <param name="candidates">候选池 Dict&lt;id, TagBag&gt;</param>
        /// <param name="matchType">匹配类型</param>
        /// <param name="matchDegree">匹配度 0.0~1.0，越高越严格</param>
        /// <returns>候选id → 权重（0表示被过滤）</returns>
        public Dictionary<string, float> Match(
            List<string> query,
            Dictionary<string, List<string>> candidates,
            TagMatchType matchType,
            float matchDegree)
        {
            var result = new Dictionary<string, float>();

            // query 转 Multiset
            var queryBag = ToMultiset(query);
            int queryTotal = query.Count;

            foreach (var (id, candidateTags) in candidates)
            {
                var candidateBag = ToMultiset(candidateTags);
                int candidateTotal = candidateTags.Count;

                // Multiset 交集大小：Σ min(query[t], candidate[t])
                int intersection = 0;
                foreach (var (tag, qCount) in queryBag)
                {
                    if (candidateBag.TryGetValue(tag, out int cCount))
                        intersection += Math.Min(qCount, cCount);
                }

                float rawScore = matchType switch
                {
                    TagMatchType.Strict =>
                        // 交集必须覆盖 query 全部
                        intersection >= queryTotal ? 1.0f : 0.0f,

                    TagMatchType.Include =>
                        // 覆盖率：交集 / query总数
                        queryTotal == 0 ? 1.0f : (float)intersection / queryTotal,

                    TagMatchType.Weighted =>
                        // Jaccard 相似度（Multiset版本）
                        (queryTotal + candidateTotal - intersection) == 0
                            ? 1.0f
                            : (float)intersection / (queryTotal + candidateTotal - intersection),

                    TagMatchType.Free =>
                        // 完全不过滤，权重均等
                        1.0f,

                    _ => 0.0f
                };

                if (rawScore <= 0f) continue;

                // matchDegree 调节：越高越严格（低分节点权重趋近0）
                // weight = rawScore ^ (1 / (1 - matchDegree + ε))
                float exponent = 1.0f / (1.0f - matchDegree + 1e-6f);
                float weight = (float)Math.Pow(rawScore, exponent);

                if (weight > 1e-6f)
                    result[id] = weight;
            }

            return result;
        }

        /// <summary>
        /// 按权重随机抽取一个 id（加权随机）
        /// </summary>
        public string? WeightedRandom(Dictionary<string, float> weights, Random rng)
        {
            float total = weights.Values.Sum();
            if (total <= 0f) return null;

            float roll = (float)rng.NextDouble() * total;
            foreach (var (id, w) in weights)
            {
                roll -= w;
                if (roll <= 0f) return id;
            }
            return weights.Keys.Last();
        }

        // ── 内部工具 ─────────────────────────────────────────

        private static Dictionary<string, int> ToMultiset(List<string> tags)
        {
            var bag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var tag in tags)
            {
                bag.TryGetValue(tag, out int count);
                bag[tag] = count + 1;
            }
            return bag;
        }

        public void Init()
        {
            throw new NotImplementedException();
        }

        public void Begin()
        {
            throw new NotImplementedException();
        }

        public void Tick(float deltaTime)
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