namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 物品定义数据管理器
    /// </summary>
    public class ItemDefineMgr : DefineMgrBase<ItemDefine>
    {
        public static ItemDefineMgr Instance { get; private set; }

        public ItemDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
