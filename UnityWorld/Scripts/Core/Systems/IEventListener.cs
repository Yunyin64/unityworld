using System;

namespace UnityWorld.Core
{
    /// <summary>
    /// 事件监听者接口。C# 和 Lua 侧均实现此接口，EventMgr 统一持有。
    /// </summary>
    public interface IEventListener
    {
        /// <summary>
        /// 当事件被触发时调用。
        /// </summary>
        /// <param name="eventId">事件字符串 ID，如 "NpcMoved"</param>
        /// <param name="scope">触发时命中的 Scope 实例</param>
        /// <param name="args">事件参数（具体类型由事件定义约定）</param>
        void OnEvent(string eventId, ScopeKey scope, object args);
    }

    /// <summary>
    /// 基于委托的事件监听者包装，供 C# 侧方便注册 lambda 或方法引用。
    /// </summary>
    public sealed class DelegateEventListener : IEventListener
    {
        private readonly Action<string, ScopeKey, object> _handler;

        /// <summary>
        /// 构造一个委托监听者。
        /// </summary>
        /// <param name="handler">事件处理委托</param>
        public DelegateEventListener(Action<string, ScopeKey, object> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc/>
        public void OnEvent(string eventId, ScopeKey scope, object args)
            => _handler(eventId, scope, args);
    }
}
