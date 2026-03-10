namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Trait系统基类：统一接口，由 TraitMgr 驱动 Tick
    /// </summary>
    public interface ITraitSystem
    {
        void OnTick(Trait trait, float deltaTime);
    }

    public abstract class TraitSystemBase : ITraitSystem
    {
        public abstract void OnTick(Trait trait, float deltaTime);
    }
}
