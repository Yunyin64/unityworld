using System.Collections.Generic;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC位置系统：管理 NPC 在哪个位面、哪个地块上
    /// </summary>
    public class NpcSystemPosition : NpcSystemBase<NpcPositionData>
    {
        protected override Dictionary<int, NpcPositionData> _dataTable { get ; set  ; } = new();

        /// <summary>
        /// NPC 诞生时：创建位置数据（默认值）并注册
        /// </summary>
        public override void OnEntityBorn(BirthContext context)
        {
            var npc = context.MainNpc;
            Register(npc, new NpcPositionData());
        }

        public override void OnTick(Npc npc, float deltaTime)
        {
            
        }
    }
}
