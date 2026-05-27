namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 元气修正定义数据管理器
    /// </summary>
    public class TileModifierDefineMgr : DefineMgrBase<TileModifierDefine>
    {
        public static TileModifierDefineMgr Instance { get; private set; }

        public TileModifierDefineMgr(string path) : base(path)
        {
            Instance = this;
        }
    }
}
