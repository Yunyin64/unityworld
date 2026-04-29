using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    public abstract class NpcSystemBase<T> : ISystemBase<Npc>
    {
        /// <summary>以 int 为键的数据表</summary>
        protected abstract Dictionary<int, T> _dataTable { get; set; }
        /// <summary>注册一个 NPC 的数据</summary>
        public virtual void Register(Npc npc, T data)
        {
            _dataTable[npc.Id] = data;
        }
        /// <summary>获取指定 NPC 的数据，不存在返回 null</summary>
        public virtual T GetData(int npcId)
        {
            return _dataTable[npcId];
        }

        public abstract void OnTick(Npc npc, float deltaTime);

        /// <summary>NPC 诞生时回调（子类可选覆写）</summary>
        public virtual void OnEntityBorn(BirthContext context) { }
    }

}
