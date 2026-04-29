## Context

Day 1 已实现 Tick 驱动战斗引擎（CombatScene.Tick、待发槽、对拼结算、伤势→SP溢出判负），但所有拼点数值通过 `CardData.ContestValue/ContestType/PhysicalType` 占位字段硬编码。现在需要将战斗数值的来源切换到 TCA 体系的 ActionDefine → ActionData 数据流。

当前代码现状：
- `ActionDefine` 只有 desc/score/weight/tags/conflictTags，没有 funcName/params
- `EffectData` 持有 `List<string> ActionIds`（ID 引用），不是运行时实例
- `CardData` 有 3 个标记为 `⏳Day2移除` 的临时字段
- `CombatCardState` 的 `GetContestValue/GetContestType` 等方法直接读 CardData 占位字段
- `APIMgr` 已存在，注册了 Attack/Defend/Heal/SelfDamage，但 Defend 签名与设计文档（Shield/Block 分离）不匹配
- `ContextBase` 已存在，是通用的 string→object 字典，可直接复用

## Goals / Non-Goals

**Goals:**
- ActionDefine 支持 `funcName` + `params` 字段，JSON 可读性高
- ActionData 运行时实例持有 ContextBase，可被 buff 修改
- EffectData 持有 `List<ActionData>` 替代 `List<string> ActionIds`
- CardDefine/CardData 新增 cardType + manaCost
- 移除 CardData 上的临时占位字段，拼点数值完全从 ActionData 提取
- 新增 ContestData 临时结构，拼完即丢
- APIMgr 签名与设计文档对齐（Attack/Shield/Block 三分离）
- Mana 基础框架（ManaPool 结构、转化、消耗检查）
- CombatScene Tick 中接入 Mana 定期转化
- 所有现有 JSON 数据文件更新到新格式

**Non-Goals:**
- 不实现具体的 Action 执行器/Resolver（Day 3-4 的事）
- 不实现五行相克加成
- 不设计具体的 30 张卡牌数据（Day 3）
- 不实现 Trigger/Condition 的运行时执行逻辑
- 不实现法宝的"激活N次免费"等复杂 Mana 消耗模式（仅框架）

## Decisions

### 1. ActionDefine.Params 使用 `List<object>` 有序数组而非命名字典

- **选择**：JSON 中 `"params": ["Huo", "SheJi", 3]`，运行时由 APIMgr 按签名索引解析为命名参数
- **原因**：对策划/AI 高可读性（直观看到 `Attack(Huo, SheJi, 3)`），同时 APIMgr 提供名称映射和校验
- **替代方案**：直接在 JSON 中用命名字典 `{"Element":"Huo",...}` → 放弃，冗余且失去"函数调用"的直觉感

### 2. EffectData 直接持有 `List<ActionData>` 实例

- **选择**：EffectData 初始化时即实例化 ActionData 列表，战斗中直接操作实例
- **原因**：buff 需要修改具体 ActionData 的参数值（如 +1 攻击），持有引用才能修改
- **替代方案**：保留 ActionIds + 延迟解析 → 放弃，每次拼点都要重新解析，且无法保存 buff 修改

### 3. ContestData 是临时值对象，不持久化

- **选择**：CD 满时从 ActionData 构造 ContestData，塞入待发槽；拼完后丢弃
- **原因**：拼点数据是 ActionData 的"快照视图"，不是独立实体。拼完后无意义
- **替代方案**：直接把 ActionData 塞进待发槽 → 放弃，ActionData 可能被 buff 继续修改，需要固定快照

### 4. APIMgr 中 Shield 和 Block 分别注册

- **选择**：`Shield(Element, PhysicalType, ShieldValue)` 和 `Block(Element, PhysicalType, BlockValue)` 独立注册
- **原因**：设计文档明确区分盾（赢了溢出加血）和防（赢了无收益），FuncName 不同便于战斗系统判断
- **替代方案**：统一 Defend + 子类型参数 → 放弃，增加判断复杂度

### 5. Mana 转化 Handler 作为 CombatScene 的组件

- **选择**：新增 `CombatManaHandler` 类，被 CombatScene 持有和调用，非独立 Mgr
- **原因**：Mana 逻辑只在战斗内存在，不需要全局单例。与 CombatContestHandler 等同级
- **替代方案**：全局 ManaMgr → 放弃，Mana 是战斗内状态，不应跨战斗共享

### 6. CardData.ManaCost 使用 `Dictionary<string, int>` 

- **选择**：`Dictionary<string, int>`（key 为元素名称字符串，value 为数量）
- **原因**：与 JSON `{"Huo":1}` 格式直接对应，简洁明了
- **替代方案**：`Dictionary<BaseElementType, int>` → 可行但 JSON 序列化需要自定义转换器，先用字符串简化

## Risks / Trade-offs

- **[EffectData 结构性变更]** 从 `List<string>` 到 `List<ActionData>` 是破坏性改动 → 所有构造 EffectData 的地方都需更新（CardSystemGenerate、手配加载逻辑）
- **[JSON 向后兼容]** 已有 ActionDefines.json 格式变更 → 需要同步更新所有 JSON 文件，旧格式不兼容
- **[APIMgr 初始化顺序]** APIMgr 必须在 ActionData 构造之前初始化 → WorldMgr.Initialize 中确保 APIMgr 注册顺序在前
- **[Mana 仅框架]** 转化规则（什么元素、多少量）暂用最简规则，不深入平衡 → Day 4 跑通后调参