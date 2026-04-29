using System.Collections;

    /// <summary>
    /// 数据管理器基础接口：统一加载入口
    /// </summary>
    public interface IDomainMgrBase
    {
        /// <summary>
        /// 管理器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 管理器描述
        /// </summary>
        string Desc { get; }

        /// <summary>
        /// 无耦合初始化
        /// </summary>
        void Init();
        /// <summary>
        /// 有耦合初始化
        /// </summary>
        void Begin();
        void Tick(float deltaTime);
        /// <summary>
        /// 帧更新
        /// </summary>
        void Update();

        /// <summary>
        /// 渲染更新
        /// </summary>
        void Render(float dt);

        /// <summary>
        /// 结束/销毁
        /// </summary>
        void End();

        /// <summary>
        /// 保存数据
        /// </summary>
        IEnumerator Save();

        /// <summary>
        /// 加载数据
        /// </summary>
        IEnumerator Load();

    }


public abstract class DomainMgrBase<T> : IDomainMgrBase where T:GameEntityBase
{
    public Dictionary<int, T> _allEntities { get; set; }

    public void Add(int id,T obj)
    {
        _allEntities[id] = obj;
    }
    public T GetById(int id)
            => _allEntities.TryGetValue(id, out var npc) ? npc : null;

    public IEnumerable<T> GetAll() => _allEntities.Values;

    public abstract void Init();
    public abstract void Begin();
    public abstract void Tick(float deltaTime);
    public abstract void Update();
    public abstract void Render(float dt);
    public abstract void End();
    public abstract IEnumerator Save();
    public abstract IEnumerator Load();

    public int Count => _allEntities.Count;

    public string Name => nameof(DomainMgrBase<T>);

    public string Desc => "";
}
