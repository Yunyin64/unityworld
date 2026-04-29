# JSON 格式参考 + ID 命名规范

## ActionDefine 条目格式

```json
{
  "ID": "action_xxx",
  "DisplayName": "中文名",
  "desc": "描述",
  "score": 1,
  "funcName": "Attack",
  "params": ["Huo", "SheJi", 3],
  "tags": ["火", "攻击"],
  "conflictTags": [],
  "Weight": 1
}
```
- `score`：正数，表示强度消耗（攻击值越高 score 越高，大致 1:1）
- `params`：有序参数列表，与 API 签名一一对应
- 没有 `funcName` 的旧 Action 不要模仿，新 Action 必须有 `funcName` + `params`

## EffectDefine 条目格式（TCA 模式）

```json
{
  "ID": "eff_xxx",
  "DisplayName": "中文名",
  "desc": "描述",
  "triggerId": "trigger_on_use",
  "conditionId": "",
  "actionIds": ["action_xxx", "action_yyy"],
   
  "complexityScore": 0
}
```
- `conditionId`：空字符串 `""` 表示无条件
- `powerScore` / `complexityScore`：可填0，运行时会重新计算

## EffectDefine 条目格式（Keyword 模式）

```json
{
  "ID": "kw_initial",
  "DisplayName": "初始",
  "Desc": "战斗开始时 CD 已满，第一个 Tick 即可触发",
  "IsKeyword": true,
  "KeywordParams": {},
  "PowerScore": 0,
  "ComplexityScore": 0
}
```
- `IsKeyword: true` 时，`TriggerId/ConditionId/ActionIds` 被忽略
- Keyword 的类别由 EffectDefine 的 **ID 本身**标识（如 `kw_initial`），代码中按 ID 做 switch 分发
- `KeywordParams`：无参数时为 `{}`；带参数的 Keyword 示例：`{"Uses": "2"}`（Ammo/Consume）
- Keyword Effect 和 TCA Effect 共存于 CardDefine 的 `effectIds` 列表中

### 已有 Keyword 列表

| ID | 名称 | 说明 | 参数 | 介入时机 |
|---|---|---|---|---|
| `kw_initial` | 初始 | 战斗开始 CD 设满，第一个 Tick 即可触发 | 无 | 初始化 |

### 卡牌中使用 Keyword 的示例

```json
{
  "ID": "card_example",
  "DisplayName": "速斩",
  "effectIds": ["kw_initial", "eff_jin_slash_use"],
  "tags": ["初始", "斩", "攻击"]
}
```
上面这张卡：战斗开始时 CD 就满了（kw_initial），触发后执行金斩击（eff_jin_slash_use）

## CardDefine 条目格式

```json
{
  "ID": "card_xxx",
  "DisplayName": "中文名",
  "desc": "描述",
  "rarity": 0,
  "size": 1,
  "Cooldown": 3,
  "cardType": "ZhaoShi",
  "manaCost": {},
  "effectIds": ["eff_xxx"],
  "tags": ["斩", "攻击"]
}
```
- `cardType`：`"ZhaoShi"` / `"FaShu"` / `"FaBao"` / `"DanYao"` / `"ZhenFa"` / `"ShenTong"`
- `manaCost`：`{}` 无灵元需求；`{"Huo": 1}` 需1火灵元
- `effectIds`：引用 EffectDefine 的 ID 列表

## ID 命名规范

| 类型 | 前缀 | 示例 |
|------|------|------|
| Card | `card_` | `card_swift_slash`, `card_fireball` |
| Effect | `eff_` | `eff_swift_slash_use`, `eff_fireball_use` |
| Action | `action_` | `action_atk_zhan_2`, `action_heal_3` |
| Trigger | `trigger_` | `trigger_on_use` |
| Condition | `cond_` | `cond_none` |

Action ID 建议用 `action_{功能}_{参数概要}`，方便复用。
只用于一张特定卡的也可用 `action_{卡名}_{功能}`。

## JSON 文件路径

所有数据文件位于项目根目录 `Data/` 下：
- `Data/Action/` — Action 定义（已拆分为多个文件）
  - `Data/Action/Action_Attack.json` — 攻击类 Action（Attack/ArmorBreak/SelfDamage）
  - `Data/Action/Action_Defense.json` — 防御类 Action（Shield/Block/护甲）
  - `Data/Action/Action_Support.json` — 辅助类 Action（治疗/控制/充能/转化/回灵/减灵等）
- `Data/Effect/` — Effect 定义（已拆分为多个文件）
  - `Data/Effect/Effect_Element.json` — 五行元素相关 Effect
  - `Data/Effect/Effect_Wound.json` — 伤势 Effect
  - `Data/Effect/Effect_Keyword.json` — Keyword Effect（kw_initial 等）
- `Data/TriggerDefines.json`
- `Data/ConditionDefines.json`
- `Data/Card/` — 战斗卡牌有分类文件
你优先读取下Data/Card/的结构，然后推断应该放到哪里

编辑 JSON 时：在数组末尾追加新条目，**不要删除已有条目**，确保 ID 不重复。