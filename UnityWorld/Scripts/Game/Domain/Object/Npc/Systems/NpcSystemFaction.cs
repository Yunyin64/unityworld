namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC派系系统
    /// </summary>
    public class NpcSystemFaction : NpcSystemBase<NpcFactionData>
    {
        /// <summary>按角色编号保存的阵营数据</summary>
        protected override Dictionary<int, NpcFactionData> _dataTable { get; set ; }= new();
        /// <summary>保留角色阵营的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 派系系统逻辑
        }
    }
}