## 1. 创建 Modifier 系统设计文档

- [x] 1.1 在 `战斗/` 目录下新建 `战斗_Modifier系统.txt`，包含：ModifierDefine 体系总览、四大组成部分（A/B/C/D）的职责定义、三种宿主类型的划分标准与典型案例
- [x] 1.2 在文档中编写现有机制原子→Modifier 的映射表（护甲、易伤、虚弱、眩晕、XX强化、中毒的完整 Modifier 表达）

## 2. 更新现有战斗文档

- [x] 2.1 更新 `战斗/战斗_机制原子清单.txt`：在状态控制类和数值操控类中标注"底层由 Modifier 承载"，添加对 Modifier 系统文档的交叉引用
- [x] 2.2 更新 `战斗/战斗_TCA体系.txt`：在 Action 原子章节中补充 AddModifier / RemoveModifier / ModifyStacks 三个新 Action 的签名与说明
- [x] 2.3 更新 `战斗/战斗_核心机制.txt`：在 Tick 驱动模型章节中补充 Modifier 结算时序（Modifier 在卡牌 CD 推进之前结算）

## 3. 更新文件地图

- [x] 3.1 更新 `.codemaker/rules/File.mdc`：在战斗区新增 `战斗_Modifier系统.txt` 条目，更新行数和交叉引用关系
