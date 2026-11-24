using System.Collections;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 通过行为卡的类型与时长创建行为实例。
    /// </summary>
    public class BehaviorMgr : IGameplayMgrBase
    {
        // ── 单例 ─────────────────────────────────────────────
        /// <summary>当前行为管理器</summary>
        public static BehaviorMgr Instance { get; private set; }


        // ── IDomainMgrBase 属性 ─────────────────────────────
        /// <summary>管理器名称</summary>
        public string Name => "BehaviorMgr";
        /// <summary>行为管理器职责说明</summary>
        public string Desc => "行为管理器，管理行为相关函数";

        // ── 构造函数 ─────────────────────────────────────────
        /// <summary>登记当前行为管理器实例</summary>
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
                WorkBehavior.BehaviorIdConstant => new WorkBehavior(duration, storyEntries, rng),
                SocialBehavior.BehaviorIdConstant => new SocialBehavior(duration, storyEntries, rng),
                _ => new ExtraBehavior(behaviorId, duration, storyEntries, rng)
            };
        }
        


        // ── 生命周期方法（骨架占位）─────────────────────────

        /// <summary>记录行为管理器初始化日志</summary>
        public void Init()
        {
            LogMgr.Instance.Dbg("[BehaviorMgr] 初始化完成（骨架模式）");
        }

        /// <summary>保留世界启动回调</summary>
        public void Begin()
        {
            
        }

        /// <summary>保留时步回调</summary>
        public void Tick(float deltaTime)
        {
            
        }

        /// <summary>保留帧更新回调</summary>
        public void Update()
        {
            // 轻量帧回调
        }

        /// <summary>保留渲染回调</summary>
        public void Render(float dt)
        {
            // 渲染更新
        }

        /// <summary>释放行为管理器实例</summary>
        public void End()
        {
            Instance = null;
        }


        /// <summary>保留行为管理日志入口</summary>
        public void Log()
        {
            
        }
    }
}
