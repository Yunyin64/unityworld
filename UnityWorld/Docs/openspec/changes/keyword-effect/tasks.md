## 1. 更新设计文档：TCA 体系

- [x] 1.1 在 `战斗/战斗_TCA体系.txt` 的「九、卡牌模型」章节补充 Effect 的 IsKeyword 分支：EffectData 支持 TCA 模式和 Keyword 模式两种，Keyword 模式使用 KeywordId + KeywordParams
- [x] 1.2 在卡牌属性列表中说明 Effects 列表可包含两种模式的 Effect

## 2. 更新设计文档：机制原子清单

- [x] 2.1 在 `战斗/战斗_机制原子清单.txt` 中新增「Keyword Effect」章节，定义 Keyword 的设计身份：特殊的 Effect，硬编码执行，共享 Score/Tag
- [x] 2.2 将已有的「初始」「消耗」「弹药」从各自章节重新归类到 Keyword Effect 章节下
- [x] 2.3 补充新增 Keyword 的设计描述：武器(Weapon)、锁位(Anchored)、速攻(Rush)、坚守(Fortify)、迟缓(Sluggish)、超载(Overcharge)
- [x] 2.4 为每个 Keyword 标注介入时机类别（初始化/预处理/流程改写/后处理/被动拦截）

## 3. 更新设计文档：卡牌生命周期

- [x] 3.1 在 `战斗/战斗_卡牌生命周期.txt` 的生命周期流程图中，标注 Keyword 的介入节点位置
- [x] 3.2 补充武器(Weapon)关键词的预处理流程说明：CD到达后、TCA结算前的 Context 补全

## 4. 新增完整 Keyword 清单表

- [x] 4.1 在机制原子清单中创建 Keyword 汇总表（KeywordId / 中文名 / 参数 / 介入时机 / 行为描述），覆盖全部 9 个首批 Keyword
