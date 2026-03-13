namespace UnityWorld.Core
{
    /// <summary>
    /// Lua 事件监听者占位实现。
    /// TODO: 待 Lua 集成（XLua 等）时替换为真实实现，将 OnEvent 回调桥接到 Lua 函数。
    /// </summary>
    public sealed class LuaEventListener : IEventListener
    {
        /// <summary>Lua 侧函数标识（用于调试定位）</summary>
        public string LuaFuncName { get; }

        /// <summary>
        /// 构造一个 Lua 事件监听者占位。
        /// </summary>
        /// <param name="luaFuncName">Lua 函数标识符（调试用）</param>
        public LuaEventListener(string luaFuncName = "unknown")
        {
            LuaFuncName = luaFuncName;
        }

        /// <inheritdoc/>
        public void OnEvent(string eventId, ScopeKey scope, object args)
        {
            // TODO: 实现 Lua 桥接，调用 XLua 函数
            LogMgr.Warn("[LuaEventListener] 尚未实现 Lua 集成，事件被忽略。eventId={0} luaFunc={1}", eventId, LuaFuncName);
        }
    }
}
