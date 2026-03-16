using UnityWorld.Core;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 故事执行上下文：Effect 执行时的环境信息容器
    /// 传入所有 StoryEffectFunc 调用，提供对触发主体和世界状态的访问
    /// </summary>
    public class StoryContext
    {
        /// <summary>触发该 Story 的主体对象（NPC/门派/世界等，具体类型由调用方判断）</summary>
        public object? Subject { get; set; }

        /// <summary>触发来源（天/地/人三池）</summary>
        public StoryPoolSource SourcePool { get; set; }

        /// <summary>触发时的游戏世界时间</summary>
        public float CurrentTime { get; set; }

        /// <summary>可复现随机数实例</summary>
        public Rng? Rng { get; set; }

        /// <summary>触发的 StoryDefine ID（便于 Effect 内部查询自身）</summary>
        public string StoryId { get; set; } = "";

        /// <summary>
        /// 构造一个故事执行上下文
        /// </summary>
        public StoryContext(object? subject, StoryPoolSource source, float currentTime, Rng? rng, string storyId = "")
        {
            Subject     = subject;
            SourcePool  = source;
            CurrentTime = currentTime;
            Rng         = rng;
            StoryId     = storyId;
        }
    }
}
