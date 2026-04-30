using UnityWorld.Core;
using UnityWorld.Game.Data;

namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// Card 运行时实例（轻量句柄）
    /// 只持有身份信息 + BaseData/EffectData 委托访问
    /// </summary>
    public partial class Card : GameEntityBase, IFormDefine<CardDefine>
    {
        public int Id {get;set;}
        /// <summary>来源的 CardDefine ID</summary>
        public string DefineId { get; set; } = "";

        /// <summary>卡牌显示名称（用于日志/UI，优先使用此字段）</summary>
        public string DisplayName { get; set; } = "";

        // ── 数据委托 ──────────────────────────────────────────

        /// <summary>基础属性数据（简单不变属性）</summary>
        public CardBaseData BaseData { get; set; } = new();

        // ── 日志 ──────────────────────────────────────────────


        /// <summary>层次化日志输出</summary>
        public override void LogAllInfo()
        {
            LogMgr.Dbg("┌══ Card · {0} ══════════════════════════════", DisplayName);
            LogMgr.Dbg("│  DefineId: {0}", DefineId);
            BaseData.Log();
            LogMgr.Dbg("└═══════════════════════════════════════════");
        }


        public override string ToString()
        {
            return $"Card({DefineId}, {DisplayName})";
        }
    }
}
