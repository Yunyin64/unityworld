namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC派系系统
    /// </summary>
    public class NpcSystemFaction : NpcSystemBase<NpcFactionData>
    {
        protected override Dictionary<int, NpcFactionData> _dataTable { get; set ; }= new();
        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 派系系统逻辑
        }
    }
}