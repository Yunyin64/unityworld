## 1. 数据模型

- [x] 1.1 EffectDefine.cs 新增 IsKeyword、KeywordParams 两个 JSON 属性
- [x] 1.2 EffectData.cs 新增 IsKeyword、KeywordParams 两个运行时字段

## 2. 构建管线

- [x] 2.1 CardMgr.BuildEffectFromDefine 增加 Keyword 分支（IsKeyword=true 时跳过 TCA 加载，拷贝 Keyword 字段）

## 3. 战斗运行时

- [x] 3.1 CombatCardState 新增 SetCdFull() 公开方法
- [x] 3.2 CombatScene.Start() 中增加 Keyword 初始化扫描逻辑（ApplyInitKeywords 方法）

## 4. JSON 数据拆分与 Keyword 数据

- [x] 4.1 将 Data/EffectDefines.json 拆分为 Data/Effect/Effect_Element.json、Data/Effect/Effect_Wound.json、Data/Effect/Effect_Keyword.json 三个文件
- [x] 4.2 EffectDefineMgr 加载逻辑适配：从加载单个文件改为加载 Data/Effect/ 文件夹下所有 JSON
- [x] 4.3 Data/Effect/Effect_Keyword.json 中新增 kw_initial 条目
