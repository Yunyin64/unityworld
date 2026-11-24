namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC物品栏系�?    /// </summary>
    public class NpcSystemInventory : NpcSystemBase<NpcInventoryData>
    {
        /// <summary>按角色编号保存的物品栏</summary>
        protected override Dictionary<int, NpcInventoryData> _dataTable { get; set ; }= new();

        /// <summary>保留角色物品栏的时步入口</summary>
        public override void OnTick(Npc npc, float deltaTime)
        {
            // Todo: 物品栏系统逻辑
        }
    }
}