using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    public class NpcPraticeData : IDomainDataBase
    {

        /// <summary>当前境界等级</summary>
        public int CurrentRealmLevel { get; set; } = 0;

        public CultivationSlot NowCultivationSlot { get; set; } 

        /// <summary>境界进度值（用于突破判定）</summary>
        public int RealmProgress { get; set; } = 0;

        /// <summary>是否正在闭关修炼</summary>
        public bool IsInCultivation { get; set; } = false;

        public NpcPraticeData Clone()
        {
            var copy = (NpcPraticeData)MemberwiseClone();
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();


        public void Log()
        {
            LogMgr.Dbg("┌── PraticeData · 修炼进度 ──────────────────────────");
            LogMgr.Dbg("│  当前境界:      {0}    境界进度: {1}", CurrentRealmLevel, RealmProgress);
            LogMgr.Dbg("│  当前修炼功法:  {0}", NowCultivationSlot?.ToString() ?? "无");
            LogMgr.Dbg("│  是否闭关:      {0}", IsInCultivation.ToString());
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        public CultivationSlot GetNowCultivationSlot() => PracticeData.NowCultivationSlot;
    }
}