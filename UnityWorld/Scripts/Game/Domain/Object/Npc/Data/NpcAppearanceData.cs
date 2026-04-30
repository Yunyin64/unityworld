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
        public float Height;

        public NpcAppearanceData Clone()
        {
            var copy = (NpcAppearanceData)MemberwiseClone();
            return copy;
        }
        IDomainDataBase IDomainDataBase.Clone() => Clone();

        // TODO: 待实现
        public void Log()
        {
            LogMgr.Dbg("┌── AppearanceData ──────────────────────────");
            LogMgr.Dbg("│  身高:          {0:F2}", Height);
            LogMgr.Dbg("└───────────────────────────────────────────");
        }

    }
    public partial class Npc
    {
        public float GetHeight() => AppearanceData.Height;
    }
        
}
