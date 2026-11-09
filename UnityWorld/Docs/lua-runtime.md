# Lua 公共执行层与游戏适配层

规则：Core 只执行 Lua、注册调用方提供的绑定；Game 决定脚本路径、API、Hook 和玩法。每个宿主持有自己的运行环境，同一脚本的可变实例 table 不缓存复用。例如，同一脚本创建两张卡，第一张的计数变成 3，第二张仍为 0。

## 代码归属

- `Scripts/Core/Lua/LuaRuntime.cs`：UTF-8 文件执行、模块搜索路径、显式全局/枚举注册、函数扫描与调用、环境释放。没有 Game 类型依赖，不持有静态 Lua 环境。
- `Scripts/Core/Base/Interface/ILuaBindable.cs`：保留原绑定数据接口与缓存开关，不引用游戏对象。
- `Scripts/Game/Domain/!Global/Lua/LuaMgr.cs`：原 LuaMgr 迁移到 Game，仍使用 `UnityWorld.Game.Domain` 命名空间；持有 LuaRuntime，组织卡牌、Modifier、Keyword、游戏枚举和 Hook 映射。
- `Scripts/Game/Domain/!Global/Lua/LuaBindableExtensions.cs`：保留原扩展类型及调用入口，游戏对象识别、日志与致命战斗错误传播留在 Game。

## 兼容约束

Lua 的 `LuaMgr`、`LogMgr`、`API` 全局名称不变，`LuaMgr:RegisterKeyword(...)` 和卡牌/Modifier 脚本返回 table 的方式不变。游戏枚举仍由 Game 选择原游戏程序集的 public enum，Core 不扫描自己的程序集。

本次保持 WorldMgr 原有的构造、Init、Begin 和反向 End 顺序；LuaMgr 的构造仍建立 Instance，Init 创建环境、绑定原全局、加载 Init.lua 与自注册 Keyword，Begin 建立事件映射，End 清空注册表后释放环境。原有游戏回调错误处理和战斗中止检查保留；公共执行器只传播异常。

不调整 Keyword 加载协议、玩法事件、调用顺序、随机数、战斗数值或正式 Data 文件。现有 keyword-system 规范对索引文件的描述与代码扫描自注册脚本存在历史差异，本次保留当前代码行为，不混入协议迁移。

## 验证

- `dotnet run --project tools/lua-runtime/LuaRuntime.Checks.csproj`：不引用后端 DLL，直接编译 Core 源文件，验证 UTF-8、显式绑定、独立状态、独立 require 缓存、异常传播与释放。
- 使用 `tools/client-observer/ClientObserver.csproj` 分别记录改动前后战斗逐 Tick 数据，并通过其 `compare` 入口检查一致性。
- 运行现有 Console 冒烟、背包战斗、时序、效果、激活、模板检查，以及表现层与属性只读检查。

本次结果：后端、Godot 客户端、观察工具和独立 Core Lua 检查均编译/运行通过；上述 8 组既有检查全部通过，激活回归也覆盖 Lua 事件循环和预算超限的明确中止。

同一固定输入下，改动前后 5,001 帧战斗验证记录逐字节相同；输入仅程序集版本标识不同，表现批次在仅排除每次新建的 `sessionId` 后逐字段相同。原比较工具的 `compare` 会先拒绝不同 `coreVersion`，故另外执行严格核对：输入只排除版本号，表现只排除会话 ID，实际战斗记录不排除任何字段。该案例在两次运行中均于 Tick 5,000 达到回合上限。

UTF-8 检查针对脚本内容；本次不扩展 Windows 原生 `require` 对非 ASCII 模块目录的支持。
