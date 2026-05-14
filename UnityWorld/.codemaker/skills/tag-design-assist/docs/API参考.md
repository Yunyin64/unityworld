# API 参考

> 自动生成时间，共 30 个API，4 个Trigger

## Trigger（触发条件）

| ID | 描述 | Score | Weight | Tags | ConflictTags |
|---|---|---|---|---|---|
| trigger_on_use | 使用时触发 | 0 | 1 | 主动, 使用 | - |
| trigger_on_attack | 攻击时触发 | 0 | 0 | 主动, 攻击 | - |
| trigger_on_burn | 敌人触发燃烧时 | -3 | 1 | 被动, 火, 燃烧 | - |
| trigger_on_death | 敌人死亡时触发 | -4 | 1 | 被动, 死亡, 黑暗 | - |

## Condition（条件函数）

| 函数名 | 描述 | 参数 |
|---|---|---|
| AllCard | 获得目标所有卡牌 | - |
| RandomCardInCD | 获得目标在CD中的一张卡牌 | - |
| AdjacentCards | 获得目标相邻卡牌 | Direction:String |

## Action（动作函数）

| 函数名 | 描述 | 参数 |
|---|---|---|
| Heal | 恢复战斗中HP | HealValue:Int |
| SelfDamage | 自伤 | DamageValue:Int |
| ArmorBreak | 消除对方护盾值 | BreakValue:Int |
| AddNpcBuff | 给目标NPC添加Buff | BuffId:String, Stacks:Int |
| AddStatBuff | 给施法者添加永久属性修正 | StatId:String, Value:Float, ?ModifierType:String, ?SourceId:String |
| RemoveWound | 移除己方一张伤势卡 | - |
| Displace | 位移目标卡牌 | - |
| Charge | 充能目标卡牌 | ReduceTick:Int |
| Freeze | 冻结目标卡牌 | FreezeTime:Float |
| Slow | 减速目标卡牌 | - |
| Haste | 加速目标卡牌 | - |
| Convert | 灵元转化回蓝条MP | Element:String, MaxAmount:Int |
| Draw | MP转化为灵元 | Amount:Int |
| ReduceMana | 减少自身指定元素的灵元 | Element:String, Amount:Int |
| GiveTrait | 给NPC添加特质 | int:String, TraitId:String |
| RemoveTrait | 移除NPC特质 | int:String, TraitId:String |
| GiveBehaviorCard | 给NPC添加行为卡 | int:String, CardDefineId:String |
| ModifyAura | 修改地块五行浓度 | PlaneId:String, Element:String, Delta:Float |
| ModifyStat | 修改NPC属性值 | int:String, StatId:String, Delta:Float |
| TriggerStory | 链式触发Story | StoryId:String, SubjectId:String |
| TriggerStoryByTag | 按Tag匹配触发Story | Tags:String |
| AddToFatePool | 向宿命池写入条目 | SubjectId:String, Time:Float, StoryId:String |
| AddToKarmaPool | 向劫缘池写入条目 | SubjectId:String, StoryId:String, Weight:Float |
| TriggerEvent | 通过EventMgr广播事件 | EventName:String |

## Other（其他/未分类）

| 函数名 | 描述 | 参数 |
|---|---|---|
| Attack | 造成伤害（攻击拼点） | Element:String, PhysicalType:String, AttackValue:Int |
| Shield | 盾牌防御（赢了叠盾） | ShieldValue:Int |
| Block | 格挡防御（赢了差值消失） | BlockValue:Int |

