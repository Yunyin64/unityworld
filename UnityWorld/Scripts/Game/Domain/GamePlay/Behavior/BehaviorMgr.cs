using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 功法运行时管理器：管理 NPC 功法持有、修炼进度、节点解锁
    /// （本次仅搭建骨架，不实现生成逻辑和 Tick 逻辑）
    /// </summary>
    public class BehaviorMgr : IGameplayMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        public static BehaviorMgr Instance { get; private set; }


        // ── IDomainMgrBase 属性 ─────────────────────────────
        public string Name => "BehaviorMgr";
        public string Desc => "行为管理器，管理行为相关函数";

        // ── 构造函数 ─────────────────────────────────────────
        public BehaviorMgr()
        {
            Instance = this;
        }


/// <summary>
        /// 根据 BehaviorId 创建对应的 BehaviorBase 子类实例
        /// </summary>
        /// <param name="behaviorId">行为 ID</param>
        /// <param name="duration">持续时间</param>
        /// <param name="storyEntries">Story 触发规则列表</param>
        /// <param name="rng">随机数生成器</param>
        /// <returns>行为实例</returns>
        public BehaviorBase Create(
            string behaviorId,
            float duration,
            List<BehaviorStoryEntry>? storyEntries = null,
            Rng? rng = null)
        {
            return behaviorId switch
            {
                MoveBehavior.BehaviorIdConstant => new MoveBehavior(duration, storyEntries, rng),
                PracticeBehavior.BehaviorIdConstant => new PracticeBehavior(duration, storyEntries, rng),
                ExploreBehavior.BehaviorIdConstant => new ExploreBehavior(duration, storyEntries, rng),
                SocialBehavior.BehaviorIdConstant => new SocialBehavior(duration, storyEntries, rng),
                _ => new ExtraBehavior(behaviorId, duration, storyEntries, rng)
            };
        }
        


        // ── 生命周期方法（骨架占位）─────────────────────────

        public void Init()
        {
            LogMgr.Dbg("[BehaviorMgr] 初始化完成（骨架模式）");
        }

        public void Begin()
        {
            
        }

        public void Tick(float deltaTime)
        {
            
        }

        public void Update()
        {
            // 轻量帧回调
        }

        public void Render(float dt)
        {
            // 渲染更新
        }

        public void End()
        {
            Instance = null;
        }


        public void Log()
        {
            
        }
    }
}