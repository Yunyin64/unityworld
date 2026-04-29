namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC物品栏系�?    /// </summary>
    public class NpcSystemInventory : NpcSystemBase<NpcInventoryData>
    {
        protected override Dictionary<int, NpcInventoryData> _dataTable { get; set ; }= new();

        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 物品栏系统逻辑
        }
    }
}