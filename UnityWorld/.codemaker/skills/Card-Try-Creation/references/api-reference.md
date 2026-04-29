# 已注册 API 速查表

## 拼点类 API

| FuncName | 说明 | 参数签名 |
|----------|------|----------|
| `Attack` | 攻击 | `(Element:String, PhysicalType:String, AttackValue:Int)` |
| `Shield` | 盾防（赢了溢出加血） | `(Element:String, PhysicalType:String, ShieldValue:Int)` |
| `Block` | 格挡（赢了差值消失） | `(Element:String, PhysicalType:String, BlockValue:Int)` |

**拼点类参数说明：**
- `Element`：五行属性，可选 `"Jin"/"Mu"/"Shui"/"Huo"/"Tu"/"None"`
- `PhysicalType`：物理类型，可选 `"Zhan"/"Ci"/"Da"/"SheJi"`
- Value：拼点数值（整数）

## 效果类 API

| FuncName | 说明 | 参数签名 |
|----------|------|----------|
| `Heal` | 恢复HP | `(HealValue:Int)` |
| `SelfDamage` | 自伤 | `(DamageValue:Int)` |
| `Charge` | 充能目标卡牌，减少CD | `(TargetCardId:String, ReduceTick:Int)` |
| `Convert` | 将灵元转化回蓝条MP（1:1） | `(Element:String, MaxAmount:Int)` |
| `Draw` | 立刻将MP转化为灵元放入灵元池 | `(Amount:Int)` |
| `Freeze` | 冻结目标卡牌，暂停CD | `(TargetCardId:String, FreezeTick:Int)` |
| `ArmorBreak` | 破甲：消除对方护盾值 | `(BreakValue:Int)` |
| `AddNpcBuff` | 给目标NPC添加Buff | `(Targetint:String, BuffId:String, Stacks:Int)` |
| `AddCardBuff` | 给目标卡牌添加Buff | `(TargetCardId:String, BuffId:String, Value:Int)` |
| `Slow` | 减速目标卡牌（可叠加） | `(TargetCardId:String, X:Int)` |
| `Haste` | 加速目标卡牌（可叠加） | `(TargetCardId:String, X:Int)` |
| `ReduceMana` | 减少自身指定元素灵元 | `(Element:String, Amount:Int)` |

## 需要 Condition 选择器的 API

- `Charge`、`Freeze`、`AddCardBuff`、`Slow`、`Haste` — 需要 Condition 写入 `TargetCardId`
- `AddNpcBuff` — 需要 Condition 写入 `Targetint`
- 这些 API 的 params 中目标字段填 `""`，运行时由 Condition 选择器写入 context

## 新增 API 流程

如果用户的卡牌需要一个不在上表中的 Action：
1. 告诉用户当前没有这个 API
2. 问用户：这个 Action 具体做什么？参数是什么？
3. 一起设计 API 签名（funcName + 参数名+类型）
4. **新 API 的 funcName 必须经用户确认后才能使用！** 命名风格应简洁（如 `Charge`、`Convert`）
5. 在对应的 `*BaseFunc.cs` 中添加 `[APIFunc("FuncName", "描述", "Param:Type", ...)]` 标记的静态方法
6. 然后继续配卡