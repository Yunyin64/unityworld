namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 装备定义数据管理器
    /// </summary>
    public class EquipDefineMgr : DefineMgrBase<EquipDefine>
    {
        public static EquipDefineMgr Instance { get; private set; }

        public EquipDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
