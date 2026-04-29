/// <summary>
    /// 子系统通用接口基类
    /// </summary>
    public interface ISystemBase<T>
{
        void OnTick(T obj, float deltaTime);
}