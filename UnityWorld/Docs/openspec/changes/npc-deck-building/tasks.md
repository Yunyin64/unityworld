## 1. NPC 卡组构建流程——更新设计文档

- [x] 1.1 在 `角色/修炼_功法系统.txt` 的 NPC Deck 部分，补充候选卡池（CardPool）与战斗卡组（BattleDeck）的概念分离说明
- [x] 1.2 在 `角色/修炼_功法系统.txt` 中，补充选卡策略的描述：当前随机策略 + 实战水平概念预留
- [x] 1.3 在 `角色/修炼_功法系统.txt` 中，补充卡牌排序规则说明（排序由构建策略决定，当前随机）
- [x] 1.4 在 `角色/修炼_功法系统.txt` 中，明确伤势卡不进入 CardPool，只存在于 BattleDeck

## 2. NPC 战斗属性——更新设计文档

- [x] 2.1 在 `战斗/战斗_核心机制.txt` 的 Mana 系统章节，将"每隔X秒"等描述改为"读取 NPC 的 ManaConvertInterval 属性"等表述，明确参数来源
- [x] 2.2 新增或在现有文档中补充"NPC 战斗属性清单"章节，列出 ManaConvertInterval、ManaConvertAmount、ManaElementDistribution、SimulationDepth 等属性
- [x] 2.3 在属性清单中标注每个属性的影响因素（道途/功法/境界/天赋/环境），但不定义具体计算公式

## 3. 实战水平概念——记录设计愿景

- [x] 3.1 在设计文档中记录"实战水平"机制的完整概念：K 种卡组 × N 轮对战 × 标准模型，推演次数作为 NPC 战力维度
- [x] 3.2 明确"实战水平"机制本次不实现，作为未来扩展项记录

## 4. 交叉一致性检查

- [x] 4.1 检查 `战斗/战斗_TCA体系.txt` 中 Card 实例化描述与本次"Card 是实例、可重复"的设计一致 ✓ ActionData 是运行时实例，Card 行为由 EffectData 持有 ActionData 实例，与本次设计一致
- [x] 4.2 检查 `战斗/战斗_核心机制.txt` 中 SP 机制描述与本次 CardPool/BattleDeck 分离设计无冲突 ✓ SP 约束作用于 BattleDeck，伤势卡塞入 BattleDeck 不进 CardPool，构筑博弈逻辑不变
- [x] 4.3 更新 `File.mdc` 文件地图（如有新增文件或章节变动）
