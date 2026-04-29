## ADDED Requirements

### Requirement: ActionDefine 支持函数调用描述
ActionDefine SHALL 新增 `funcName`（string）和 `params`（List<object>）字段，用于描述该 Action 对应的函数调用。`funcName` 对应 APIMgr 中注册的函数名，`params` 按注册签名的参数顺序提供值。

#### Scenario: ActionDefine JSON 解析
- **WHEN** 加载 ActionDefine JSON `{"id":"attack_huo_shot_3", "funcName":"Attack", "params":["Huo","SheJi",3], "score":1, "tags":["火","攻击"]}`
- **THEN** ActionDefine 实例的 FuncName 为 "Attack"，Params 为 ["Huo","SheJi",3]，其余字段正常

#### Scenario: ActionDefine 无 funcName 时兼容
- **WHEN** 加载不含 funcName 字段的 ActionDefine JSON
- **THEN** FuncName 默认为空字符串 ""，Params 默认为空列表

### Requirement: ActionData 运行时实例
系统 SHALL 提供 ActionData 类，持有 `FuncName`（string）、`Context`（ContextBase）、`DefineId`（string）。ActionData 从 ActionDefine 拷贝初始化，通过 APIMgr.ParseToContext 将 params 数组解析为带名称的 ContextBase 参数包。

#### Scenario: 从 ActionDefine 构造 ActionData
- **WHEN** 使用 ActionDefine (funcName="Attack", params=["Huo","SheJi",3]) 构造 ActionData
- **THEN** ActionData.FuncName = "Attack"，ActionData.Context 包含 {Element="Huo", PhysicalType="SheJi", AttackValue=3}

#### Scenario: 运行时修改 ActionData 参数
- **WHEN** buff 将 ActionData.Context 中 "AttackValue" 从 3 改为 4
- **THEN** 下次读取 ActionData.Context.GetValue("AttackValue") 返回 4

### Requirement: APIMgr 签名对齐设计文档
APIMgr SHALL 注册 Attack、Shield、Block 三个独立的拼点类 API 签名，替换当前的 Defend 签名。签名定义：
- Attack(Element:String, PhysicalType:String, AttackValue:Int)
- Shield(Element:String, PhysicalType:String, ShieldValue:Int)
- Block(Element:String, PhysicalType:String, BlockValue:Int)

#### Scenario: Shield 和 Block 独立注册
- **WHEN** APIMgr 初始化完成后查询 "Shield"
- **THEN** 返回 API 签名包含 Element, PhysicalType, ShieldValue 三个参数

#### Scenario: Defend 签名移除
- **WHEN** APIMgr 初始化完成后查询 "Defend"
- **THEN** 返回 null（不存在）

### Requirement: APIMgr 校验 ActionDefine 合法性
APIMgr.Validate SHALL 检查 ActionDefine 的 funcName 是否已注册，params 数量是否匹配签名。

#### Scenario: 参数数量不匹配时报错
- **WHEN** 调用 Validate("Attack", ["Huo","SheJi"])（缺少第3个参数）
- **THEN** 返回非空错误信息，包含参数数量不匹配描述