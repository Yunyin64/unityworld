using NLua;

/// <summary>
/// Lua 绑定能力接口：持有 Lua env 和预扫描的 LuaHooks 缓存。
/// 所有需要 Lua 脚本驱动的运行时对象（Modifier、Card 等）实现此接口。
/// 这里只声明绑定数据；游戏调用策略由 Game 层的扩展方法提供。
/// </summary>
public interface ILuaBindable
{
    /// <summary>Lua 脚本返回的 table（原始 env）</summary>
    public LuaTable env { get; set; }

    /// <summary>预扫描的 Lua 函数缓存：hookName → LuaFunction</summary>
    public Dictionary<string, LuaFunction> LuaHooks { get; set; }
    /// <summary>当前帧Hook的使用情况</summary>
    public HashSet<string> HookUse { get; set; }
}

/// <summary>
/// ILuaBindable 全局配置。
/// </summary>
public static class LuaBindableConfig
{
    /// <summary>
    /// 全局开关：true 时 CallLuaHook 走 LuaHooks 缓存路径，false 时走老路径 env[hookName]。
    /// 用于 AB 测试，确认无回归后可移除老路径。
    /// </summary>
    public static bool UseLuaHooksCache = true;
}
