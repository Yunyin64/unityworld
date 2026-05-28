# API 代码模板与规范

## 文件位置

| APIType | 文件路径 |
|---------|----------|
| Action / Contest | `Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs` 或同目录下按功能拆分的 partial 文件 |
| Condition | `Scripts/Game/Domain/!Global/API/Combat/Condition/CombatBaseCondition.cs` |
| Story 相关 | `Scripts/Game/Domain/!Global/API/Story/StoryBaseFunc.cs` |

所有 API 函数都写在 `CombatBaseFunc` 的 partial class 中。

---

## Action / Contest 模板

```csharp
/// <summary>{描述}。参数：{Param1}({Type1}), {Param2}({Type2})</summary>
[APIFunc("{FuncName}", APIType.{Type}, "{描述}", Scope.{Scope}, "{Param1:Type1}", "{Param2:Type2}")]
public static APIContext {FuncName}(APIContext ctx)
{
    // 1. 取施法者（几乎所有 API 都需要）
    var caster = ctx.Caster;
    if (caster == null) return ctx;

    // 2. 取目标（如果不是只对自己用）
    var target = ctx.Get<CombatNpc>("Target");
    if (target == null) return ctx;

    // 3. 取参数
    int value = ctx.GetValue("ParamName", 0);
    string element = ctx.GetValue("Element", "None");

    // 4. 核心逻辑
    // ...

    // 5. 日志
    LogMgr.Instance.Dbg("[{FuncName}] {0} → {1}, 值: {2}", caster.GetName(), target.GetName(), value);

    return ctx;
}
```

---

## Condition 模板

Condition 的职责是**选择目标**或**检查条件**，结果写入 ctx：
- 选择成功：`ctx.Set<bool>("Ret", true)` + 写入 Result
- 选择失败：`ctx.Set<bool>("Ret", false)`

```csharp
/// <summary>{描述}。</summary>
[APIFunc("{FuncName}", APIType.Condition, "{描述}", Scope.{Scope}, "Target:{TargetType}", "Result:{ResultType}")]
public static APIContext {FuncName}(APIContext ctx)
{
    var caster = ctx.Caster;
    var target = ctx.Get<{TargetType}>("Target");

    // 选择逻辑
    var result = /* ... */;

    if (result != null)
    {
        ctx.Set<{ResultType}>("Result", result);
        ctx.Set<bool>("Ret", true);
    }
    else
    {
        ctx.Set<bool>("Ret", false);
    }

    return ctx;
}
```

---

## 关键 API 调用参考

### CombatNpc 可用方法

```csharp
// HP
caster.ApplyHeal(int value)
caster.AddDamage(DamageInfo dmg)
float caster.Hp
float caster.GetCombatHpMax()

// 护盾
caster.ChangeShield(float delta)  // 负数减盾
float caster.ShieldValue

// 灵元
caster.DrawMana(int amount)       // MP→灵元
caster.ConsumeMana(Dictionary<ElementType,int> cost)
caster.ManaConvert(Dictionary<ElementType,int> cost)  // 灵元→MP
bool caster.CanAffordMana(Dictionary<ElementType,int> cost)
int caster.GetManaCount(ElementType element)

// 卡组
List<CombatCard> caster.GetCardDeck()
caster.RemoveCombatCard(CombatCard card)
caster.AddCombatCard(CombatCard card)
caster.DisplaceCombatCard(CombatCard card, ComabtCardDisplaceType toPlace)
int caster.GetIndexByCard(CombatCard card)

// Modifier
caster.AddModifier(string buffId, int stacks)

// StatBuff
caster.AddStatBuff(string statId, float value, ModifierType type, string sourceId)

// 随机数（确定性）
caster.Soul.Random(int min, int max)  // [min, max)

// 目标
CombatNpc caster.Target
```

### CombatCard 可用方法

```csharp
card.Charge(int reduceTick)
card.AddCardBuff()
card.HasKeyword(string keyword) → bool
card.GetPhase() → CombatCardPhase
card.Owner → CombatNpc
card.DefineId → string
card.DisplayName → string
```

### ctx 取值方法

```csharp
ctx.Caster          // CombatNpc 施法者
ctx.SourceCard      // CombatCard 来源卡牌
ctx.Scene           // CombatScene

ctx.GetValue<T>("Key", defaultValue)  // 取基础类型参数
ctx.Get<T>("Key")                     // 取对象引用
ctx.Set<T>("Key", value)              // 写入值（Condition 用）
```

---

## 命名规范

- FuncName：PascalCase 动词短语，简洁明了（`StealMana` 不是 `StealTargetElementMana`）
- 参数名：PascalCase，语义清晰（`HealValue`、`TargetCard`、`Element`）
- 日志前缀：`[{FuncName}]`

## 禁止

- 不用 `System.Random` / `UnityEngine.Random`，用 `Soul.Random`
- 遍历中不直接修改集合（卡组操作走 `_changes` 队列）
- 不在 API 中直接 new DamageInfo 以外的复杂对象
