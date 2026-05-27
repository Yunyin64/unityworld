namespace UnityWorld.Game.Data
{
    /// <summary>
    /// ExtraElement 定义数据管理器
    /// </summary>
    public class ExtraElementMgr : DefineMgrBase<ExtraElementDefine>
    {
        public static ExtraElementMgr Instance { get; private set; }

        public ExtraElementMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
