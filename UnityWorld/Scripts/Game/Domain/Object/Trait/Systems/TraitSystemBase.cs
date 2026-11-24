namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait系统基类：统一接口，由 TraitMgr 驱动 Tick
    /// </summary>
    public interface ITraitSystem
    {
        /// <summary>推进特质在该系统中的时步</summary>
        void OnTick(Trait trait, float deltaTime);
    }

    public abstract class TraitSystemBase : ITraitSystem
    {
        /// <summary>由具体特质系统实现时步处理</summary>
        public abstract void OnTick(Trait trait, float deltaTime);
    }
}
