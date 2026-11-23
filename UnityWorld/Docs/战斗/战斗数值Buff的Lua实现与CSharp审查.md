# 未提交 C# 逐项审查 · 数值 Buff

审查基线是 2026-11-23 已推送的 [`a54024f`](https://github.com/Yunyin64/unityworld/commit/a54024f9b8d27297afced01d8ed03751e3719d5a)。该提交只含 `UnityWorld/Data/Modifier/`。相对它，工作区有 **19 个未提交的 C# 文件、58 个差异块，增加 278 行、删除 112 行**。暂存区和未跟踪文件中均无额外 C#。下文的“撤回”均指撤销本文件的差异、保留原文件和其他改动。

## 我会怎样取舍

- **两处可直接撤回。** 01 卡牌轮次上限的平衡建议、02 定义查询注释的措辞。它们对战斗结果没有影响。
- **三处可以拆。** 03 的获盾来源传递、05 的加载器二次查定义及返回值整理、09 的治疗后事件载荷，都可与数值 Buff 本身分别裁决。现有 76 条正式 Buff 并不依靠这些附加信息。若保留公开接口承诺，应保留相应测试。
- **把实例消费、回蓝分流、双方治疗和零伤害边界留下。** 已有 Lua 能表达每个 Buff 怎么改数值。角色仍需知道在什么时候调用脚本、该消费哪个实例，以及哪一次资源写入是灵元兑换。隔离撤回已经复现了这些差异。
- **单独评审公开数值类型。** 12 和 13 把两个 C# 属性从 `int` 扩为 `long`。常规的 `2 − 5 + 4 = 1` 只需要阶段末截断；宽字段的额外收益是接近 32 位上限时仍能直接写同一份事件。代价是外部已编译的 C# Mod 需要重编，仓库内的表现层也要窄化。伤害有边界实测，拼点的对称覆盖较弱。

## 提交边界与实验方法

**远端现状。** 目录中有 4 份更新过的 JSON、76 份正式 Lua，并移除了 29 个旧名 Lua 文件。Git 识别出部分重命名。该提交中的 28 个拼点脚本调用目录外的共用函数。本地后续已将它们展开为独立 Hook；`Consume()`、有符号叠层和治疗等运行时改动也仍在工作区。隔离构建远端提交的控制台项目，首个错误为 `CS0246 CombatValueStage`；父提交的测试已经有相同的悬空类型引用。远端数据提交已经完成，完整工作区的测试结果不能用来代表远端提交。

**撤回实验。** 在临时目录中以同一提交加上当前工作区改动建立可运行基线。完整基线通过 TCA 1,876 条断言和表现核心 68 条断言。每次只把一个 C# 文件恢复为远端版本，重新构建，再跑对应入口，随后还原临时副本。前 17 个文件跑 `--tca-tests`，最后两个客户端相关文件跑 `--check-presentation-core`。**4 个通过、6 个断言失败、9 个编译失败。** 下文列的是实际观测到的首个结果；编译失败只证明文件之间有依赖，不能单独给设计判定“必需”。

**补做的局部实验。** 只撤获盾来源传递、只把 Lua 泛型返回类型换回 `bool`、只取消“盾已满”的判断，TCA 分别仍通过 1,876 条。只取消满血治疗保护，满血保留双方单次状态的断言失败。把 `Damage` 恢复为 `int` 并适配一处由 `long` 向它赋值的调用点，编译通过，但最终出伤上限断言失败。测试脚本不返回兼容探针用的整数 123 时，TCA 也仍通过。

## 按文件评审

每项列出动机、对实际行为的影响和撤销代价。各文件末尾可以展开完整 Git 差异，包括注释、空行和被删除的旧断言。

### 01．CardDefine.cs

**建议撤回。** [打开源文件](../../Scripts/Game/Data/Defines/CardDefine.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `MaxUsesPerRound` 的 `<remarks>` | 提醒调数值时同时看单次收益、灵元费用与实际发动频率。发动上限和默认值 1 都没有动。 | 撤回只影响读源码的人。它是平衡设计建议，放在现有数值设计文档更合适；数值 Buff 提交没有带它的理由。 |

整文件撤回后，通过 TCA 1,876 条断言。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 31f1659..131b6e1 100644
--- a/UnityWorld/Scripts/Game/Data/Defines/CardDefine.cs
+++ b/UnityWorld/Scripts/Game/Data/Defines/CardDefine.cs
@@ -30,2 +30,3 @@ namespace UnityWorld.Game.Data
         /// <summary>招式实例每个背包扫描轮的成功发动上限。</summary>
+        /// <remarks>因强度调整次数前，先核对单次收益、灵元费用和实际发动频率。单独限次应有明确的玩法含义。</remarks>
         public int MaxUsesPerRound { get; set; } = 1;
```

</details>

### 02．CombatModifierDefineMgr.cs

**建议撤回。** [打开源文件](../../Scripts/Game/Data/Mgr/Modifier/CombatModifierDefineMgr.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `Get` 的 XML 注释 | 把“按 ID 查询”改成“按定义 ID 查询”，并补句号。`base.Get(id)`、找不到时告警和返回值完全相同。 | 撤回不会复活旧 ID 对照表。当前提交本身已按正式 ID 查找，这行文案没有独立保留价值。 |

整文件撤回后，通过 TCA 1,876 条断言。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index ef8cdbc..82f4da5 100644
--- a/UnityWorld/Scripts/Game/Data/Mgr/Modifier/CombatModifierDefineMgr.cs
+++ b/UnityWorld/Scripts/Game/Data/Mgr/Modifier/CombatModifierDefineMgr.cs
@@ -16,3 +16,3 @@ namespace UnityWorld.Game.Data
 
-        /// <summary>按 ID 查询，不存在时记录警告并返回 null</summary>
+        /// <summary>按定义 ID 查询，不存在时记录警告并返回 null。</summary>
         public new CombatNpcModifierDefine Get(string id)
```

</details>

### 03．CombatBaseAction.cs

**保留治疗传参；获盾传参可以拆。** [打开源文件](../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `Heal` 把 `caster` 传给每个目标，更新对应注释 | 治疗量先读施术者的 `ModifyHealOut`，再读目标的 `ModifyHealIn`。单参调用只知道目标。 | 应保留。撤掉后，给别人治疗时会丢掉施术者，施术者的治疗加成不会参与；`OnHeal.SourceNpc` 也会缺失。注释应跟实际调用一致。 |
| `GainShield` 额外传 `ctx.Caster`、`ctx.SourceCard` | 两个值进入本次获盾上下文，Lua 可以辨认由谁、哪张卡给盾。当前两个正式获盾 Buff 仅读 `Amount` 和自身层数。 | 目前可撤。保留的依据是确有卡要按获盾来源匹配；仅实现现有两个 Buff 不需要。单独撤回此行的实验见下。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“受到治疗时的主体为目标，载荷记录实际回复量和施术者”。

<details>
<summary>查看本文件的完整差异（3 个 Git 差异块，1 行上下文）</summary>

```diff
index 21ea330..cd4e355 100644
--- a/UnityWorld/Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs
+++ b/UnityWorld/Scripts/Game/Domain/!Global/API/Combat/Action/CombatBaseAction.cs
@@ -96,3 +96,3 @@ namespace UnityWorld.Game.Domain
             foreach (var target in APIMgr.Instance.GetTargetNpc(ctx.GetStringValue("Domain"), ctx))
-                target.ChangeShield(amount);
+                target.ChangeShield(amount, ctx.Caster, ctx.SourceCard);
             return ctx;
@@ -147,3 +147,3 @@ namespace UnityWorld.Game.Domain
 
-        /// <summary>恢复战斗中HP。参数：Domain(String), HealValue(Int)</summary>
+        /// <summary>对 Domain 指定的角色施加 HealValue 点治疗，逐个结算施术者和目标的治疗修正。</summary>
         [APIFunc("Heal", APIType.Action, "恢复战斗中HP", Scope.CombatNpc, "Domain:String", "HealValue:Int")]
@@ -158,3 +158,3 @@ namespace UnityWorld.Game.Domain
             {
-                npc.ApplyHeal(healValue);
+                npc.ApplyHeal(healValue, caster);
             }
```

</details>

### 04．CombatManaAction.cs

**保留。** [打开源文件](../../Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 回收灵元调用 `RestoreMP`，补充兑换注释 | 回收消耗一个格子换 1 MP。此前复用 `RecoverMP`，而它现在会分派直接回蓝 Hook。 | 应保留这处分流。否则回收 1 格可能拿到多点 MP，还会用掉留给下次直接回蓝的 Buff。现有回收动作已知道来源，在入口选对方法比让每个 Lua 脚本猜来源简单。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“耗蓝、零回复和灵元回收保留单次回复提高，回收一格只增加 1 MP”。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 8d218fc..92b4187 100644
--- a/UnityWorld/Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs
+++ b/UnityWorld/Scripts/Game/Domain/!Global/API/Combat/Action/CombatManaAction.cs
@@ -53,3 +53,4 @@ namespace UnityWorld.Game.Domain
                 var removed = npc.Backpack.RemoveMana(amount, element.Kind == BaseElementType.None ? null : element, ctx.Scene.Soul);
-                var restored = removed.Count > 0 ? npc.RecoverMP(removed.Count) : 0;
+                // 回收按一格灵元换一点 MP，直接回蓝的 Buff 在回收时不生效也不消耗。
+                var restored = removed.Count > 0 ? npc.RestoreMP(removed.Count) : 0;
                 npc.Log($"回收灵元 请求={requested} 实际={removed.Count} MP差额=+{restored} 格={FormatManaCells(removed)}");
```

</details>

### 05．LuaMgr.cs

**常规战斗可整体撤；直接加载 API 的约束另算。** [打开源文件](../../Scripts/Game/Domain/!Global/Lua/LuaMgr.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 先查定义、命中后按 `define.ID` 找同名 Lua；文件不存在直接返回 | 让公开的 `LoadModifierScript(id)` 也必须有正式 Define。常规 `AddModifierCore` 已先查过定义，所以正常挂载会查两次。 | 对已登记的 76 个 Buff 没有必要。若要求独立调用该公开加载器时也拒绝孤立脚本，保留定义检查有理由；若只关心正常挂载，建议恢复原入口。旧名文件已从数据目录删除。 |
| 用 `FirstOrDefault() as LuaTable` 代替检查结果数组首项 | 两种实现都执行文件、接受返回的 table，遇到空值或错误类型都会告警并返回空。 | 纯改写，可撤。原来就有按文件加载的实现。这里没有新 Hook，也不需要运行时工厂。 |

整文件撤回后，通过 TCA 1,876 条断言。

<details>
<summary>查看本文件的完整差异（2 个 Git 差异块，1 行上下文）</summary>

```diff
index 57267d1..3ea1ec1 100644
--- a/UnityWorld/Scripts/Game/Domain/!Global/Lua/LuaMgr.cs
+++ b/UnityWorld/Scripts/Game/Domain/!Global/Lua/LuaMgr.cs
@@ -339,7 +339,8 @@ namespace UnityWorld.Game.Domain
 
-            var filePath = Path.Combine(CombatNpcModifierDefineMgr.Instance.DataDir, "Lua", $"{defineId}.lua");
-            if (!File.Exists(filePath))
-            {
-                return null;
-            }
+            var definitions = CombatNpcModifierDefineMgr.Instance;
+            var define = definitions.Get(defineId);
+            if (define == null) return null;
+
+            var filePath = Path.Combine(definitions.DataDir, "Lua", $"{define.ID}.lua");
+            if (!File.Exists(filePath)) return null;
 
@@ -347,13 +348,6 @@ namespace UnityWorld.Game.Domain
             {
-                var results = DoFileUtf8(filePath);
-
-                if (results != null && results.Length > 0 && results[0] is LuaTable modTable)
-                {
-                    return modTable;
-                }
-                else
-                {
+                var buff = DoFileUtf8(filePath)?.FirstOrDefault() as LuaTable;
+                if (buff == null)
                     LogMgr.Instance.Warn("[LuaMgr] LoadModifierScript '{0}': 脚本未 return table", defineId);
-                    return null;
-                }
+                return buff;
             }
```

</details>

### 06．CombatCardFunc.cs

**保留入槽边界，补测后再考虑合并。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatCard/CombatCardFunc.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `TryPushToPendingSlot` 在 Lua 回调后把 `FinalValue` 写回原对象 | 让槽里的 `PendingSlot.ContestValue` 从入槽那一刻起就是结算后的点数。例如 2 点受 -5 修正，槽内应存 0。 | 整文件撤回仍通过现有 TCA。原因是许多读点数的路径又读了一次 `FinalValue`；直接读槽中原字段的 Lua 仍可能看到 -3。若把这一步集中到所有入槽者共用的 `AddContestData`，当前文件这行可以撤。应验证实际槽对象的原值。 |

整文件撤回后，通过 TCA 1,876 条断言。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 2c56684..a179e75 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatCard/CombatCardFunc.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatCard/CombatCardFunc.cs
@@ -37,2 +37,3 @@ namespace UnityWorld.Game.Domain.Combat
             Owner.ModifyHook("Contest", CreateCtx("ContestData", contestData));
+            contestData.ContestValue = contestData.FinalValue;
             Owner.AddContestData(contestData);
```

</details>

### 07．CombatNpc.cs

**保留承伤阶段的两条边界。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 只对正伤害调用已有 `DamageIn` | 来源侧把伤害降到零后，目标保留易伤、抗性等单次状态。否则一笔已经落空的伤害还可能被入伤加成重新抬高。 | 保留。逐个 Lua 检查当前值会把同阶段其他 Buff 临时减到零的中间值也当成落空，触发结果会受挂载顺序影响。 |
| 全部入伤回调结束后限制到 `0..int.MaxValue` | 后续固定减伤、灵盾和 HP 使用结算点数，同阶段 Lua 可以先减过零再加回来。 | 保留一次结算。可与出伤入口复用同名数值工具，但两行 `Math.Clamp` 还不足以引入一层新接口。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“零伤害保留出伤与入伤的下次效果”。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 8416297..e3b2a6f 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs
@@ -125,3 +125,4 @@ namespace UnityWorld.Game.Domain.Combat
             ctx.Set("DamageInfo", info);
-            this.ModifyHook("DamageIn", ctx);
+            if (info.Damage > 0) ModifyHook("DamageIn", ctx);
+            info.Damage = Math.Clamp(info.Damage, 0, int.MaxValue);
```

</details>

### 08．CombatNpcData.cs

**保留获盾入口和条件。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `ChangeShield` 在能增加盾值时分派 `ShieldGain` | 正向获盾且盾未满才让 Lua 改 `Amount`；原有扣盾、吸收和耗尽通知继续按实际盾值处理。XML 与行内注释解释单次消费条件。 | 保留。去掉入口会让两条获盾定义失效。只去掉满盾判断也会在无法增加盾值时浪费一次 Buff；这和是否需要传施术者是两个问题。 |
| 在获盾上下文中携带来源、来源卡和目标 | `Caster`、`SourceCard`、`NpcTargets` 给自定义 Hook 提供来源和接收者。正式获盾脚本已经用 `m_Owner` 知道持有者。 | 数值和时机应保留。这三个字段可按已承诺的 Hook 契约取舍，不应把它们都说成现有加盾算式的必要条件。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“基础 2 与两份单次 3、4 合计 9，移除触发的新获盾只获得 1”。

<details>
<summary>查看本文件的完整差异（2 个 Git 差异块，1 行上下文）</summary>

```diff
index 502a172..82839e6 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs
@@ -31,3 +31,3 @@ namespace UnityWorld.Game.Domain.Combat
 
-        /// <summary>调整额外灵盾，盾值最低为零。</summary>
+        /// <summary>调整额外灵盾。正值受灵盾获得量修正，盾值最低为零。</summary>
         public void ChangeShield(int val) => ChangeShield(val, null, null);
@@ -36,2 +36,9 @@ namespace UnityWorld.Game.Domain.Combat
         {
+            // 获盾修正只作用于仍能增加的盾值。扣盾和盾值已达上限时保留单次状态。
+            if (val > 0 && ShieldValue < int.MaxValue)
+            {
+                var ctx = new APIContext { Caster = source, SourceCard = sourceCard, Scene = Scene, NpcTargets = [this] };
+                val = ModifyAmount("ShieldGain", val, ctx);
+            }
+
             int before = ShieldValue;
```

</details>

### 09．CombatNpcFunc.cs

**保留治疗与出伤入口；部分拼点读数可以合并。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `ApplyHeal` 保留单参调用并增加带施术者入口；先跑 `HealOut` 再跑 `HealIn` | 原来的自疗、无来源治疗仍可调用。满血或基础量非正即结束；若来源把治疗量减为零，目标仍保有单次受疗状态。最后按缺失 HP 截断。 | 保留这套时序。现有 `OnHeal` 是 HP 实际写入后的广播，不能用它代替治疗前改值而仍保持相同事件和消耗次数。 |
| `OnHeal` 新增 `Amount` 与 `SourceNpc` | `Caster` 仍是受疗者。缺 2 点、施放 5 点时广播实际回复量 2，监听者也可识别施术者。 | 这是事件数据扩充，可单独取舍。现有治疗量修正并不靠这两个字段，已有测试和触发器说明已承诺它们。若撤，应连同文档和专门的载荷断言一起调整。 |
| `AddDamage` 的 `DamageOut` 加前置条件并在入队前限幅 | 只有有来源的正伤害进入出伤 Hook；Lua 直接写同一个 `DamageInfo`。出伤阶段全部结束后限幅，再给目标入队。 | 保留。有来源的零伤害不应消费单次出伤；同一阶段的 2 − 5 + 4 应得到 1。Lua 负责加减，阶段入口负责何时提交。 |
| `RecordPending` 和 `Straight` 读取 `FinalValue` | 待发显示使用整数点数；直击从最终攻击点数生成伤害。 | 表现接口的整数转换要保留。若所有构造和入槽入口都保证原字段已提交，直击处的再次钳制可以简化。不能只依赖显示层兜底。 |
| `ResolveContest` 在配对修正后写回双方最终值，在 `OnContest` 后取得用于胜负的两个整数 | 决胜、平局、差值以及后续数值约定共用同一对点数。过去只处理下限，工作区也处理超出上限。 | 保留统一的决胜读数。第二次读取时是否还要钳制，取决于两处之间的回调能否改写原拼点。没核实写入契约前，不建议直接去掉。 |
| 双方表现快照、`WinnerValue` 和 `LoserValue` 改读最终数值 | 客户端和 Lua 事件仍收到原来范围内的整数。 | 保留输出类型。胜负已经算出时，可以复用已算好的 `valueA/valueB`，少几次 `FinalValue` 读取；这是可删的重复，不是新玩法。 |

整文件撤回后，未进入运行，CS1501: “ApplyHeal”方法没有采用 2 个参数的重载。

<details>
<summary>查看本文件的完整差异（10 个 Git 差异块，1 行上下文）</summary>

```diff
index d300268..74b4a4b 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs
@@ -33,6 +33,19 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>
-        /// 应用治疗，不超过初始HP上限。
+        /// 应用无指定施术者的治疗，只结算目标的受疗修正，实际回复量受最大 HP 限制。
         /// </summary>
-        public void ApplyHeal(int amount)
+        public void ApplyHeal(int amount) => ApplyHeal(amount, null);
+
+        /// <summary>先结算施术者的治疗量，再结算目标的受疗量，最后按目标缺失的 HP 截断。</summary>
+        internal void ApplyHeal(int amount, CombatNpc source)
         {
+            // 目标满血或基础治疗量非正时，保留双方的单次状态。
+            if (amount <= 0 || Hp >= GetCombatHpMax()) return;
+
+            var ctx = new APIContext { Caster = source, Scene = Scene, NpcTargets = [this] };
+            if (source != null)
+                amount = source.ModifyAmount("HealOut", amount, ctx);
+            // 出手治疗量归零时，保留目标的单次受疗状态。
+            if (amount <= 0) return;
+
+            amount = ModifyAmount("HealIn", amount, ctx);
             var actual = Math.Min(amount, GetCombatHpMax() - Hp);
@@ -42,3 +55,6 @@ namespace UnityWorld.Game.Domain.Combat
             RecoverHP(actual);
-            Scene.TriggerCombatEvent("OnHeal", new APIContext { Caster = this, Scene = Scene });
+            var healed = new APIContext { Caster = this, Scene = Scene };
+            healed.Set("Amount", actual);
+            healed.Set("SourceNpc", source);
+            Scene.TriggerCombatEvent("OnHeal", healed);
         }
@@ -144,3 +160,3 @@ namespace UnityWorld.Game.Domain.Combat
                 new PresentationOrigin(data.PresentationActionId, null, data.PresentationContestId, null),
-                pending: new PendingPresentation(entered, reason, data.ContestType.ToString(), data.Element.ToString(), data.ContestValue));
+                pending: new PendingPresentation(entered, reason, data.ContestType.ToString(), data.Element.ToString(), data.FinalValue));
         /// <summary>
@@ -162,3 +178,3 @@ namespace UnityWorld.Game.Domain.Combat
                 var ctx = new DamageInfo(contestData);
-                ctx.Damage = contestData.ContestValue;
+                ctx.Damage = contestData.FinalValue;
                 ctx.TargetNpc.AddDamage(ctx);
@@ -187,3 +203,3 @@ namespace UnityWorld.Game.Domain.Combat
             dmg.PresentationOrigin ??= Scene.CapturePresentationOrigin();
-            if (dmg.SourceNpc != null)
+            if (dmg.SourceNpc != null && dmg.Damage > 0)
             {
@@ -193,2 +209,3 @@ namespace UnityWorld.Game.Domain.Combat
             }
+            dmg.Damage = Math.Clamp(dmg.Damage, 0, int.MaxValue);
             damageInfos.Enqueue(dmg);
@@ -223,4 +240,4 @@ namespace UnityWorld.Game.Domain.Combat
             npcA.ApplyPairedContestModifiers(contestA, contestB);
-            contestA.ContestValue = Math.Max(0, contestA.ContestValue);
-            contestB.ContestValue = Math.Max(0, contestB.ContestValue);
+            contestA.ContestValue = contestA.FinalValue;
+            contestB.ContestValue = contestB.FinalValue;
 
@@ -235,4 +252,4 @@ namespace UnityWorld.Game.Domain.Combat
             Scene.TriggerCombatEvent("OnContest", pairedContext);
-            int valueA = contestA.ContestValue;
-            int valueB = contestB.ContestValue;
+            int valueA = contestA.FinalValue;
+            int valueB = contestB.FinalValue;
 
@@ -246,3 +263,3 @@ namespace UnityWorld.Game.Domain.Combat
                     CombatScene.PresentationCard(data.SourceCard), data.PresentationActionId,
-                    data.ContestType.ToString(), data.Element.ToString(), data.ContestValue);
+                    data.ContestType.ToString(), data.Element.ToString(), data.FinalValue);
                 Scene.RecordPresentation(CombatPresentationKind.ContestResolved, npcA, contestA.SourceCard,
@@ -289,4 +306,4 @@ namespace UnityWorld.Game.Domain.Combat
                 pairedContext.Set("LoserTypeName", loserContest.ContestType.ToString());
-                pairedContext.Set("WinnerValue", winnerContest.ContestValue);
-                pairedContext.Set("LoserValue", loserContest.ContestValue);
+                pairedContext.Set("WinnerValue", winnerContest.FinalValue);
+                pairedContext.Set("LoserValue", loserContest.FinalValue);
                 pairedContext.Set("WinnerCard", winnerContest.SourceCard);
```

</details>

### 10．CombatNpcManaFunc.cs

**保留直接回蓝与实际写入的分工。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `RecoverMP` 对正回复且未满蓝者分派 `MpRecovery` | Lua 可增加或降低直接回复量，满 MP 时保留单次状态。 | 保留。`OnMPChanged` 在 MP 变化后才触发，耗蓝和回收也会触发，无法准确表达这一笔直接回复。 |
| 提取 `RestoreMP`，两条路径共用实际缺失量裁剪与写入 | 直接回蓝带着修正后的数量调用它，灵元回收按移除格数调用它。 | 保留一个实际写入点。方法名和拆分方式可换，但若合回只有一种语义的 `RecoverMP`，就会混淆固定兑换和可加成的回蓝。 |

整文件撤回后，未进入运行，CS1061: “CombatNpc”未包含“RestoreMP”的定义，并且找不到可接受第一个“CombatNpc”类型参数的可访问扩展方法“RestoreMP”(是否缺少 using 指令或程序集引用?)。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 1bc975f..cbad561 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs
@@ -101,3 +101,16 @@ namespace UnityWorld.Game.Domain.Combat
 
+        /// <summary>直接回复 MP，结算回复量修正后返回实际回复点数。</summary>
         public int RecoverMP(int amount)
+        {
+            if (amount > 0 && Mp < GetCombatMpMax())
+            {
+                var ctx = new APIContext { Caster = this, Scene = Scene, NpcTargets = [this] };
+                amount = ModifyAmount("MpRecovery", amount, ctx);
+            }
+
+            return RestoreMP(amount);
+        }
+
+        /// <summary>按给定点数增加 MP，受缺失量限制。灵元回收通过此入口按固定比例结算。</summary>
+        internal int RestoreMP(int amount)
         {
```

</details>

### 11．CombatNpcModifierFunc.cs

**保留实例管理；把细枝末节与必需规则拆开。** [打开源文件](../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 移除触发器减层时，有符号状态向零移动；普通状态仍调用 `ReduceStack()` | 例如 -3 层衰减后为 -2。普通 `ReduceStack` 会把负数直接裁成零。 | 支持有符号状态使用减层触发器时必须保留。当前这批数值定义主要由计时或单次命中消费；若只承诺内置配置，这条扩展可单独考虑。 |
| `AddModifierCore` 叠层匹配增加已消费、已过期过滤；对现有有符号实例施加零层直接结束 | 同 ID 同到期轮的新施加不会并入正在消失的实例，零层也不会刷新时限或触发 `OnStack`。 | 保留。尤其是 `OnRemove` 又施加同 ID 的情况，玩家新拿的 Buff 应当存活。 |
| 首次创建时提前加载 Lua，再判断零层、按正负对称上限设置初始层数 | `SignedStacks` 现在来自脚本，必须先知道它，才能处理首次负层或零层；普通五行继续沿用原有上限规则。 | 保留判定顺序。它的代价是零层首次施加也可能读一份脚本。当前无需为这点代价再建一套 JSON 元数据。 |
| `StackModifier` 对有符号实例宽整数相加、双向限幅、抵消到零即移除，有限时状态按配置刷新 | -3 加 1 得 -2，再加 2 删除；超大负数不会绕回正数。未归零仍走原来的 Lua `OnStack`、属性和表现刷新。 | 保留这些行为；现有普通 `AddStack` 只有上限且用 `int` 相加，直接替回不能等价。刷新时限的几行和普通扩展有重复，可以局部复用。 |
| 普通叠层分支重排；删掉旧注释和空行 | 普通 `AddStack` 得以继续使用，与有符号逻辑分开。旧版仅正向叠加才做的 `Math.Min` 现在对负值也执行，计算出的负值相同。 | 普通状态的结果要保留。这个微小的写法差异、标题和空行没有保留理由，可以缩小 diff。 |
| `ModifyHook` 对有效实例快照调用 Lua，跳过已消费实例，遍历后清理；忽略返回值 | 同一笔事件里两个单次 Buff 都能改值；第一个的 `OnRemove` 马上触发新事件时，第二个已标记，避免重复结算。Hook 名在循环外拼一次。 | 按实例延后清理要保留。预拼字符串是局部优化；`object` 更符合不用返回值的契约，但这一个泛型参数并不承担结算，单独撤回的结果见下。 |
| 新增 `ModifyAmount` | 治疗、获盾、回蓝已有 `APIContext`。写入 `Amount`、分派 Hook、读回并在阶段末限幅，三个入口共享同一操作。 | 保留这几行或就地展开均可。撤掉前须同时改三个调用方。它只搬运数值，不解释 Buff 的类型和范围。 |
| `DoRemoveModifier` 对有符号或已消费实例先从集合移除，且重复移除直接结束；普通状态保留原通知顺序 | 避免 `OnRemove` 重入时再次处理同一旧实例；已有普通五行状态仍能收到原顺序的层数变化快照。 | 保留单次消费的去重与可重入性。强行让所有旧状态改为同一种回调顺序，会扩大行为变化。文档、参数说明与移除逻辑一起评审即可。 |

整文件撤回后，未进入运行，CS0103: 当前上下文中不存在名称“ModifyAmount”。

<details>
<summary>查看本文件的完整差异（14 个 Git 差异块，1 行上下文）</summary>

```diff
index 7adb380..2a80c49 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs
@@ -80,3 +80,7 @@ namespace UnityWorld.Game.Domain.Combat
                     long before = isElementStatus ? ReadElementStacks(mod.DefineId) : 0;
-                    mod.ReduceStack(1);
+                    // 正负数值层数都向 0 衰减一层。
+                    if (mod.SignedStacks)
+                        mod.CurrentStack -= Math.Sign(mod.CurrentStack);
+                    else
+                        mod.ReduceStack();
                     if (isElementStatus)
@@ -110,6 +114,7 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>
-        /// 添加战斗 Modifier。同 DefineId 已存在时执行叠层逻辑。
+        /// 添加战斗 Modifier。使用正式 ID 和最后生效轮查找可叠层的实例。
         /// </summary>
-        /// <param name="defineId">CombatNpcModifierDefine ID</param>
-        /// <param name="stacks">叠加层数（默认 1）</param>
+        /// <param name="defineId">战斗状态定义的 ID。</param>
+        /// <param name="stacks">叠加层数（默认 1）。数值状态接受有符号层数。</param>
+        /// <param name="duration">持续的战斗 Tick 数。正值启用计时。</param>
         public void AddModifier(string defineId, int stacks = 1,float duration = -1)
@@ -126,2 +131,3 @@ namespace UnityWorld.Game.Domain.Combat
             if (define == null) return;
+
             bool isElementStatus = IsBaseElementStatus(defineId);
@@ -130,6 +136,7 @@ namespace UnityWorld.Game.Domain.Combat
             var lastRound = GetLastEffectRound(Math.Max(0, rounds ?? define.DurationRounds));
-            var existing = Modifiers.FirstOrDefault(m => m.DefineId == defineId && m.LastRound == lastRound);
+            var existing = Modifiers.FirstOrDefault(m => m.DefineId == defineId && m.LastRound == lastRound
+                && !m.Consumed && (!m.SignedStacks || !m.IsExpired()));
             if (existing != null)
             {
-                // 叠层逻辑
+                if (existing.SignedStacks && stacks == 0) return;
                 StackModifier(existing, stacks);
@@ -142,4 +149,7 @@ namespace UnityWorld.Game.Domain.Combat
             modifier.Owner = this;
-            modifier.CurrentStack = isElementStatus && define.MaxStack > 0
-                ? Math.Min(stacks, define.MaxStack) : stacks;
+            modifier.InitializeLuaModifier();
+            if (modifier.SignedStacks && stacks == 0) return;
+            modifier.CurrentStack = modifier.SignedStacks
+                ? modifier.LimitSignedStacks(stacks)
+                : isElementStatus && define.MaxStack > 0 ? Math.Min(stacks, define.MaxStack) : stacks;
             modifier.Duration = duration;
@@ -147,3 +157,2 @@ namespace UnityWorld.Game.Domain.Combat
             if (modifier.StatModifiers.Count > 0) modifier.StatSource = $"battle-modifier:{++_modifierStatSource}";
-            modifier.InitializeLuaModifier();
             
@@ -200,5 +209,3 @@ namespace UnityWorld.Game.Domain.Combat
 
-        /// <summary>
-        /// 叠层逻辑：使用统一的 AddStack 扩展方法，含 MaxStack 限制和 RefreshOnStack。
-        /// </summary>
+        /// <summary>按实例的层数规则合并，再统一处理移除、刷新和 Lua 通知。</summary>
         private void StackModifier(CombatNpcModifier modifier, int stacks)
@@ -206,12 +213,23 @@ namespace UnityWorld.Game.Domain.Combat
             var before = (modifier.CurrentStack, modifier.RemainingTime);
-            // 大层数仍在定义允许范围内，避免 int 相加回绕。
-            if (stacks > 0)
+            if (modifier.SignedStacks)
+            {
+                modifier.CurrentStack = modifier.LimitSignedStacks((long)modifier.CurrentStack + stacks);
+                if (modifier.CurrentStack == 0)
+                {
+                    DoRemoveModifier(modifier);
+                    return;
+                }
+                if (modifier.RefreshOnStack && modifier.Duration > 0)
+                    modifier.RemainingTime = modifier.Duration;
+            }
+            else
+            {
+                // 普通状态继续使用原有叠层规则，正向叠加前留足整数空间。
                 stacks = Math.Min(stacks, int.MaxValue - Math.Max(0, modifier.CurrentStack));
-            modifier.AddStack(stacks);
-            if (before != (modifier.CurrentStack, modifier.RemainingTime)) Scene?.MarkPresentationEffect();
+                modifier.AddStack(stacks);
+            }
 
-            // 调用 OnStack hook
+            if (before != (modifier.CurrentStack, modifier.RemainingTime)) Scene?.MarkPresentationEffect();
             modifier.CallLuaHook<bool>("OnStack", modifier.env, CreateModifierCtx(modifier));
             RefreshModifierStats(modifier);
-
             Log($"[Modifier] 叠层: {modifier.DefineId} (Stack={modifier.CurrentStack})");
@@ -243,10 +261,6 @@ namespace UnityWorld.Game.Domain.Combat
         // ══════════════════════════════════════════════════════════
-        //  拼点修正管线
+        //  战斗数值修正管线
         // ══════════════════════════════════════════════════════════
 
-        /// <summary>
-        /// 通用数值修正管线：遍历所有 Modifier，调用 "Modify{HookName}" hook。
-        /// hook 签名（Lua）：function mod:ModifyXxx(ctx) — self 为 C# Modifier 实例，ctx 为 APIContext。
-        /// 需要修正的数据通过 ctx.Set 存入，Lua 侧 ctx:GetObject 取出。
-        /// </summary>
+        /// <summary>逐个调用有效 Buff 的 Lua 回调。脚本直接修改本次事件，并可消费自己的实例。</summary>
         public void ModifyHook(string hookName, APIContext ctx)
@@ -254,8 +268,21 @@ namespace UnityWorld.Game.Domain.Combat
             if (Modifiers.Count == 0) return;
-
-            foreach (var mod in GetAllModifiers())
+            var active = GetAllModifiers();
+            var hook = "Modify" + hookName;
+            foreach (var mod in active)
             {
+                if (mod.Consumed) continue;
                 ctx.Set("Modifier", mod);
-                mod.CallLuaHook<bool>("Modify" + hookName, mod.env, ctx);
+                mod.CallLuaHook<object>(hook, mod.env, ctx);
             }
+            // 全部命中的单次实例已失效，OnRemove 触发的新结算不会再次兑现它们。
+            foreach (var mod in active)
+                if (mod.Consumed) DoRemoveModifier(mod);
+        }
+
+        /// <summary>治疗、获盾和直接回蓝共用 Amount 参数，脚本可以读取本次基础点数。</summary>
+        private int ModifyAmount(string hook, int amount, APIContext ctx)
+        {
+            ctx.Set("Amount", amount);
+            ModifyHook(hook, ctx);
+            return (int)Math.Clamp(ctx.GetValue("Amount", (long)amount), 0, int.MaxValue);
         }
@@ -267,3 +294,3 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>
-        /// 主动移除指定 DefineId 的 Modifier，调用 OnRemove hook。未找到时静默跳过。
+        /// 主动移除指定定义 ID 的所有实例，并对每个实例调用 Lua OnRemove。
         /// </summary>
@@ -297,3 +324,3 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>
-        /// 内部统一移除逻辑：调用 OnRemove hook → 注销触发器监听 → 从列表移除。
+        /// 移除状态时执行 Lua OnRemove、注销触发器监听并撤销属性修正。
         /// </summary>
@@ -301,4 +328,8 @@ namespace UnityWorld.Game.Domain.Combat
         {
+            // 有符号状态先离开集合，避免移除回调重入时消费旧实例或吞掉同 ID 的新状态。
+            bool detachFirst = modifier.SignedStacks || modifier.Consumed;
             bool isElementStatus = IsBaseElementStatus(modifier.DefineId);
             long before = isElementStatus ? ReadElementStacks(modifier.DefineId) : 0;
+            if (detachFirst && !Modifiers.Remove(modifier)) return;
+
             modifier.CallLuaHook<bool>("OnRemove", modifier.env, CreateModifierCtx(modifier));
@@ -306,3 +337,3 @@ namespace UnityWorld.Game.Domain.Combat
             if (modifier.StatSource != null) Stats.RemoveModifiersBySource(modifier.StatSource);
-            Modifiers.Remove(modifier);
+            if (!detachFirst) Modifiers.Remove(modifier);
             Scene?.MarkPresentationEffect();
```

</details>

### 12．ContestData.cs

**有条件保留；公开 API 成本较高。** [打开源文件](../../Scripts/Game/Domain/Combat/ContestData.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 公开 `ContestValue` 从 `int` 扩为 `long` | 让 Lua 直接连续改同一个字段，中间值可以短时越过 32 位上限。单纯出现负数本来就能由 `int` 表示。 | 若要求直接写字段并允许任意合法 `int` 边界组合，需要更宽中间值或等价机制。日常个位数拼点不足以证明这项改动必要。外部已编译 C# 调用方要重编。 |
| 只读 `FinalValue` 把阶段结果收回 `0..int.MaxValue` | 给决胜、待发快照与 UI 提供一致的整数取值。 | 在当前宽字段设计下保留统一读法。若撤销整个宽字段方案，应连同其下游适配一并撤。伤害有越界抵消专项，本轮没有对称的拼点越界抵消专项。 |

整文件撤回后，未进入运行，CS1061: “ContestData”未包含“FinalValue”的定义，并且找不到可接受第一个“ContestData”类型参数的可访问扩展方法“FinalValue”(是否缺少 using 指令或程序集引用?)。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 70fd9f5..181f549 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/ContestData.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/ContestData.cs
@@ -16,3 +16,6 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>拼点数值（攻击值/盾值/防值）</summary>
-        public int ContestValue { get; set; } = 0;
+        public long ContestValue { get; set; }
+
+        /// <summary>拼点阶段结束后使用的点数。Lua 回调之间允许出现负数和超出 int 的中间结果。</summary>
+        public int FinalValue => (int)Math.Clamp(ContestValue, 0, int.MaxValue);
```

</details>

### 13．DamageInfo.cs

**保留宽中间值，但明确接受 API 成本。** [打开源文件](../../Scripts/Game/Domain/Combat/DamageInfo.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 公开 `Damage` 从 `int` 变为 `long`，注释说明只在阶段末裁剪 | 每个 Lua 直接用 `hit.Damage` 加减。基础 `int.MaxValue − 1`、先 +99、再 −99，最终应该回到原值。 | 保留该规则就有实质理由；仅靠每个 Lua 自己截断会丢掉溢出部分。它会破坏现有 C# 二进制属性签名，已编译 Mod 要重编。下面还做了只改回 `int` 并适配赋值点的实验。 |

整文件撤回后，未进入运行，CS0266: 无法将类型“long”隐式转换为“int”。存在一个显式转换(是否缺少强制转换?)。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 7a0f67b..080b0ce 100644
--- a/UnityWorld/Scripts/Game/Domain/Combat/DamageInfo.cs
+++ b/UnityWorld/Scripts/Game/Domain/Combat/DamageInfo.cs
@@ -19,3 +19,4 @@ namespace UnityWorld.Game.Domain.Combat
         /// <summary>基础伤害值（结算前的原始值）</summary>
-        public int Damage { get; set; } = 0;
+        // Lua 在同一阶段连续改值，暂存相加的中间结果，阶段结束才限制到战斗点数范围。
+        public long Damage { get; set; }
         public DamageType damageType { get; set; } = DamageType.Zhan;
```

</details>

### 14．CombatNpcModifier.cs

**保留实例状态和按实例消费。** [打开源文件](../../Scripts/Game/Domain/Object/Modifier/CombatNpcModifier.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `SignedStacks` 从加载完成的 Lua 表读取一次 | 声明在脚本里，现有状态实例可以区分负层有效和普通叠层。 | 保留区分能力。若让所有旧 Buff 都接受负层，会改变五行、免疫等普通计数语义。逐帧重新到 Lua 读同一标志没有收益。 |
| `Consumed` 与 `Consume()` | Lua 在真正匹配时标记自身。与 `RemoveModifier(id)` 按 ID 查找所有实例不同，这里只标记当前实例。 | 保留。可有同 ID 不同到期轮的状态；按 ID 一起删会多删，立刻执行移除回调还会引入同笔结算重入。 |
| `IsExpired()` 分流与 `LimitSignedStacks(long)` | 普通 Buff 交由已有过期策略；有符号 Buff 归零或计时结束才过期。叠层按绝对上限约束，避免最小整数取绝对值或相加溢出。 | 保留。-2 层作为减益必须能存活。具体限幅函数可以内联，但首次施加与续叠需要同一套规则。 |

整文件撤回后，未进入运行，CS1061: “CombatNpcModifier”未包含“SignedStacks”的定义，并且找不到可接受第一个“CombatNpcModifier”类型参数的可访问扩展方法“SignedStacks”(是否缺少 using 指令或程序集引用?)。

<details>
<summary>查看本文件的完整差异（3 个 Git 差异块，1 行上下文）</summary>

```diff
index a26c5f0..1dac357 100644
--- a/UnityWorld/Scripts/Game/Domain/Object/Modifier/CombatNpcModifier.cs
+++ b/UnityWorld/Scripts/Game/Domain/Object/Modifier/CombatNpcModifier.cs
@@ -29,2 +29,8 @@ namespace UnityWorld.Game.Domain
         internal string StatSource { get; set; }
+        internal bool SignedStacks { get; private set; }
+        // 同一笔结算命中的单次实例一起标记，OnRemove 重入时不能再次兑现。
+        internal bool Consumed { get; private set; }
+
+        /// <summary>Lua 在匹配的 Modify 回调里消费自身。实例立即失效，本轮回调结束后执行 OnRemove。</summary>
+        public void Consume() => Consumed = true;
         // 蓄力属于当场的 Buff 实例。每次记录实际吸收，同轮达标后只兑现一次。
@@ -70,2 +76,13 @@ namespace UnityWorld.Game.Domain
 
+        /// <summary>数值状态在层数归零或计时结束时过期，轮数由持有者另行判断。</summary>
+        internal bool IsExpired() => Consumed || (SignedStacks
+            ? CurrentStack == 0 || (Duration > 0 && RemainingTime <= 0)
+            : IModifierBaseExt.IsExpired(this));
+
+        internal int LimitSignedStacks(long stacks)
+        {
+            long limit = MaxStack > 0 ? MaxStack : int.MaxValue;
+            return (int)Math.Clamp(stacks, -limit, limit);
+        }
+
         /// <summary>
@@ -81,2 +98,4 @@ namespace UnityWorld.Game.Domain
             {
+                // 层数规则随同名脚本声明；普通状态继续使用原有过期策略。
+                SignedStacks = env["SignedStacks"] is true;
                 env["m_Self"] = this;
```

</details>

### 15．TcaAlignmentTests.Catalog.cs

**保留测试迁移；补清它验证的边界。** [打开源文件](../../Scripts/Tests/TcaAlignmentTests.Catalog.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 测试矩阵改用现有 Hook 名字，拼点 ID 等预期字符串随之改写 | 19 个文件之外已没有 `CombatValueStage` 类型。测试仍按动作目标、方向、持续/单次核对候选列表。 | 保留。恢复旧枚举引用会直接编译失败。这里的字符串用作测试预期，不参与运行时行为。 |
| 每个候选加载正式 Lua，检查对应函数和显示名；实际施加后再检查 `HasHook` | 能抓到脚本缺失、函数名写错，以及脚本返回正确但实例挂载未接通。去掉了读取 JSON 中 `Range/Stage/Once` 的断言。 | 保留文件级和实例级检查。它们不能证明每个物理、五行条件写对，具体命中和消费仍要看战斗用例。 |

整文件撤回后，未进入运行，CS0246: 未能找到类型或命名空间名“CombatValueStage”(是否缺少 using 指令或程序集引用?)。

<details>
<summary>查看本文件的完整差异（4 个 Git 差异块，1 行上下文）</summary>

```diff
index 7c905a3..ed0c522 100644
--- a/UnityWorld/Scripts/Tests/TcaAlignmentTests.Catalog.cs
+++ b/UnityWorld/Scripts/Tests/TcaAlignmentTests.Catalog.cs
@@ -71,18 +71,18 @@ public static partial class TcaAlignmentTests
         string[] damage = ["All", .. attacks.Skip(1)];
-        var cases = new (string Prefix, CombatNpc Target, int Sign, CombatValueStage Stage, string[] Ranges)[]
+        var cases = new (string Prefix, CombatNpc Target, int Sign, string Stage, string[] Ranges)[]
         {
-            ("action_self_attack_contest_boost", owner, 1, CombatValueStage.Contest, attacks),
-            ("action_target_attack_contest_weaken", opponent, -1, CombatValueStage.Contest, attacks),
-            ("action_self_defense_contest_boost", owner, 1, CombatValueStage.Contest, defenses),
-            ("action_target_defense_contest_weaken", opponent, -1, CombatValueStage.Contest, defenses),
-            ("action_self_damage_boost", owner, 1, CombatValueStage.DamageOut, damage),
-            ("action_target_damage_weaken", opponent, -1, CombatValueStage.DamageOut, damage),
-            ("action_self_damage_resist", owner, -1, CombatValueStage.DamageIn, damage),
-            ("action_target_damage_vulnerability", opponent, 1, CombatValueStage.DamageIn, damage),
-            ("action_self_heal_boost", owner, 1, CombatValueStage.HealOut, ["All"]),
-            ("action_target_heal_received_weaken", opponent, -1, CombatValueStage.HealIn, ["All"]),
-            ("action_self_shield_gain_boost", owner, 1, CombatValueStage.ShieldGain, ["All"]),
-            ("action_target_shield_gain_weaken", opponent, -1, CombatValueStage.ShieldGain, ["All"]),
-            ("action_self_mp_recovery_boost", owner, 1, CombatValueStage.MpRecovery, ["All"]),
-            ("action_target_mp_recovery_weaken", opponent, -1, CombatValueStage.MpRecovery, ["All"])
+            ("action_self_attack_contest_boost", owner, 1, "Contest", attacks),
+            ("action_target_attack_contest_weaken", opponent, -1, "Contest", attacks),
+            ("action_self_defense_contest_boost", owner, 1, "Contest", defenses),
+            ("action_target_defense_contest_weaken", opponent, -1, "Contest", defenses),
+            ("action_self_damage_boost", owner, 1, "DamageOut", damage),
+            ("action_target_damage_weaken", opponent, -1, "DamageOut", damage),
+            ("action_self_damage_resist", owner, -1, "DamageIn", damage),
+            ("action_target_damage_vulnerability", opponent, 1, "DamageIn", damage),
+            ("action_self_heal_boost", owner, 1, "HealOut", ["All"]),
+            ("action_target_heal_received_weaken", opponent, -1, "HealIn", ["All"]),
+            ("action_self_shield_gain_boost", owner, 1, "ShieldGain", ["All"]),
+            ("action_target_shield_gain_weaken", opponent, -1, "ShieldGain", ["All"]),
+            ("action_self_mp_recovery_boost", owner, 1, "MpRecovery", ["All"]),
+            ("action_target_mp_recovery_weaken", opponent, -1, "MpRecovery", ["All"])
         };
@@ -97,3 +97,3 @@ public static partial class TcaAlignmentTests
                 prefix + suffix + " 的时机显示为持续或单次");
-            var axes = stage == CombatValueStage.Contest ? "" : "_" + stage;
+            var axes = stage == "Contest" ? "" : "_" + stage;
             var expectedIds = ranges.Select(range => range + axes + "_Changed" + (once ? "_Once" : "")).ToArray();
@@ -112,9 +112,9 @@ public static partial class TcaAlignmentTests
 
-            for (int i = 0; i < expectedIds.Length; i++)
+            foreach (string id in expectedIds)
             {
-                var define = defines.Get(expectedIds[i]);
-                var change = define?.ValueChange;
-                Check(change != null && change.Range == ranges[i] && change.Stage == stage && change.Once == once
-                    && define!.DisplayName.StartsWith("单次", StringComparison.Ordinal) == once,
-                    action.ID + " 候选定义有效 " + expectedIds[i]);
+                var define = defines.Get(id);
+                using var script = LuaMgr.Instance.LoadModifierScript(id);
+                using var hook = script?["Modify" + stage] as LuaFunction;
+                Check(hook != null && define.DisplayName.StartsWith("单次", StringComparison.Ordinal) == once,
+                    action.ID + " 候选状态有对应 Lua 回调 " + id);
             }
@@ -127,3 +127,4 @@ public static partial class TcaAlignmentTests
             var other = target == owner ? opponent : owner;
-            Check(modifier?.CurrentStack == 2 * sign && modifier.Duration == (once ? -1 : 30)
+            Check(modifier?.CurrentStack == 2 * sign && modifier.HasHook("Modify" + stage)
+                && modifier.Duration == (once ? -1 : 30)
                 && other.GetAllModifiers().All(mod => mod.DefineId != selected),
```

</details>

### 16．TcaAlignmentTests.Contest.cs

**保留行为回归；旧约定可连带裁剪。** [打开源文件](../../Scripts/Tests/TcaAlignmentTests.Contest.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| `OnHeal` 监听断言实际回复量和来源 | 锁定过量治疗只报告实际值，且监听者可以识别施术者。 | 若保留事件新载荷就保留此测试。此例是自疗，来源和目标相同，单靠它不能区分二者写反；施术与受疗的实际行为另有双角色用例。 |
| 新增 `VerifyScriptedModifierHooks` 并纳入资源用例 | 用两个普通状态装载 Lua。基础盾 2，加 3、加 4，再由移除回调加 1，验证总数 10、实例已清理。另测 Lua 直接写 `Amount` 且返回 123。 | 前一部分保留，是回调重入会重复消费的实证。后一部分在需要承诺任意单个返回值均被忽略时保留；只要求 Lua 直接改值，可简化成无返回值的测试。 |
| 无来源斩伤测试改用正式 ID 和负层；刺击增伤改用新 ID | 验证 -1 与 -2 合并为 -3，无来源斩击仍受抗性影响；删除和施加均按正式 ID。 | 保留有效数值和正式 ID 的检查。撤回这些字符串会测已经删除的兼容功能。 |
| 新增真实伤害入队用例及清理 | 连续两笔 10 都变 12；交换 -5 与单次 +4 的挂载顺序都得 1；另测临时越过 `int.MaxValue` 后抵消，以及最终确实到达上限。 | 保留。前两组检验实际 Lua 与消费时机，后两组是公开伤害属性扩宽的直接证据。去掉试验后的清理会污染后续用例。 |
| 拼点测试读 `FinalValue`；删除 JSON 单次字段断言和旧闪避别名断言 | 后续断言仍以实际匹配、失配、持续和单次消费为准。旧别名已按要求删除。 | 保留正式行为测试，旧配置断言无需恢复。但这里直接读 `FinalValue`，不能代替检查真实入槽后的 `PendingSlot.ContestValue`；这是 06 项的覆盖缺口。 |

整文件撤回后，未进入运行，CS0266: 无法将类型“long”隐式转换为“int”。存在一个显式转换(是否缺少强制转换?)。

<details>
<summary>查看本文件的完整差异（7 个 Git 差异块，1 行上下文）</summary>

```diff
index aa5be15..dd92e70 100644
--- a/UnityWorld/Scripts/Tests/TcaAlignmentTests.Contest.cs
+++ b/UnityWorld/Scripts/Tests/TcaAlignmentTests.Contest.cs
@@ -17,3 +17,9 @@ public static partial class TcaAlignmentTests
         var healedHp = new List<int>();
-        var listener = new DelegateEventListener((_, _, args) => healedHp.Add(((APIContext)args).Caster.GetHp()));
+        var listener = new DelegateEventListener((_, _, args) =>
+        {
+            var healed = (APIContext)args;
+            healedHp.Add(healed.Caster.GetHp());
+            Check(healed.GetValue("Amount", 0) == 2 && ReferenceEquals(healed.GetObject("SourceNpc"), owner),
+                "受到治疗时的主体为目标，载荷记录实际回复量和施术者");
+        });
         EventMgr.Instance.RegisterEvent("tca-heal", "OnHeal", ScopeKey.Global, listener);
@@ -130,2 +136,3 @@ public static partial class TcaAlignmentTests
             var ctx = new APIContext { Caster = owner, Scene = scene };
+            VerifyScriptedModifierHooks(lua, owner);
             VerifyShieldGainBuffs(lua, ctx, owner, opponent);
@@ -139,2 +146,50 @@ public static partial class TcaAlignmentTests
 
+    // 两个普通状态直接定义获盾 Hook，验证 Lua 改值、消费与移除回调重入。
+    private static void VerifyScriptedModifierHooks(Lua lua, CombatNpc owner)
+    {
+        void Attach(string id, string script)
+        {
+            owner.AddModifier(id);
+            var buff = owner.GetAllModifiers().Single(mod => mod.DefineId == id);
+            buff.env = (LuaTable)lua.DoString(script)[0];
+            buff.env["owner"] = owner;
+            buff.env["m_Self"] = buff;
+            buff.ScanLuaHooks();
+        }
+
+        var before = owner.ShieldValue;
+        try
+        {
+            Attach("Armor", """
+                return {
+                    ModifyShieldGain = function(self, ctx)
+                        ctx:Set("Amount", ctx:GetObject("Amount") + 3)
+                        self.m_Self:Consume()
+                    end,
+                    OnRemove = function(self, ctx) self.owner:ChangeShield(1) end
+                }
+                """);
+            Attach("Fortification", """
+                return {
+                    ModifyShieldGain = function(self, ctx)
+                        ctx:Set("Amount", ctx:GetObject("Amount") + 4)
+                        self.m_Self:Consume()
+                    end
+                }
+                """);
+            owner.ChangeShield(2);
+            Check(owner.ShieldValue == before + 10 && owner.GetAllModifiers().All(m => m.DefineId != "Fortification" && m.DefineId != "Armor"),
+                "基础 2 与两份单次 3、4 合计 9，移除触发的新获盾只获得 1");
+            Attach("Armor", "return { ModifyShieldGain = function(self, ctx) ctx:Set('Amount', 5) return 123 end }");
+            owner.ChangeShield(1);
+            Check(owner.ShieldValue == before + 15, "脚本可直接修改 Amount，旧回调的单个整数返回值不计入增量");
+        }
+        finally
+        {
+            owner.RemoveModifier("Armor");
+            owner.RemoveModifier("Fortification");
+            owner.ChangeShield(before - owner.ShieldValue);
+        }
+    }
+
     private static void VerifyShieldGainBuffs(Lua lua, APIContext ctx, CombatNpc owner, CombatNpc opponent)
@@ -396,16 +451,45 @@ public static partial class TcaAlignmentTests
 
-            target.AddModifier("Zhan_Resist", 1);
+            target.AddModifier("Zhan_DamageIn_Changed", -1);
             var unsourced = new DamageInfo { Damage = 3, TargetNpc = target };
             target.ApplyDamage(unsourced);
-            Check(unsourced.Damage == 2, "旧斩击抗性对无来源斩伤仍每层减免一点");
+            Check(unsourced.Damage == 2, "无来源斩伤也受斩击入伤修正");
             target.AddModifier("Zhan_DamageIn_Changed", -2);
             Check(target.GetAllModifiers().Single(mod => mod.DefineId == "Zhan_DamageIn_Changed").CurrentStack == -3,
-                "旧抗性的正层与新抗性的负层叠入同一规范状态");
-            target.RemoveModifier("Zhan_Resist");
-            Check(!Has("Zhan_DamageIn_Changed"), "旧伤害抗性 ID 可移除对应状态");
-            source.AddModifier("Ci_Damage_Add", 2);
+                "斩击入伤修正的 -1 层与 -2 层合并为 -3 层");
+            target.RemoveModifier("Zhan_DamageIn_Changed");
+            Check(!Has("Zhan_DamageIn_Changed"), "持有者可移除斩击入伤修正");
+            source.AddModifier("Ci_DamageOut_Changed", 2);
             var stab = new DamageInfo(source, null, target, 1, DamageType.Ci, ElementType.None);
             target.AddDamage(stab);
-            Check(stab.Damage == 3, "旧物理增伤 ID 保留原正向加成");
+            Check(stab.Damage == 3, "刺击出伤修正每层增加一点伤害");
             target.DealDamage();
+            source.RemoveModifier("Ci_DamageOut_Changed");
+
+            // 从真实入队入口观察同一个 DamageInfo。大数用整数初始化，避免 float 构造参数舍入。
+            long Outgoing(int damage)
+            {
+                var hit = new DamageInfo { SourceNpc = source, TargetNpc = target, Damage = damage, damageType = DamageType.Ci };
+                target.AddDamage(hit);
+                return hit.Damage;
+            }
+
+            const string all = "All_DamageOut_Changed";
+            const string stabOnce = "Ci_DamageOut_Changed_Once";
+            source.AddModifier(all, 2);
+            Check(Outgoing(10) == 12 && Outgoing(10) == 12, "全出伤 Lua 直接改值，持续状态可以重复生效");
+            source.RemoveModifier(all);
+            foreach (bool weakenFirst in new[] { true, false })
+            {
+                if (weakenFirst) source.AddModifier(all, -5);
+                source.AddModifier(stabOnce, 4);
+                if (!weakenFirst) source.AddModifier(all, -5);
+                Check(Outgoing(2) == 1 && source.GetAllModifiers().All(m => m.DefineId != stabOnce),
+                    "两项出伤修正按任意施加顺序均为 2 - 5 + 4，单次只兑现一次");
+                source.RemoveModifier(all);
+            }
+            source.AddModifier(all, 99);
+            source.AddModifier(stabOnce, -99);
+            Check(Outgoing(int.MaxValue - 1) == int.MaxValue - 1, "同阶段中间值越过 int 上限后仍能完整抵消");
+            Check(Outgoing(int.MaxValue) == int.MaxValue, "出伤提交时钳制到 int 上限");
+            source.RemoveModifier(all);
         }
@@ -483,3 +567,3 @@ public static partial class TcaAlignmentTests
             owner.ModifyHook("Contest", ctx);
-            return contest.ContestValue;
+            return contest.FinalValue;
         }
@@ -490,5 +574,2 @@ public static partial class TcaAlignmentTests
             var once = always + "_Once";
-            Check(CombatNpcModifierDefineMgr.Instance.Get(always)?.ValueChange is { Once: false }
-                && CombatNpcModifierDefineMgr.Instance.Get(once)?.ValueChange is { Once: true },
-                id + " 的持续和下次定义均已加载");
             add.Call(ctx, "Self", always, 1, -1);
@@ -517,7 +598,2 @@ public static partial class TcaAlignmentTests
         owner.RemoveModifier("Jin_Changed");
-
-        add.Call(ctx, "Self", "Doge_Improved", 2, -1);
-        Check(Modify(ContestType.Dodge, ElementType.None) == 6, "旧闪避拼法映射到规范定义");
-        owner.RemoveModifier("Doge_Improved");
-        Check(Modify(ContestType.Dodge, ElementType.None) == 4, "旧 ID 可移除对应规范状态");
     }
```

</details>

### 17．TcaAlignmentTests.RoundAndEquipment.cs

**保留。** [打开源文件](../../Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 下次刺击的测试输入改为 `Ci_Changed_Once` | 仍验证末格生成拼点时 4 变 6，以及配对前 Buff 已消费。 | 仅替换已删除的 ID。撤回后测试不再施加有效状态，会把正确的入槽和消费时机误报为失败。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“刺击 4 在末格生成时变成 6 入槽，首次配对前一次性强化已经移除”。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 7f1d39e..688a8b8 100644
--- a/UnityWorld/Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs
+++ b/UnityWorld/Scripts/Tests/TcaAlignmentTests.RoundAndEquipment.cs
@@ -29,3 +29,3 @@ public static partial class TcaAlignmentTests
             var ctx = new APIContext { Caster = owner, SourceCard = card, Scene = scene };
-            add.Call(ctx, "Self", "Ci_Improved_Once", 2, -1);
+            add.Call(ctx, "Self", "Ci_Changed_Once", 2, -1);
             var cell = owner.Backpack.EmptyCells()[0];
```

</details>

### 18．BattleSnapshotProjector.cs

**跟随拼点属性类型，条件性保留。** [打开源文件](../../../scripts/client/battle/BattleSnapshotProjector.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| Godot 待发快照读取 `pending.FinalValue` | 快照字段是 `int`，底层可写的 `ContestValue` 现在是 `long`。读取提交后的整数让 UI 与后端结算一致。 | 当前类型选择下必须有窄化边界，直接撤回会编译失败。若撤回 12 项的公开类型变更，则这处适配也可以取消。 |

整文件撤回后，未进入运行，CS1503: 参数 5: 无法从“long”转换为“int”。

<details>
<summary>查看本文件的完整差异（1 个 Git 差异块，1 行上下文）</summary>

```diff
index 83d892d..1629a21 100644
--- a/scripts/client/battle/BattleSnapshotProjector.cs
+++ b/scripts/client/battle/BattleSnapshotProjector.cs
@@ -57,3 +57,3 @@ internal sealed class BattleSnapshotProjector
             pending == null ? null : new(pending.SourceCard.Id, pending.SourceCard.DefineId,
-                pending.SourceCard.DisplayName, pending.ContestType.ToString(), pending.ContestValue, pending.Element.ToString()));
+                pending.SourceCard.DisplayName, pending.ContestType.ToString(), pending.FinalValue, pending.Element.ToString()));
         try
```

</details>

### 19．PresentationCoreChecks.cs

**保留。** [打开源文件](../../../tools/client-observer/PresentationCoreChecks.cs)

| 改了什么 | 为什么写、产生什么结果 | 删掉的后果与建议 |
|---|---|---|
| 承伤测试改为正式斩击入伤 ID，层数从 +2 改成 -2，清理使用同一正式 ID | 5 点斩击记录成 3 点实际扣血，符合客户端表现验证目标。 | 保留。撤回会因旧 ID 无效而失去减伤，5 点被当成 5 点记录。 |
| 检查被动卡启动后的强化时读取 `Ci_Changed` | 仍然验证真实 `Passive.Start` 已施加强化并记录实际占格。 | 保留。不应为了满足测试恢复旧 Buff 别名。 |

整文件撤回后，编译通过，运行失败。首个失败断言为“DamageIn 减伤后的金额”。

<details>
<summary>查看本文件的完整差异（3 个 Git 差异块，1 行上下文）</summary>

```diff
index ef637bb..617415a 100644
--- a/tools/client-observer/PresentationCoreChecks.cs
+++ b/tools/client-observer/PresentationCoreChecks.cs
@@ -166,3 +166,3 @@ internal static class PresentationCoreChecks
         Check(initial.All(e => e.Damage!.SourceNpcId == null && e.Damage.SourceCard == null), "无来源伤害合法");
-        target.AddModifier("Zhan_Resist", 2);
+        target.AddModifier("Zhan_DamageIn_Changed", -2);
         target.ApplyDamage(new DamageInfo { Damage = 5, damageType = DamageType.Zhan });
@@ -170,3 +170,3 @@ internal static class PresentationCoreChecks
         Check(reduced.Damage!.HpDamage == 3, "DamageIn 减伤后的金额");
-        target.RemoveModifier("Zhan_Resist");
+        target.RemoveModifier("Zhan_DamageIn_Changed");
         var oldHp = target.GetHp(); target.ApplyDamage(new DamageInfo { Damage = oldHp + 5 });
@@ -212,3 +212,3 @@ internal static class PresentationCoreChecks
             "真实新增被动卡的执行身份与入包所属者一致");
-        Check(owner.GetAllModifiers().Any(m => m.DefineId == "Ci_Improved") && owner.Backpack.Placements[card.Id].Count == added.Card!.Cells.Count,
+        Check(owner.GetAllModifiers().Any(m => m.DefineId == "Ci_Changed") && owner.Backpack.Placements[card.Id].Count == added.Card!.Cells.Count,
             "真实 Passive.Start 已提交强化，入包记录包含实际成功占格");
```

</details>

## 核对正式脚本

“全伤害提高”的 Lua 直接执行 `hit.Damage = hit.Damage + self.m_Self.CurrentStack`。命中条件及是否单次消费均由对应脚本决定。28 个拼点、20 个出伤、20 个入伤、4 个治疗、2 个获盾和 2 个回蓝，共 76 个正式 ID。[持续全出伤](../../Data/Modifier/CombatModifierDefines/Lua/All_DamageOut_Changed.lua) · [单次斩击出伤](../../Data/Modifier/CombatModifierDefines/Lua/Zhan_DamageOut_Changed_Once.lua) · [单次刺击拼点](../../Data/Modifier/CombatModifierDefines/Lua/Ci_Changed_Once.lua)

上一次完整工作区验证通过 7 组控制台回归 2,509 条断言，以及表现核心 68 条。本次针对可删性重新建立基线并逐文件做了上述撤回实验。图形界面试玩不在这些实测之内。
