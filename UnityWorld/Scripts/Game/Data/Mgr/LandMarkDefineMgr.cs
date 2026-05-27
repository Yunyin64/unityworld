namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 地标定义数据管理器
    /// </summary>
    public class LandMarkDefineMgr : DefineMgrBase<LandMarkDefine>
    {
        public static LandMarkDefineMgr Instance { get; private set; }

        public LandMarkDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
