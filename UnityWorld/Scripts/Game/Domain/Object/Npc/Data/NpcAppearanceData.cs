using UnityWorld.Core;
namespace UnityWorld.Game.Domain
{
    /// <summary>
    /// NPC 外观数据（TODO: 未来实现）
    /// 
    /// 预计字段：
    ///   - 身高、体重
    ///   - 肤色、发色、瞳色
    ///   - 头像/立绘 ID
    ///   - 装备外观覆盖
    /// </summary>
    public class NpcAppearanceData : IDomainDataBase
    {
        /// <summary>角色外观身高</summary>
        public float Height;

        /// <summary>浅复制角色的外观数据</summary>
        public NpcAppearanceData Clone()
        {
            var copy = (NpcAppearanceData)MemberwiseClone();
            return copy;
        }
        /// <summary>通过领域数据接口复制外观</summary>
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // TODO: 待实现
        /// <summary>输出角色身高到日志</summary>
        public void Log()
        {
            LogMgr.Instance.Dbg("┌── AppearanceData ──────────────────────────");
            LogMgr.Instance.Dbg("│  身高:          {0:F2}", Height);
            LogMgr.Instance.Dbg("└───────────────────────────────────────────");
        }

    }
    public partial class Npc
    {
        /// <summary>读取角色外观的身高</summary>
        public float GetHeight() => AppearanceData.Height;
    }
        
}
