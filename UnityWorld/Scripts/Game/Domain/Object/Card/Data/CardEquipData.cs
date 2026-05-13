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
            LogMgr.Dbg("┌── CardEquipData ───────────────────────────");
            LogMgr.Dbg("│  CardId:    {0}", CardId);
            LogMgr.Dbg("│  Equip:     {0}", eq?.ToString() ?? "null");
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }
    public partial class Card
    {

        /// <summary>装备附属数据（通过 CardMgr.EquipSystem 访问）</summary>
        protected CardEquipData EquipData => CardMgr.Instance.EquipSystem.GetData(Id);

        /// <summary>是否为装备卡</summary>
        public bool IsEquipCard => EquipMgr.Instance?.GetById(Id) != null;
    }
}
