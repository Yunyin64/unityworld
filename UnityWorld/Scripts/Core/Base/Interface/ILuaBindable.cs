using NLua;

/// <summary>
/// Lua 绑定能力接口：持有 Lua env 和预扫描的 LuaHooks 缓存。
/// 所有需要 Lua 脚本驱动的运行时对象（Modifier、Card 等）实现此接口。
/// 方法实现通过 <see cref="LuaBindableExtensions"/> 扩展方法提供，实现类无需重写。
/// </summary>
public interface ILuaBindable
{
    /// <summary>Lua 脚本返回的 table（原始 env）</summary>
    public LuaTable env { get; set; }

    /// <summary>预扫描的 Lua 函数缓存：hookName → LuaFunction</summary>
    public Dictionary<string, LuaFunction> LuaHooks { get; set; }
}

/// <summary>
/// ILuaBindable 扩展方法：ScanLuaHooks、HasHook、GetHookFunc、CallLuaHook、CallLuaHookWithReturn。
/// 实现类通过 this.XXX() 直接调用，无需重写。
/// </summary>
public static class LuaBindableExtensions
{
    /// <summary>
    /// 扫描 env 中所有 Lua 函数并缓存到 LuaHooks。
    /// env 为 null 时初始化空字典。应在加载 env 后立即调用。
    /// </summary>
    public static void ScanLuaHooks(this ILuaBindable self)
    {
        self.LuaHooks = new Dictionary<string, LuaFunction>();
        if (self.env == null) return;
        foreach (var key in self.env.Keys)
        {
            if (key is string s && self.env[s] is LuaFunction func)
                self.LuaHooks[s] = func;
        }
    }

    /// <summary>
    /// 判定是否存在指定名称的 Lua hook。
    /// </summary>
    public static bool HasHook(this ILuaBindable self, string hookName)
    {
        return self.LuaHooks != null && self.LuaHooks.ContainsKey(hookName);
    }

    /// <summary>
    /// 获取指定 hook 的 LuaFunction 引用。
    /// 根据全局开关选择缓存路径或老路径。不存在返回 null。
    /// </summary>
    public static LuaFunction GetHookFunc(this ILuaBindable self, string hookName)
    {
        if (LuaBindableConfig.UseLuaHooksCache)
        {
            if (self.LuaHooks != null && self.LuaHooks.TryGetValue(hookName, out var f))
                return f;
            return null;
        }
        else
        {
            if (self.env == null) return null;
            return self.env[hookName] as LuaFunction;
        }
    }

    /// <summary>
    /// 调用指定名称的 Lua hook 函数（无返回值），参数自由传入。
    /// hook 不存在时静默跳过。
    /// </summary>
    public static void CallLuaHook(this ILuaBindable self, string hookName, params object[] args)
    {
        var func = self.GetHookFunc(hookName);
        if (func == null) return;

        try
        {
            func.Call(args);
        }
        catch (System.Exception ex)
        {
            UnityWorld.Core.LogMgr.Err("[ILuaBindable] hook '{0}' 异常: {1}", hookName, ex.Message);
        }
    }

    /// <summary>
    /// 调用指定名称的 Lua hook 函数并返回结果。
    /// hook 不存在或返回值为空时返回 default(T)。
    /// </summary>
    public static T CallLuaHookWithReturn<T>(this ILuaBindable self, string hookName, params object[] args)
    {
        var func = self.GetHookFunc(hookName);
        if (func == null) return default;

        try
        {
            var results = func.Call(args);
            if (results != null && results.Length > 0 && results[0] is T val)
                return val;
            // Lua 返回 number 时可能是 double，需要转换
            if (results != null && results.Length > 0 && results[0] != null)
                return (T)System.Convert.ChangeType(results[0], typeof(T));
        }
        catch (System.Exception ex)
        {
            UnityWorld.Core.LogMgr.Err("[ILuaBindable] hook '{0}' 异常: {1}", hookName, ex.Message);
        }
        return default;
    }
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
