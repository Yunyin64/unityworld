using NLua;
using UnityWorld.Core;

var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
using (var firstRuntime = new LuaRuntime())
using (var secondRuntime = new LuaRuntime())
{
    firstRuntime.RegisterGlobal("Host", new RuntimeProbeHost());
    Require(Convert.ToInt32(firstRuntime.Execute("return Host:Double(3)")[0]) == 6, "C# 对象绑定");
    Require(firstRuntime.RegisterEnums(new[] { typeof(RuntimeProbeMode) }) == 1, "显式枚举注册数量");
    Require(Equals(firstRuntime.Execute("return RuntimeProbeMode.Ready")[0], RuntimeProbeMode.Ready), "C# 枚举值保留");
    Require(secondRuntime.Execute("return Host == nil and RuntimeProbeMode == nil")[0] is true, "跨环境绑定隔离");

    using var first = (LuaTable)firstRuntime.ExecuteFile(Path.Combine(fixtures, "instance.lua"))[0];
    using var second = (LuaTable)firstRuntime.ExecuteFile(Path.Combine(fixtures, "instance.lua"))[0];
    Require(Equals(first["Message"], "中文脚本"), "UTF-8 文件读取");
    var functions = LuaRuntime.ScanFunctions(first);
    Require(functions.Count == 4 && !functions.ContainsKey("Count"), "只扫描函数成员");
    LuaRuntime.Call(functions["Increase"], first, 3);
    Require(Convert.ToInt32(first["Count"]) == 3 && Convert.ToInt32(second["Count"]) == 0, "同脚本实例状态隔离");
    Require(LuaRuntime.Call<int>(functions["Number"]) == 7, "Lua number 转换");
    Require(LuaRuntime.Call<int>(functions["Empty"]) == 0, "无返回值保持默认值");
    Require(LuaRuntime.ScanFunctions(null).Count == 0, "空绑定扫描");

    // 两个宿主共用文件，各自持有 require 缓存与全局状态。
    var modules = Path.Combine(fixtures, "modules'probe");
    firstRuntime.AddModuleDirectory(modules);
    secondRuntime.AddModuleDirectory(modules);
    firstRuntime.Execute("require('shared_probe').Count = 9; private_value = 5");
    Require(Convert.ToInt32(secondRuntime.Execute("return require('shared_probe').Count")[0]) == 0, "模块缓存隔离及路径转义");
    Require(secondRuntime.Execute("return private_value == nil")[0] is true, "全局变量隔离");

    // 公共层必须把执行/调用错误交还调用方，不能在这里吞掉致命玩法错误。
    ExpectError(() => firstRuntime.Execute("error('runtime-probe-failure')", "probe-chunk"), "probe-chunk");
    ExpectError(() => LuaRuntime.Call(functions["Fail"]), "runtime-probe-failure");
    ExpectError(() => firstRuntime.Execute("function (", "syntax-probe"), "syntax-probe");
    Require(Convert.ToInt32(firstRuntime.Execute("return 11")[0]) == 11, "错误后仍能执行");
}

var disposed = new LuaRuntime();
disposed.Dispose();
disposed.Dispose();
try
{
    disposed.Execute("return 1");
    throw new InvalidOperationException("释放后的环境仍可执行。");
}
catch (ObjectDisposedException) { }

Console.WriteLine("LUA_RUNTIME_CHECKS_OK isolated-core utf8 bindings enums tables modules errors lifecycle");

static void Require(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description + "失败。");
}

static void ExpectError(Action action, string expected)
{
    Exception failure = null;
    try { action(); }
    catch (Exception ex) { failure = ex; }
    Require(failure != null && failure.ToString().Contains(expected, StringComparison.Ordinal), "异常传播：" + expected);
}

/// <summary>调用方显式注册的枚举，用于验证公共层不自行扫描程序集。</summary>
public enum RuntimeProbeMode { Ready = 7 }

/// <summary>模拟游戏提供给 Lua 的宿主对象，不依赖真实 Game 类型。</summary>
public sealed class RuntimeProbeHost
{
    /// <summary>用于验证带 self 参数的 C# 方法调用。</summary>
    public int Double(int value) => value * 2;
}
