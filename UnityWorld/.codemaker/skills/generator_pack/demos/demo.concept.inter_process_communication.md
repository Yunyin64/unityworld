# Demo：进程间通信概念

## Loading Hint
- 先读本概念获取认知框架，再按 `implemented_by` 下钻到 `module / flow / slice`。

## 概念定义
- 这里的“进程间通信”不是单指底层 socket，而是项目中“消息承载 + 服务治理 + 对象级投递”这一整套通信机制。
- 它把 `ArkPipe`、`Service`、`ObjectRpc` 放到同一认知框架下，回答“消息怎么到、路由怎么定、谁来处理”。

## 为什么重要
- 排障时，很多问题不是单点代码 bug，而是协议表、注册表、路由表和运行时处理链共同作用的结果。
- 如果只看某一个模块，很容易把“控制面 RPC”“业务 RPC”“对象 RPC”混为一谈。

## 分层模型
- `ArkPipe`：负责连接、收发、线程切换、RPC 解包与基础路由。
- `Service`：负责服务地址、注册发现、switcher 转发、心跳与拓扑治理。
- `ObjectRpc`：负责对象级 RPC 定义、对象路由和对象状态投递。

## 模块映射
- `module.demo_network_component`：承载底层网络与 RPC 分发。
- `module.demo_service_ipc`：承载服务间 RPC 与服务治理。

## 典型查询路径
- 选型时：先看 `Concept` 分层，再看对应 `Module` 的边界。
- 排障时：先确定问题落在底层承载、服务治理还是对象投递，再钻到对应 `Flow / Slice`。
- 新功能设计时：先找已有对外接口和对外配置，再决定扩展点。

## 常见误区
- 把 `SwitcherRpcProList.lua` 误当成服务间业务 RPC 总表。
- 只看接口，不看注册表、协议表、映射表这类对外配置。

## 边界（In / Out）
- In：跨进程消息承载、服务间转发、对象级 RPC 传递。
- Out：具体业务 handler 内部语义、玩法状态机和对象行为细节。
