namespace UnityWorld.Game.Data
{
    /// <summary>
    /// 数据管理器基础接口：统一加载入口
    /// </summary>
    public interface IDataMgrBase
    {
        void Load();
        void Load(string filePath);
    }

    /// <summary>
    /// 数据管理器泛型接口：在基础加载能力之上，提供类型安全的定义查询
    /// </summary>
    public interface IDataMgrBase<TDefine> : IDataMgrBase where TDefine : DefineBase
    {
        /// <summary>根据 ID 获取定义，不存在返回 null</summary>
        TDefine? Get(string id);

        /// <summary>获取所有定义</summary>
        IEnumerable<TDefine> GetAll();

        /// <summary>指定 ID 是否存在</summary>
        bool Contains(string id);
    }
}