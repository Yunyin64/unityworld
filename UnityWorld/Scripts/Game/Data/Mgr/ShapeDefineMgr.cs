namespace UnityWorld.Game.Data
{
    /// <summary>运行时形状目录，重复 ID 会阻止加载。</summary>
    public sealed class ShapeDefineMgr : DefineMgrBase<ShapeDefine>
    {
        /// <summary>当前玩法数据使用的形状目录。</summary>
        public static ShapeDefineMgr Instance { get; private set; }

        /// <summary>创建指定数据目录的索引。</summary>
        public ShapeDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        protected override bool RejectInvalidDefinitions => true;
    }
}
