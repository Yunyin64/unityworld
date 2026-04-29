using System.Text.Json;
using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 故事定义数据管理器
    /// 负责加载 StoryDefines.json，并在 Begin() 中构建双向 Option 合并表
    /// </summary>
    public class StoryDefineMgr : IDataMgrBase<StoryDefine>
    {
        // ── 单例 ─────────────────────────────────────────────
        public static StoryDefineMgr? Instance { get; private set; }

        // ── 内部数据 ──────────────────────────────────────────
        private Dictionary<string, StoryDefine> _stories = [];

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly string _filePath;

        // ── 构造 ──────────────────────────────────────────────
        public StoryDefineMgr(string filePath)
        {
            _filePath = filePath;
            Instance  = this;
        }

        // ── IDataMgrBase ─────────────────────────────────────

        /// <summary>加载 JSON 文件</summary>
        public void Load() => Load(_filePath);

        /// <summary>加载指定路径的 JSON 文件</summary>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LogMgr.Warn("[StoryDefineMgr] 找不到 {0}，故事库为空", filePath);
                return;
            }
            var list = JsonSerializer.Deserialize<List<StoryDefine>>(
                File.ReadAllText(filePath), _jsonOpts) ?? [];
            _stories = list.ToDictionary(s => s.ID, StringComparer.OrdinalIgnoreCase);
            LogMgr.Dbg("[StoryDefineMgr] 加载完成：{0} 个故事定义", _stories.Count);
        }

        /// <summary>
        /// 构建双向 Option 合并表
        /// 需要在 OptionDefineMgr.Load() 之后调用
        /// 将所有 OptionDefine.StoryIds 的反向注入合并到对应 StoryDefine.MergedOptionIds 中
        /// </summary>
        public void BuildMergedOptions()
        {
            // 先用正向 OptionIds 初始化
            foreach (var story in _stories.Values)
            {
                story.MergedOptionIds = new List<string>(story.OptionIds);
                // 检查正向引用的合法性
                foreach (var optId in story.OptionIds)
                {
                    if (OptionDefineMgr.Instance?.Contains(optId) == false)
                        LogMgr.Warn("[StoryDefineMgr] Story '{0}' 引用了不存在的 OptionId '{1}'，已跳过", story.ID, optId);
                }
            }

            // 再合并反向注入
            if (OptionDefineMgr.Instance == null) return;
            foreach (var option in OptionDefineMgr.Instance.GetAll())
            {
                foreach (var storyId in option.StoryIds)
                {
                    if (_stories.TryGetValue(storyId, out var story))
                    {
                        if (!story.MergedOptionIds.Contains(option.ID))
                            story.MergedOptionIds.Add(option.ID);
                    }
                    else
                    {
                        LogMgr.Warn("[StoryDefineMgr] OptionDefine '{0}' 反向引用了不存在的 StoryId '{1}'，已跳过", option.ID, storyId);
                    }
                }
            }
            LogMgr.Dbg("[StoryDefineMgr] 双向 Option 合并完成");
        }

        /// <summary>通过 ID 查询故事定义，不存在返回 null</summary>
        public StoryDefine? Get(string id)
            => _stories.TryGetValue(id, out var s) ? s : null;

        /// <summary>获取所有故事定义</summary>
        public IEnumerable<StoryDefine> GetAll() => _stories.Values;

        /// <summary>故事 ID 是否存在</summary>
        public bool Contains(string id) => _stories.ContainsKey(id);
        public IEnumerable<StoryDefine> Query(Func<StoryDefine, bool> predicate) => _stories.Values.Where(predicate);
    }
}
