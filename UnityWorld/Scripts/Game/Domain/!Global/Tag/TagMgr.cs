using System.Collections;
using UnityWorld.Game.Data;
using UnityWorld.Core;

namespace UnityWorld.Game.Domain.Tag
{
    /// <summary>
    /// Tag 管理器
    /// 职责：
    /// 1. 提供 TagBag 匹配算法（TagBagMatcher 的核心实现）
    /// 2. 提供加权随机抽取
    /// </summary>
    public class TagMgr : IDomainMgrBase
    {
        /// <summary>当前标签管理器</summary>
        public static TagMgr? Instance { get; private set; }

        /// <summary>管理器名称</summary>
        public string Name => "TagMgr";
        /// <summary>标签管理器职责说明</summary>
        public string Desc => "Tag 匹配与随机抽取管理器";

        /// <summary>登记当前标签管理器实例</summary>
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
        public string WeightedRandom(Dictionary<string, float> weights, Random rng)
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

        /// <summary>统计每种标签的出现次数，忽略大小写</summary>
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

        /// <summary>保留独立初始化入口</summary>
        public void Init()
        {
            // TagMgr 主要依赖 TagDefineMgr，无需独立初始化
        }

        /// <summary>保留世界启动回调</summary>
        public void Begin()
        {
            // 无依赖初始化需求
        }

        /// <summary>保留时步回调，标签匹配无持续状态</summary>
        public void Tick(float deltaTime)
        {
            // Tag 匹配是无状态的，无需 Tick
        }

        /// <summary>保留帧更新回调</summary>
        public void Update()
        {
            // 无轻量回调需求
        }

        /// <summary>保留渲染回调</summary>
        public void Render(float dt)
        {
            // 无渲染逻辑
        }

        /// <summary>释放当前标签管理器实例</summary>
        public void End()
        {
            Instance = null;
        }

        /// <summary>结束存档流程，当前无写入</summary>
        public IEnumerator Save()
        {
            yield break;
        }

        /// <summary>结束读档流程，当前无读取</summary>
        public IEnumerator Load()
        {
            yield break;
        }

        /// <summary>日志输出（输出Name、Desc、存的数据信息的数量与概括）</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("[{0}] {1}", Name, Desc);
        }
    }

    
}