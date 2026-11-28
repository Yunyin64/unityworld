using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>NPC 已选定的一个插槽的资源、来源和染色。</summary>
    public sealed record NpcAppearancePart(string Address, string SourceId, string? Dye = null);

    /// <summary>NPC 实例已经选定的外观，资源本身由宿主管理。</summary>
    public class NpcAppearanceData : IDomainDataBase
    {
        /// <summary>角色外观身高</summary>
        public float Height;

        /// <summary>选定的骨架，非人形为空。</summary>
        public string Rig { get; set; } = "";

        /// <summary>按骨架插槽记录实例选中的部件。</summary>
        public Dictionary<string, NpcAppearancePart> Parts { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>复制角色外观，部件映射由副本独占。</summary>
        public NpcAppearanceData Clone()
        {
            var copy = (NpcAppearanceData)MemberwiseClone();
            copy.Parts = new(Parts, StringComparer.Ordinal);
            return copy;
        }
        /// <summary>通过领域数据接口复制外观</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        /// <summary>输出角色身高和已装配的骨架到日志。</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("┌── AppearanceData ──────────────────────────");
            LogMgr.Instance.Dbg("│  身高:          {0:F2}", Height);
            LogMgr.Instance.Dbg("│  骨架:          {0}（{1} 个部件）", Rig, Parts.Count);
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }

    }
    public partial class Npc
    {
        /// <summary>读取角色外观的身高</summary>
        public float GetHeight() => AppearanceData.Height;
    }
        
}
