using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 故事定义数据管理器
    /// </summary>
    public class StoryDefineMgr : DefineMgrBase<StoryDefine>
    {
        public static StoryDefineMgr Instance { get; private set; }

        public StoryDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        /// <summary>
        /// 构建双向 Option 合并表。
        /// 需要在 OptionDefineMgr.Load() 之后调用。
        /// 将所有 OptionDefine.StoryIds 的反向注入合并到对应 StoryDefine.MergedOptionIds 中。
        /// </summary>
        public void BuildMergedOptions()
        {
            // 先用正向 OptionIds 初始化
            foreach (var story in GetAll())
            {
                story.MergedOptionIds = new List<string>(story.OptionIds);
                foreach (var optId in story.OptionIds)
                {
                    if (OptionDefineMgr.Instance?.Contains(optId) == false)
                        LogMgr.Instance.Warn("[StoryDefineMgr] Story '{0}' 引用了不存在的 OptionId '{1}'，已跳过", story.ID, optId);
                }
            }

            // 再合并反向注入
            if (OptionDefineMgr.Instance == null) return;
            foreach (var option in OptionDefineMgr.Instance.GetAll())
            {
                foreach (var storyId in option.StoryIds)
                {
                    var story = Get(storyId);
                    if (story != null)
                    {
                        if (!story.MergedOptionIds.Contains(option.ID))
                            story.MergedOptionIds.Add(option.ID);
                    }
                    else
                    {
                        LogMgr.Instance.Warn("[StoryDefineMgr] OptionDefine '{0}' 反向引用了不存在的 StoryId '{1}'，已跳过", option.ID, storyId);
                    }
                }
            }
            LogMgr.Instance.Dbg("[StoryDefineMgr] 双向 Option 合并完成");
        }
    }
}
