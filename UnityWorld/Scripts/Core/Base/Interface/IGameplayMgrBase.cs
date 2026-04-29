using System.Collections;

    /// <summary>
    /// 玩法管理器基础接口：统一加载入口
    /// </summary>
    public interface IGameplayMgrBase
    {
        /// <summary>
        /// 管理器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 管理器描述
        /// </summary>
        string Desc { get; }

        /// <summary>
        /// 无耦合初始化
        /// </summary>
        void Init();
        /// <summary>
        /// 有耦合初始化
        /// </summary>
        void Begin();
        void Tick(float deltaTime);
        /// <summary>
        /// 帧更新
        /// </summary>
        void Update();

        /// <summary>
        /// 渲染更新
        /// </summary>
        void Render(float dt);

        /// <summary>
        /// 结束/销毁
        /// </summary>
        void End();
        
        /// <summary>
        /// 日志输出（输出Name、Desc、存的数据信息的数量与概括）
        /// </summary>
        void Log();

    }
