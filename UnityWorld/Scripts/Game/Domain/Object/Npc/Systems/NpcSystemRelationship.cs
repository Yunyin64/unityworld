namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC关系系统
    /// </summary>
    public class NpcSystemRelationship : NpcSystemBase<NpcRelationshipData>
    {
        /// <summary>按角色编号保存的关系数据</summary>
        protected override Dictionary<int, NpcRelationshipData> _dataTable { get ; set  ; } = new();
        /// <summary>保留角色关系的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 关系系统逻辑
        }
    }
}