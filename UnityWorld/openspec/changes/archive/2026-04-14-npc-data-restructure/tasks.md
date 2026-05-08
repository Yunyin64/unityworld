## 1. 数据文件拆分与新建

- [ ] 1.1 新建 `Npc/Data/NpcBioData.cs`：定义精简后的 NpcBioData 类（Name, Gender, NpcType, AgeAccumulated, BirthTick, BaseMoveSpeed, AppearanceId, IsAlive, DeathTick），含 XML 注释和 TODO 标注
- [ ] 1.2 新建 `Npc/Data/AppearanceData.cs`：定义 AppearanceData TODO 空类
- [ ] 1.3 新建 `Npc/Data/NpcCultivationData.cs`：迁移并扩充 NpcCultivationData（含 GongFa），新增 BaseProperty struct、ElementalAffinity struct、LifespanMax、SpiritRoot、IsInCultivation、HpMax/MpMax/SpMax、Get 便捷方法
- [ ] 1.4 新建 `Npc/Data/NpcFactionData.cs`：定义 NpcFactionData TODO 空类（含注释说明未来字段方向）

## 2. 清理原文件

- [ ] 2.1 清空 `Systems/NpcSystemBio.cs` 中的 NpcBioData 类定义（只保留 NpcSystemBio 类）
- [ ] 2.2 清空 `GamePlay/Practice/NpcCultivationData.cs` 的内容，替换为重定向注释（不删除文件）

## 3. 重构 NpcSystemBio

- [ ] 3.1 更新 NpcSystemBio：适配新的 NpcBioData 字段，移除 LifespanMax/TimeFlowRate/CultivationLevel 相关方法，简化 OnTick（仅推进年龄），保留 Register/GetBio/GetAge/GetMoveSpeed

## 4. 完善 NpcSystemPractice

- [ ] 4.1 实现 NpcSystemPractice：添加 `_cultivationTable` 字典，实现 Register/GetCultivation 方法
- [ ] 4.2 实现寿元判定方法：IsLifespanExhausted（读取 BioData.AgeAccumulated 对比 LifespanMax）、GetLifespanMax、GetLifespanRatio
- [ ] 4.3 实现八大基础属性的 Get 便捷方法：GetQiXue, GetTiPo, GetQiGan, GetLingJi, GetShenShi, GetWuXing, GetJiYuan, GetMeiLi
- [ ] 4.4 实现 OnTick 骨架（寿元耗尽判定等，标注 TODO 未来修炼逻辑）

## 5. 更新 NpcSystemFaction

- [ ] 5.1 更新 NpcSystemFaction：添加 TODO 注释说明未来职责

## 6. 联动更新 Npc 实体

- [ ] 6.1 更新 `Npc.cs`：新增 `Cultivation` 属性访问器（通过 NpcMgr.PracticeSystem），更新已有属性访问器

## 7. 联动更新 NpcMgr

- [ ] 7.1 在 NpcMgr 中注册 PracticeSystem 和 FactionSystem 子系统
- [ ] 7.2 更新 NpcMgr.Create()：适配新的 BioData 字段（去掉 LifespanMax/TimeFlowRate/CultivationLevel），新增注册 PracticeSystem 的 NpcCultivationData
- [ ] 7.3 更新 NpcMgr.RandomCreate()：同上适配
- [ ] 7.4 更新 NpcMgr.Tick()：加入 PracticeSystem.OnTick 调用
- [ ] 7.5 更新 NpcMgr 日志方法（NpcAllInfoLog, PrintFullInfo）：从 PracticeSystem 读取修行数据，从 BioData 读取凡间数据

## 8. 联动更新 NpcGenerator

- [ ] 8.1 更新 NpcGenerator.GenerateCultivators()：适配新的 RandomCreate 签名和数据结构