namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 属性定义数据管理器
    /// </summary>
    public class StatDefineMgr : DefineMgrBase<StatDefine>
    {
        public static StatDefineMgr Instance { get; private set; }

        public StatDefineMgr(string path) : base(path)
        {
            Instance = this;
        }

        /// <summary>按 Object 类型过滤 StatDefine</summary>
        public IEnumerable<StatDefine> GetByType(string type)
            => Query(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }
}
