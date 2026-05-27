namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 行为卡静态定义数据管理器
    /// </summary>
    public class BehaviorCardDataMgr : DefineMgrBase<BehaviorCardDefine>
    {
        public static BehaviorCardDataMgr Instance { get; private set; }

        public BehaviorCardDataMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
