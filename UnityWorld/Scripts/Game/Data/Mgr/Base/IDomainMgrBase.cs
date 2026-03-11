namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 数据管理器基础接口：统一加载入口
    /// </summary>
    public interface IDomainMgrBase
    {
        void Initialize();
        void Tick(float deltaTime);
    }
}