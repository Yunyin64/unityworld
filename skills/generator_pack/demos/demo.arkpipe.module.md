# Demo：网络组件模块

## Loading Hint
- Prefer front matter relation fields (`submodules`, `depends_on`, `affects`) for on-demand loading; body focuses on this document itself.

## 模块定位
- 承载网络连接、命令投递、RPC 分发的通用能力。

## 对外使用（Public Surface）
- 对外接口1：`INetworkMgr::StartUp / ShutDown`
  - 作用：启动/关闭网络组件生命周期。
  - 使用前置：网络线程参数、监听配置已准备完成。
  - 输入/输出：输入配置对象；输出启动是否成功与运行态切换结果。
  - 使用样例：`CNetworkMgr::StartUp` -> `CreateNetworkThread` -> `StartNetworkThread`
- 对外接口2：`CRpcDispatcher::RegisterRpc / HandleRpc`
  - 作用：注册 RPC 契约并在运行时完成分发。
  - 使用前置：`rpcId`、parser、handler 已绑定且不冲突。
  - 输入/输出：输入 `rpcId + 回调`；输出分发执行结果（成功/拦截/错误）。
  - 使用样例：`RegisterRpc` 后由 `CRpcProtocolHandler::HandleInMainThread` 进入 `HandleRpc`
- 对外配置1：`program/game/common/lua/rpc/Connection.lua` 中的 `g_RpcName2RpcId`
  - 作用：以映射表形式对外暴露协议注册结果，驱动运行时 `rpcName -> rpcId` 查找。
  - 配置前置：协议注册流程已完成，Lua 侧 RPC 表已装载。
  - 关键字段/取值：键为 `rpcName`，值为 `rpcId`；要求同一 `rpcName` 不能重复映射。
  - 使用样例：`_RegisterLuaRpcHandler` -> `g_RpcName2RpcId`
- 对外配置2：`无显式对外配置`
  - 作用：无。
  - 配置前置：无。
  - 关键字段/取值：无。
  - 使用样例：`无`

## 总体架构（One-screen）
- 核心运作原理：
  - 网络线程负责收发包与初步解包。
  - 协议处理层按 `rpcId` 判定处理路径（net thread / 主线程 / 转发）。
  - 主线程命令队列承接需要切回主逻辑线程的 RPC。
  - 分发器执行最终业务 handler，形成端到端调用闭环。
- 组件与职责：
  - `CNetworkMgr`：组件生命周期、线程与连接管理。
  - `CRpcProtocolHandler`：包解析、处理路径选择、线程切换。
  - `CRpcDispatcher`：RPC 注册与最终分发执行。
- 失败/降级总览：
  - 非法 `rpcId`、解析失败、未注册 handler 走统一拦截/日志路径，不进入业务 handler。

## 边界（In / Out）
- In：连接生命周期、线程间命令投递、RPC 分发执行。
- Out：玩法协议语义定义、业务状态机流转策略。

## 拆分判定（主模块/子模块）
- 当前可给出“一屏架构”，保持单主模块 + 子模块拆分模式。

## 关键实现定位（文件 + 搜索关键字）
- `program/engine/src/ArkPipe/CNetworkMgr.cpp`
  - 搜索关键字：`StartUp`、`ProcessMainThread`
- `program/engine/src/ArkPipe/CRpcProtocolHandler.cpp`
  - 搜索关键字：`HandleInNetThread`
