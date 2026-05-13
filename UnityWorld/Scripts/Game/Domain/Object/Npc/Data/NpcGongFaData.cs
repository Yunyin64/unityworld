using System.Collections.Generic;
using System.Linq;
using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 功法索引数据：存储 NPC 持有的功法卡 ID 列表，
    /// 实际 GongFa 实例由 GongFaMgr 管理。
    /// </summary>
    public class NpcGongFaData : IDomainDataBase
    {
        /// <summary>持有的功法卡 ID 列表</summary>
        public List<int> AllSlotCardIds { get; set; } = [];

        /// <summary>激活的功法卡 ID 列表</summary>
        public List<int> ActiveSlotCardIds { get; set; } = [];

        // ── 便捷查询（从 GongFaMgr 动态获取实例） ──────────

        /// <summary>获取所有持有的 GongFa 实例</summary>
        public List<GongFa> GetAllGongFa()
            => AllSlotCardIds
                .Select(id => GongFaMgr.Instance?.GetById(id))
                .Where(g => g != null)
                .ToList();

        /// <summary>获取所有激活的 GongFa 实例</summary>
        public List<GongFa> GetActiveGongFa()
            => ActiveSlotCardIds
                .Select(id => GongFaMgr.Instance?.GetById(id))
                .Where(g => g != null)
                .ToList();

        // ── IDomainDataBase ─────────────────────────────────

        public NpcGongFaData Clone()
        {
            var copy = (NpcGongFaData)MemberwiseClone();
            copy.AllSlotCardIds = new List<int>(AllSlotCardIds);
            copy.ActiveSlotCardIds = new List<int>(ActiveSlotCardIds);
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        public void Log()
        {
            var allGongFa = GetAllGongFa();
            var activeGongFa = GetActiveGongFa();
            LogMgr.Dbg("┌── GongFa · 功法数据 ──────────────────────────");
            LogMgr.Dbg("│  持有功法({0}):  [{1}]",
                allGongFa.Count,
                allGongFa.ToInfoString());
            LogMgr.Dbg("│  激活功法({0}):  [{1}]",
                activeGongFa.Count,
                activeGongFa.ToInfoString());
            LogMgr.Dbg("└───────────────────────────────────────────");
        }
    }

    public partial class Npc
    {
        /// <summary>获取所有持有的 GongFa 实例</summary>
        public List<GongFa> GetAllSlots() => GongFa.GetAllGongFa();

        /// <summary>获取所有激活的 GongFa 实例</summary>
        public List<GongFa> GetActiveSlots() => GongFa.GetActiveGongFa();
    }
}
