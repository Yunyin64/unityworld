# Demo：RPC 接收与分发流程

## Loading Hint
- Prefer front matter relation fields (`depends_on`, `affects`) for on-demand loading.

## 触发点
- 触发点1：网络线程收到完整 RPC 数据帧。
  - 代码定位：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp`
  - 搜索关键字：`HandleInNetThread`
- 触发点2：协议注册流程已完成（具备 `rpcId -> handler` 映射）。
  - 代码定位：`program/game/common/lua/rpc/Connection.lua`
  - 搜索关键字：`_RegisterLuaRpcHandler`、`g_RpcName2RpcId`

## 主链路
1. 网络线程从接收缓冲读取 `rpcId` 并判断是否已注册。
   - 代码定位：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp`
   - 搜索关键字：`HandleInNetThread`、`HasRpc`
2. 根据 parser 计算完整包长度并构造 `IRefBuffer`。
   - 代码定位：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp`
   - 搜索关键字：`GetRpcParseResult`、`CopyOutRecvData`
3. 按策略分流到主线程处理或网络线程快速处理。
   - 代码定位：`program/engine/src/ArkPipe/CRpcProtocolHandler.cpp`
   - 搜索关键字：`SendCmd_Main`、`m_RpcIdMapping`
4. 主线程最终执行分发 handler。
   - 代码定位：`program/engine/src/ArkPipe/CRpcDispatcher.cpp`
   - 搜索关键字：`HandleRpc`

## 终态校验
- 合法 `rpcId` 最终被处理一次。
- 非法 `rpcId` 被拦截并记录。
