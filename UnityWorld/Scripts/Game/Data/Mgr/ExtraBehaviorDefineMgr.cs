namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为拓展定义数据管理器
    /// </summary>
    public class ExtraBehaviorDefineMgr : DefineMgrBase<ExtraBehaviorDefine>
    {
        public static ExtraBehaviorDefineMgr Instance { get; private set; }

        public ExtraBehaviorDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
