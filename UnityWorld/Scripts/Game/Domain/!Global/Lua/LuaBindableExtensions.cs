using NLua;
using UnityWorld.Core;
using UnityWorld.Game.Domain;

// 保留原全局扩展类型，避免改变既有调用；游戏对象识别和错误策略只属于 Game。
/// <summary>
/// 游戏层 Lua 绑定扩展：函数查询、调用及游戏错误处理。
/// 实现类通过 this.XXX() 直接调用，无需重写。
/// </summary>
public static class LuaBindableExtensions
{
    /// <summary>生成游戏对象的日志标识，保留原卡牌标识格式。</summary>
    public static string GetOwnerID(this ILuaBindable self)
    {
        if (self is Card c) return $"Card#{c.DefineId}";
        return self.GetType().Name;
    }
    /// <summary>
    /// 扫描 env 中所有 Lua 函数并缓存到 LuaHooks。
    /// env 为 null 时初始化空字典。应在加载 env 后立即调用。
    /// </summary>
    public static void ScanLuaHooks(this ILuaBindable self)
    {
        self.LuaHooks = LuaRuntime.ScanFunctions(self.env);
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
    /// 调用指定名称的 Lua hook 函数并返回结果。
    /// hook 不存在或返回值为空时返回 default(T)。
    /// </summary>
    public static T CallLuaHook<T>(this ILuaBindable self, string hookName, params object[] args)
    {
        var func = self.GetHookFunc(hookName);
        if (func == null) return default;
        //这里要用下HookUse，确保同来源Hook一个Tick只能用一次
        try
        {
            // Core 负责调用和转换；下方保留 Game 的致命战斗错误传播。
            return LuaRuntime.Call<T>(func, args);
        }
        catch (System.Exception ex)
        {
            foreach (var ctx in args.OfType<APIContext>()) ctx.Scene?.ThrowIfEventAborted();
            UnityWorld.Core.LogMgr.Instance.Err("[ILuaBindable]{0} hook '{1}' 异常: {2}",self.GetOwnerID(), hookName, ex.Message);
        }
        return default;
    }
}

