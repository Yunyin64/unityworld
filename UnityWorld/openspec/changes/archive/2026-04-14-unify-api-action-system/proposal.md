## Why

项目中目前存在两套功能重叠但互不相通的"函数注册+执行"机制：`StoryEffectFunc`（有执行、无签名、仅限 Story 域）和 `APIMgr`（有签名、无执行、仅限 Card/Action 域）。卡牌的 Action（如 Heal、SelfDamage）在战斗中没有真正的执行逻辑——`APIMgr` 只解析参数但不执行；而 Story 侧的 `StoryEffectFunc` 虽然能执行但没有参数类型定义和校验。两套系统做的是同一件事的不同侧面，需要合并为统一的 API 函数注册+执行体系。

## What Changes

- 升级 `APIMgr`：在已有的签名注册/解析/校验基础上，新增**执行能力**（`_handlers` 字典 + `Execute()` 方法）
- 新增 `APIFuncAttribute`：通过 C# Attribute 标记可执行的 API 函数，APIMgr 启动时反射扫描自动注册
- 新增 `ActionContext`：统一的执行上下文，内含 ActionData 参数 + 环境信息（主体对象、Rng 等），通过 ContextBase 机制承载
- 新增 `CombatBaseFunc`：战斗域的 Action 执行函数集合（Heal、SelfDamage 等效果类 Action 的真正实现）
- 新增 `StoryBaseFunc`：大世界域的 Action 执行函数集合（从 StoryEffectFunc 迁移全部 10 个函数）
- **迁移 `StoryEffectFunc`**：将其所有函数迁移到 `StoryBaseFunc`，`StoryEffectFunc.Execute` 改为转发到 `APIMgr.Execute`，最终 StoryEffectFunc 变为薄转发层
- 战斗侧 `CombatCardFlowHandler` 的 `ResolveEffectCard` 接入 `APIMgr.Execute`，使效果卡（非拼点卡）能够真正执行 Action

## Capabilities

### New Capabilities
- `api-action-execute`: 统一的 API 函数执行体系——APIMgr 升级为同时具备签名注册与执行能力，通过 [APIFunc] Attribute 自动扫描注册执行函数，通过 ActionContext 传递统一上下文

### Modified Capabilities
- `story-effect`: StoryEffectFunc 原子效果注册表迁移到 APIMgr 统一体系，StoryEffectFunc 变为薄转发层，所有函数实现迁移到 StoryBaseFunc 并使用 [APIFunc] Attribute 标记

## Impact

- `Scripts/Game/Domain/!Global/API/` —— APIMgr 升级 + 新增 APIFuncAttribute、ActionContext
- `Scripts/Game/Domain/!Global/API/Combat/CombatBaseFunc.cs` —— 战斗域 Handler 实现
- `Scripts/Game/Domain/!Global/API/Story/StoryBaseFunc.cs` —— 大世界域 Handler 实现（迁移自 StoryEffectFunc）
- `Scripts/Game/Domain/Story/StoryEffectFunc.cs` —— 改为转发层
- `Scripts/Game/Domain/Combat/CombatCardFlowHandler.cs` —— ResolveEffectCard 接入 APIMgr.Execute
- `Scripts/Game/Domain/Story/StoryMgr.cs` —— Story 执行链适配（StoryEffectEntry → ContextBase 转换）
