namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC系统基类：统一接口/基类(IUpdate/Tick)
    /// </summary>
    public interface INpcSystem
    {
        void OnTick(Npc npc, float deltaTime);
    }

    public abstract class NpcSystemBase : INpcSystem
    {
        public abstract void OnTick(Npc npc, float deltaTime);
    }
}