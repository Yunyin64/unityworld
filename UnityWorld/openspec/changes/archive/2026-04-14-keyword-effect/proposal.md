## Why

当前 EffectData 只支持 TCA（Trigger+Condition+Action）模式，无法表达卡牌的「存在方式修饰」——
如战斗开始即就绪（Initial）、用完即消耗（Consume）、弹药限次（Ammo）、武器引用（Weapon）等。
这些机制在文档 `战斗_机制原子清单.txt` 中被定义为 **Keyword Effect**，是 TCA Effect 之外的第二种 Effect 模式。

本次变更搭建 Keyword Effect 的底层基础设施，并以 `Initial`（初始）作为第一个端到端示例跑通。

## What Changes

- **EffectDefine** / **EffectData** 新增 `IsKeyword`、`KeywordParams` 字段，与 TCA 字段互斥共存
- **CardMgr.BuildEffectFromDefine** 扩展分支：IsKeyword=true 时走 Keyword 构建路径
- **CombatScene / CombatNpc** 在 `InitCardStates` 之后增加 Keyword 初始化阶段，扫描卡牌的 Keyword Effect 并按类别分发执行
- **Initial Keyword 实现**：战斗开始时将带 Initial 的卡牌 CD 设为满值，第一个 Tick 即可触发
- 新增一条 Initial 的 EffectDefine JSON 数据 + 一张测试用 CardDefine JSON 引用它

## Capabilities

### New Capabilities
- `keyword-effect`: Keyword Effect 底层基础设施（数据模型、枚举、构建路径、战斗初始化阶段分发）+ Initial 关键词的完整实现

### Modified Capabilities
<!-- 无需修改已有 spec -->

## Impact

- `Scripts/Game/Data/Defines/EffectDefine.cs` — 新增 IsKeyword、KeywordParams 两个 JSON 属性
- `Scripts/Game/Domain/Object/Card/Data/EffectData.cs` — 新增 IsKeyword、KeywordParams 两个运行时字段
- `Scripts/Game/Domain/Object/Card/CardMgr.cs` — BuildEffectFromDefine 增加 Keyword 分支
- `Scripts/Game/Domain/Combat/CombatCardState.cs` — 可能新增 Keyword 初始化方法
- `Scripts/Game/Domain/Combat/CombatScene.cs` — PreStart/Start 阶段增加 Keyword 初始化扫描
- `Data/Effect/` — 新增 Initial keyword 的 EffectDefine JSON
- `Data/Card/` — 新增测试卡牌引用 Initial Effect