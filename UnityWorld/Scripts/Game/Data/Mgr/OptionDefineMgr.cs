namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 选项定义数据管理器
    /// </summary>
    public class OptionDefineMgr : DefineMgrBase<OptionDefine>
    {
        public static OptionDefineMgr Instance { get; private set; }

        public OptionDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
