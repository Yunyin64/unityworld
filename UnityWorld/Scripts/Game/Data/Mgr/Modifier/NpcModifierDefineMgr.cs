namespace UnityWorld.Game.Data
{
    /// <summary>
    /// NPC 修正定义数据管理器
    /// </summary>
    public class NpcModifierDefineMgr : DefineMgrBase<NpcModifierDefine>
    {
        public static NpcModifierDefineMgr Instance { get; private set; }

        public NpcModifierDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
