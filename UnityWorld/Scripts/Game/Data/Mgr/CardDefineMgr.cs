namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Card 模板定义数据管理器
    /// </summary>
    public class CardDefineMgr : DefineMgrBase<CardDefine>
    {
        public static CardDefineMgr Instance { get; private set; }

        public CardDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
