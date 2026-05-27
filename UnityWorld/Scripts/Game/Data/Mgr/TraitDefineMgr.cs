namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 特质定义数据管理器
    /// </summary>
    public class TraitDefineMgr : DefineMgrBase<TraitDefine>
    {
        public static TraitDefineMgr Instance { get; private set; }

        public TraitDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
