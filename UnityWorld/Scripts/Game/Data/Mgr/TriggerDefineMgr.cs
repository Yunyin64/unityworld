namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Trigger 定义数据管理器
    /// </summary>
    public class TriggerDefineMgr : DefineMgrBase<TriggerDefine>
    {
        public static TriggerDefineMgr Instance { get; private set; }

        public TriggerDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
