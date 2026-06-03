# API 参考

> 自动生成时间，共 38 个API，6 个Trigger

## Trigger（触发条件）

| ID | 描述 | Score | Weight | Tags | ConflictTags |
|---|---|---|---|---|---|
| OnUse | 使用时触发 | 0 | 1 | 主动, 使用 | - |
| OnAttack | 己方攻击拼点赢时触发 | 0 | 0 | 主动, 攻击 | - |
| OnApply | 卡牌生效时触发 | 0 | 1 | 主动, 应用 | - |
| OnStraight | 造成直击时触发 | 0 | 1 | 主动, 直击 | - |
| OnContest | 对拼时触发 | 0 | 1 | 主动, 对拼 | - |
| OnDeath | 敌人死亡时触发 | -4 | 1 | 被动, 死亡, 黑暗 | - |

## Condition（条件函数）

| 函数名 | 描述 | 参数 |
|---|---|---|
| IsFabao | 判断目标卡牌是否为法宝 | Target:CombatCard, Result:Bool |
| IsFaShu | 判断目标卡牌是否为法术 | Target:CombatCard, Result:Bool |
| IsGongFa | 判断目标卡牌是否为功法 | Target:CombatCard, Result:Bool |
| IsItem | 判断目标卡牌是否为物品 | Target:CombatCard, Result:Bool |
| IsEquip | 判断目标卡牌是否为装备 | Target:CombatCard, Result:Bool |
| IsZhaoShi | 判断目标卡牌是否为招式 | Target:CombatCard, Result:Bool |

## Action（动作函数）

| 函数名 | 描述 | 参数 |
|---|---|---|
| Heal | 恢复战斗中HP | HealValue:Int |
| SelfDamage | 自伤 | DamageValue:Int |
| ArmorBreak | 消除对方护盾值 | BreakValue:Int |
| AddNpcBuff | 给目标NPC添加Buff | Target:CombatNpc, BuffId:String, Stacks:Int, [Duration:Float] |
| AddStatBuff | 给施法者添加永久属性修正 | Target:CombatNpc, StatId:String, Value:Float, ?ModifierType:String, ?SourceId:String |
| RemoveRandomWound | 移除目标随机一张伤势卡 | Target:CombatNpc, Size:Int, ?Exact:Bool |
| Displace | 位移目标卡牌 | TargetCard:CombatCard, Position:String |
| Charge | 充能目标卡牌 | TargetCard:List<CombatCard>, ReduceTick:Int |
| Freeze | 冻结目标卡牌 | TargetCard:CombatCard, FreezeTime:Float |
| Slow | 减速目标卡牌 | TargetCard:CombatCard, Stack:Int |
| Haste | 加速目标卡牌 | TargetCard:CombatCard, Stack:Int |
| AddCardStatBuff | 给目标卡牌添加永久属性修正 | TargetCard:CombatCard, StatId:String, Value:Float |
| Convert | 灵元转化回蓝条MP | Element:String, MaxAmount:Int |
| Draw | MP转化为灵元 | Amount:Int |
| ReduceMana | 减少自身指定元素的灵元 | Element:String, Amount:Int |
| Deploy | 将卡从候补池部署到运转池 | CardId:Int |
| Recall | 将卡从运转池召回候补池 | CardId:Int |
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
| Defend | 防御拼点 | DefendType:String, DefendValue:Int |
| AllCard | 获得目标所有卡牌 | Target:CombatNpc, Result:List<CombatCard> |
| RandomCardInCD | 获得目标在CD中的一张卡牌 | Target:CombatNpc, Result:CombatCard |
| AdjacentCards | 获得目标相邻卡牌 | Target:CombatCard, Direction:string, Result:List<CombatCard> |

