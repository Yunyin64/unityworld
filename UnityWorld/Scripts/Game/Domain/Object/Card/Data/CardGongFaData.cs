using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 侧功法附属数据：提供便捷方法壳子，实际数据问 GongFaMgr。
    /// </summary>
    public class CardGongFaData : IDomainDataBase
    {        /// <summary>所属 Card 的 Id（创建时传入）</summary>
        public int CardId { get; set; }

        // ── 便捷查询 ────────────────────────────────────────

        /// <summary>从 GongFaMgr 获取功法实例</summary>
        public GongFa GetGongFa()
            => GongFaMgr.Instance?.GetById(CardId);

        /// <summary>获取已解锁的修炼节点</summary>
        public List<CultivationPointDefine> GetUnlockedPoints()
            => GetGongFa()?.GetUnlockedPoints() ?? [];

        /// <summary>获取下一个待解锁的修炼节点</summary>
        public CultivationPointDefine GetNextPoint()
            => GetGongFa()?.GetNextPoint();

        /// <summary>功法是否已修炼完成</summary>
        public bool IsComplete()
            => GetGongFa()?.IsComplete() ?? false;

        // ── IDomainDataBase ─────────────────────────────────

        public IDomainDataBase Clone()
        {
            return (CardGongFaData)MemberwiseClone();
        }

        public void Log()
        {
            var gf = GetGongFa();
            LogMgr.Dbg("┌── CardGongFaData ──────────────────────────");
            LogMgr.Dbg("│  CardId:    {0}", CardId);
            LogMgr.Dbg("│  GongFa:    {0}", gf?.ToString() ?? "null");
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Card
    {
        /// <summary>功法附属数据（通过 CardMgr.GongFaSystem 访问）</summary>
        protected CardGongFaData GongFaData => CardMgr.Instance.GongFaSystem.GetData(Id);

        /// <summary>是否为功法卡</summary>
        public bool IsGongFaCard => GongFaMgr.Instance?.GetById(Id) != null;
    }
}
