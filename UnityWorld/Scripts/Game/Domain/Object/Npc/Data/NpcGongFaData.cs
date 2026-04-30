using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    public class NpcGongFaData : IDomainDataBase
    {// ── 功法槽位 ────────────────────────────────────

        /// <summary>持有的功法列表</summary>
        public List<CultivationSlot> AllSlots { get; set; } = [];

        /// <summary>激活的功法列表</summary>
        public List<CultivationSlot> ActiveSlots { get; set; } = [];

        public NpcGongFaData Clone()
        {
            var copy = (NpcGongFaData)MemberwiseClone();
            copy.AllSlots = new List<CultivationSlot>(AllSlots);
            copy.ActiveSlots = new List<CultivationSlot>(ActiveSlots);
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        public void Log()
        {
            LogMgr.Dbg("┌── GongFaData · 功法数据 ──────────────────────────");
            LogMgr.Dbg("│  持有功法({0}):  [{1}]",
                AllSlots.Count,
                AllSlots.ToInfoString());
            LogMgr.Dbg("│  激活功法({0}):  [{1}]",
                ActiveSlots.Count,
                ActiveSlots.ToInfoString());
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        
        public List<CultivationSlot> GetAllSlots() =>GongFaData.AllSlots;
        
        public List<CultivationSlot> GetActiveSlots() =>GongFaData.ActiveSlots;
    }
}