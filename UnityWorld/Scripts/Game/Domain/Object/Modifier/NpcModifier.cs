namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 修正源占位类，暂无额外字段，待 NPC 个体层迭代时补充。
    /// </summary>
    public class NpcModifier : ModifierBase
    {
        public NpcModifier(string id, string sourceId, float duration = -1f)
            : base(id, sourceId, duration)
        {
        }
    }
}
