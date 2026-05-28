using UnityWorld.Core;

namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 战斗修正定义数据管理器
    /// </summary>
    public class CombatNpcModifierDefineMgr : DefineMgrBase<CombatNpcModifierDefine>
    {
        public static CombatNpcModifierDefineMgr Instance { get; private set; }

        public CombatNpcModifierDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        /// <summary>按 ID 查询，不存在时记录警告并返回 null</summary>
        public new CombatNpcModifierDefine Get(string id)
        {
            var result = base.Get(id);
            if (result == null)
                LogMgr.Instance.Warn("[CombatNpcModifierDefineMgr] Define不存在:{0}", id);
            return result;
        }
    }
}
