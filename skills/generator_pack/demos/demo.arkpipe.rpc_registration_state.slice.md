# Demo：RPC 注册状态

## Loading Hint
- Prefer front matter relation fields (`affects`) for on-demand loading.

## 状态字段拆解

### 状态字段1：`m_RpcInfoMap`（rpcId -> parser/handler）
- 定义/声明点：`program/engine/src/ArkPipe/CRpcDispatcher.h` + `CRpcDispatcher` + `m_RpcInfoMap`
- 写入点1：`program/engine/src/ArkPipe/CRpcDispatcher.cpp` + `RegisterRpc`
- 读取点1：`program/engine/src/ArkPipe/CRpcDispatcher.cpp` + `HandleRpc`

### 状态字段2：`m_NetThreadRpcInfoMap`（网络线程处理表）
- 定义/声明点：`program/engine/src/ArkPipe/CRpcProtocolHandler.h` + `CRpcProtocolHandler` + `m_NetThreadRpcInfoMap`
- 写入点1：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp` + `RegisterNetThreadRpc`
- 读取点1：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp` + `HandleInNetThread` + 先查网络线程 handler，命中后在 net thread 快速执行

### 状态字段3：`m_RpcIdMapping`（fromId -> toId）
- 定义/声明点：`program/engine/src/ArkPipe/CRpcProtocolHandler.h` + `CRpcProtocolHandler` + `m_RpcIdMapping`
- 写入点1：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp` + `BindRpcIdForward`
- 读取点1：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp` + `HandleInNetThread` + 命中映射后改写目标 `rpcId` 并走转发 handler

## 不变量
- 同一 `rpcId` 不允许重复注册。
- 转发目标 `rpcId` 必须有效。
