namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// 卡牌修正源占位类，暂无额外字段，待卡牌系统迭代时补充。
    /// </summary>
    public class CardModifier : ModifierBase
    {
        public CardModifier(string id, string sourceId, float duration = -1f)
            : base(id, sourceId, duration)
        {
        }
    }
}
