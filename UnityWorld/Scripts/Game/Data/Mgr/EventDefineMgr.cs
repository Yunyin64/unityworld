namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 事件定义数据管理器
    /// </summary>
    public class EventDefineMgr : DefineMgrBase<EventDefine>
    {
        public static EventDefineMgr Instance { get; private set; }

        public EventDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
