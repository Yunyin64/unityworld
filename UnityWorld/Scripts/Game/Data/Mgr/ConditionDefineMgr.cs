namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Condition 定义数据管理器
    /// </summary>
    public class ConditionDefineMgr : DefineMgrBase<ConditionDefine>
    {
        public static ConditionDefineMgr Instance { get; private set; }

        public ConditionDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
