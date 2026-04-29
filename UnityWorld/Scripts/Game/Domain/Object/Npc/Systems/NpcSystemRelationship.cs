namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC关系系统
    /// </summary>
    public class NpcSystemRelationship : NpcSystemBase<NpcRelationshipData>
    {
        protected override Dictionary<int, NpcRelationshipData> _dataTable { get ; set  ; } = new();
        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 关系系统逻辑
        }
    }
}