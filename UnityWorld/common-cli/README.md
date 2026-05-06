# common-cli

## 解决什么问题

AI Agent（如 Claude Code）运行在终端中，需要与 GUI 工具（如 SchemaMaster）进行交互。但面临几个问题：

- GUI 工具运行在独立进程中，Agent 无法直接调用其功能
- 同一工具可能打开了多个工作区（项目），Agent 需要知道连哪个
- 不同工具需要统一的对接方式，避免每个工具各搞一套

common-cli 提供了一套统一的 **工作区发现 + 命令通信** 机制，让 Agent 能够发现正在运行的工具实例，并向指定工作区发送命令、获取结果。

```
终端 (AI Agent)                         GUI 工具
┌──────────────┐                      ┌──────────────────┐
│              │   common-cli.exe     │                  │
│  Agent       │ ──────────────────▶  │  工具进程        │
│              │   发送命令            │  接收并处理命令  │
│              │ ◀──────────────────  │  返回结果        │
│              │   获取结果            │                  │
└──────────────┘                      └──────────────────┘
```

## 命令行端（Agent 侧）

### 命令格式

```
common-cli.exe <ToolName> <WorkspaceId|auto|list> [args...]
```

| 参数 | 说明 |
|------|------|
| `ToolName` | 工具名称，与工具端注册时使用的名称一致 |
| `WorkspaceId` | 工作区 ID，支持三种模式，见下文 |
| `args...` | 传递给工具的参数，可以有多个 |

### 三种连接模式

#### 1. `list` — 列出所有存活的工作区

```bash
common-cli.exe SchemaMaster list
```

返回：

```json
{"workspaces": [{"id": "MyGame", "description": "我的游戏项目"}, {"id": "Demo", "description": "演示项目"}]}
```

#### 2. `auto` — 自动选择工作区

```bash
common-cli.exe SchemaMaster auto arg1 arg2
```

- 只有一个工作区时：自动连接并执行命令
- 没有工作区时：返回提示信息，退出码 1
- 有多个工作区时：返回工作区列表，退出码 1，由 Agent 决定连哪个

`auto` 模式的返回示例（多个工作区时）：

```json
{"workspaces": [{"id": "MyGame", "description": "..."}, {"id": "Demo", "description": "..."}], "message": "存在多个工作区，请询问用户要连接哪个工作区"}
```

#### 3. 指定工作区 ID — 直连

```bash
common-cli.exe SchemaMaster MyGame arg1 arg2
```

### 通过 stdin 传递数据

支持管道输入，stdin 内容会作为最后一个参数追加：

```bash
echo "大段数据" | common-cli.exe SchemaMaster auto arg1
# 工具端收到的 args: ["arg1", "大段数据"]

cat data.json | common-cli.exe SchemaMaster MyGame import
# 工具端收到的 args: ["import", "{...json内容...}"]
```

### 返回值

正常执行时，stdout 输出工具返回的内容，退出码为工具返回的 code（0 表示成功）。

连接失败时，stderr 输出错误信息，退出码 1。

## 工具端集成

工具端需要做两件事：**注册工作区** 和 **启动命令服务**。

### 1. 注册工作区

使用 `WorkspaceRegistry` 将当前工作区注册到共享注册表，使其可被 `common-cli.exe list/auto` 发现。

```csharp
// 创建注册表（toolName 需与 CLI 端使用的名称一致）
var registry = new WorkspaceRegistry("SchemaMaster");

// 注册工作区（id 唯一标识，description 供 Agent/用户识别）
registry.Register("MyGame", "我的游戏项目 - D:/Projects/MyGame");

// 工具关闭时注销
registry.Unregister("MyGame");
registry.Dispose();
```

- `id`：工作区唯一标识，CLI 端通过它指定连接目标
- `description`：描述信息，帮助 Agent 或用户区分多个工作区

### 2. 启动命令服务

使用 `CommonCliServer` 监听来自 CLI 的命令请求。

```csharp
var server = new CommonCliServer("SchemaMaster", "MyGame", args =>
{
    // args 是 CLI 端传来的参数数组
    // 返回 CliResponse 表示执行结果

    if (args.Length == 0)
        return CliResponse.Fail("缺少命令");

    switch (args[0])
    {
        case "ping":
            return CliResponse.Success("pong");

        case "get-schema":
            var schema = LoadSchema(args.ElementAtOrDefault(1));
            return CliResponse.Success(schema);

        default:
            return CliResponse.Fail($"未知命令: {args[0]}");
    }
});

server.Start();

// 工具关闭时
server.Stop();
```

**注意**：handler 在后台 IO 线程中执行。如果需要访问 UI 线程资源，请在 handler 内部自行 Invoke 到主线程。

### CliResponse

| 方法 | 说明 |
|------|------|
| `CliResponse.Success(output)` | 成功，code=0，output 输出到 CLI 的 stdout |
| `CliResponse.Fail(error, code)` | 失败，code 默认 1，error 输出到 CLI 的 stderr |

## 通信协议

请求和响应均为单行 JSON，以换行符 `\n` 结尾。

**请求**（CLI → 工具）：

```json
{"args": ["命令", "参数1", "参数2"]}
```

**响应**（工具 → CLI）：

```json
{"code": 0, "output": "执行结果", "error": ""}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `code` | int | 退出码，0 表示成功 |
| `output` | string | 成功时的输出内容，写入 CLI 的 stdout |
| `error` | string | 失败时的错误信息，写入 CLI 的 stderr |

## 命名约定

所有资源名称基于 `ToolName` 和 `WorkspaceId` 拼接，工具端和 CLI 端必须使用一致的命名：

| 资源 | 名称格式 |
|------|----------|
| Named Pipe | `\\.\pipe\common-cli-{ToolName}-{WorkspaceId}` |
| Memory-Mapped File | `Local\common-cli-{ToolName}` |
| Mutex | `Local\common-cli-{ToolName}-lock` |
