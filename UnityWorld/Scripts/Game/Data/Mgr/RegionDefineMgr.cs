namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 区域定义数据管理器
    /// </summary>
    public class RegionDefineMgr : DefineMgrBase<RegionDefine>
    {
        public static RegionDefineMgr Instance { get; private set; }

        public RegionDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
