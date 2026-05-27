namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Tag 定义数据管理器
    /// </summary>
    public class TagDefineMgr : DefineMgrBase<TagDefine>
    {
        public static TagDefineMgr Instance { get; private set; }

        public TagDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
