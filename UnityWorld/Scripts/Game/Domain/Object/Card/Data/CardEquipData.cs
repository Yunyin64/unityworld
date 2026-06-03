using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 侧装备附属数据：提供便捷方法壳子，实际数据问 EquipMgr。
    /// </summary>
    public class CardEquipData : IDomainDataBase
    {
        /// <summary>所属 Card 的 Id（创建时传入）</summary>
        public int CardId { get; set; }

        // ── 便捷查询 ────────────────────────────────────────

        /// <summary>从 EquipMgr 获取装备实例</summary>
        public Equip GetEquip()
            => EquipMgr.Instance?.GetById(CardId);

        // ── IDomainDataBase ─────────────────────────────────

        public IDomainDataBase Clone()
        {
            return (CardEquipData)MemberwiseClone();
        }

        public void Log()
        {
            var eq = GetEquip();
            LogMgr.Instance.Dbg("┌── CardEquipData ───────────────────────────");
            LogMgr.Instance.Dbg("│  CardId:    {0}", CardId);
            LogMgr.Instance.Dbg("│  Equip:     {0}", eq?.ToString() ?? "null");
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }
    }
    public partial class Card
    {

        /// <summary>装备附属数据（通过 CardMgr.EquipSystem 访问）</summary>
        protected CardEquipData EquipData => CardMgr.Instance.EquipSystem.GetData(Id);
        public Equip GetEquip() => EquipData.GetEquip();
        /// <summary>是否为装备卡</summary>
        public bool IsEquipCard => EquipMgr.Instance?.GetById(Id) != null;

        
        /// <summary>
        /// 获取所属装备数据（通过 ParentCardId → EquipMgr）。
        /// 返回 ContextBase 包含 Attack/Defend/Speed/Amount/DisplayName/Element。
        /// 无父装备时返回空 ContextBase。
        /// </summary>
        public ContextBase GetEquipData()
        {
            var ctx = new ContextBase();
            var parentCard = CardMgr.Instance?.GetById(ParentCardId);
            var equip = GetEquip();
            if(equip == null) equip = parentCard.GetEquip();
            if (equip == null) return ctx;
            ctx.Set("Attack", equip.Attack);
            ctx.Set("Defend", equip.Defend);
            ctx.Set("Speed", equip.Speed);
            ctx.Set("Amount", equip.Amount);
            ctx.Set("DisplayName", equip.DisplayName);
            ctx.Set("Element", parentCard.GetElementType().ToString());
            return ctx;
        }
    }
}
