namespace UnityWorld.Game.Data
{
    /// <summary>
    /// Effect 定义数据管理器
    /// 现在废弃状态，AI不要关注这个
    /// </summary>
    public class EffectDefineMgr : DefineMgrBase<EffectDefine>
    {
        public static EffectDefineMgr Instance { get; private set; }

        public EffectDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
