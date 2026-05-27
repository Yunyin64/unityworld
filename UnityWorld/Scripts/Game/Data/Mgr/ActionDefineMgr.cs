namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Action 定义数据管理器
    /// </summary>
    public class ActionDefineMgr : DefineMgrBase<ActionDefine>
    {
        public static ActionDefineMgr Instance { get; private set; }

        public ActionDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
