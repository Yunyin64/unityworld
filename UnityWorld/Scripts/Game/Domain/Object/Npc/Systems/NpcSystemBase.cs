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
        /// <summary>移除指定角色的系统数据</summary>
        public virtual void Unregister(int npcId) => _dataTable.Remove(npcId);
        /// <summary>检查是否已登记指定角色的数据</summary>
        public bool Contains(int npcId) => _dataTable.ContainsKey(npcId);
        /// <summary>本系统已登记的角色数</summary>
        public int RegisteredCount => _dataTable.Count;

        /// <summary>推进指定角色在本系统中的时步</summary>
        public abstract void OnTick(Npc npc, float deltaTime);

        /// <summary>NPC 诞生时回调（子类可选覆写）</summary>
        public virtual void OnEntityBorn(BirthContext context) { }
    }

}
