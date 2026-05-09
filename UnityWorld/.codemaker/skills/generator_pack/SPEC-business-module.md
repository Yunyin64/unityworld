# 业务模块生成规范（Business Module Spec）

> 本规范补充 SPEC-write.md，描述业务模块（`business/` 目录）的生成规则。
> 业务模块对应玩家直接感知的玩法功能，有明确的 UI 入口和业务闭环。

## 1. 业务模块定义

一个业务模块对应**一个可独立运营的游戏功能**（如"EVA 联动"、"玩家召回"、"时装返场投票"），以 UI 界面为入口，包含两个维度：

**纵向**——实现链路，从 UI 到存储：

```
VVM 视图（UI 入口）
    ↓ m_CreateArgs / 事件监听
GAC 客户端逻辑（Mgr / 辅助类）
    ↓ Gac2Gas RPC
GAS 服务端逻辑（Handler / 校验）
    ↓ DB 操作
数据存储（MySQL / Redis / KVMgr）
```

**横向**——业务辐射面，该业务涉及的各个功能域（任务、道具、红点、成就等）。
纵向链路回答"请求怎么流转"，横向辐射面回答"这个业务还碰了哪些东西"。

## 2. 命名规则

```
<business_name>.module
<business_name>.<flow_name>.flow
<business_name>.<slice_name>.slice
```

- `business_name`：语义化的业务名（如 `eva`、`player_recall`、`fashion_encore`）
- 禁止使用编号（如 `312`）作 business_name，必须用语义名

示例：
- `activity_ip_collaboration.eva.module`
- `activity_ip_collaboration.eva.activation.flow`
- `activity_ip_collaboration.eva.festival_state.slice`

## 3. 通用 vs 专属分离原则（强制）

业务模块文档必须区分"通用流程"和"本业务专属内容"：

| 类别 | 定义 | 写法 | 示例 |
|------|------|------|------|
| 通用流程 | 多个业务共用的接口链路（如 FestivalTask 领奖、Achieve 领奖、SignIn 领奖） | 不展开链路细节，用**搜索关键字**指向已有知识库文档 | "Festival 任务领奖 → 搜索 `RequestGetFestivalTaskReward`" |
| 本业务专属 | 本业务特有的参数、ID、视图类、红点、配置项、UI 交互 | 完整文档化 | "festivalId=366, fTaskId=1/2" |

**判断标准**：如果一个链路步骤换一个活动编号后仍然成立，那就是通用流程，不应在业务模块中展开。

**执行步骤**：

1. 在写全链路接口清单、Flow、Slice 之前，**必须先检查知识库**中是否已有该通用流程的文档
2. 检查方法：用 `kb_index_query.py` 搜索相关关键字（如 `FestivalTask`、`SignIn`、`Achieve`）
3. 如果已有 → 在业务模块中用搜索关键字指向，不展开细节
4. 如果没有 → 按 SPEC-write.md §3.1 追加到 `.context/TODO-knowledge-gaps.md`，文档本身只写引用，不标注 TODO 状态

**通用流程引用格式**（在业务模块中使用）：

```markdown
### 链路 X：Festival 任务领奖

- 本业务触发点：`OnLinkageRewardItemLuaClick` / `OnGenVideoRewardItemLuaClick`
- 本业务专属参数：festivalId=366, fTaskId=1/2
- 通用流程：搜索关键字 `RequestGetFestivalTaskReward`
```

## 4. 生成前的追踪步骤（强制）

生成业务模块前，**必须**沿以下路径逐层追踪，每一层都需有代码定位锚点：

### 4.1 VVM 视图层（入口）

1. 定位主视图类（继承 CViewBase）
2. 找到 `m_CreateArgs` 中的 `FPageId` / `FId` —— 这是关联 Festival 系统的键
3. 找到所有 `MsgHub_*` 事件监听 —— 这是服务端推送的入口
4. 找到所有 `Gac2Gas:*` 调用 —— 这是客户端→服务端的 RPC
5. 找到所有 `g_*Mgr` 引用 —— 这是客户端逻辑管理器

### 4.2 GAC 客户端逻辑层

1. 定位 `g_*Mgr` 对应的 Mgr 类（通常在 `gac/lua/<domain>/` 下）
2. 找到 Mgr 中处理 RPC 回调的方法（`Gas2Gac_*` handler）
3. 找到 Mgr 中的状态管理（读/写哪些数据）
4. 找到 Mgr 中调用的 `Gac2Gas` RPC
5. 找到 Mgr 依赖的配置表（`FestivalGame_*`、自定义表等）

### 4.3 GAS 服务端逻辑层

1. 在 `gas/lua/<domain>/` 下定位 RPC handler（`Gac2Gas_*` 对应的方法）
2. 找到校验逻辑（参数检查、权限检查、状态检查）
3. 找到服务端状态变更逻辑
4. 找到 `Gas2Gac` 推送（通知客户端状态变更）
5. 找到 DB 操作（读/写/更新）

### 4.4 数据存储层

1. 定位 DB 表和 prepared statement（`server_common/lua/dbstatements/`）
2. 定位 Redis/缓存 key 模式
3. 定位 KVMgr 同步字段（如 `g_AutoSyncKVMgr:GetValue("Festival")`）
4. 若涉及持久化存储或独立缓存，**必须**创建 `<name>.persistence.slice`（详见 SPEC-write.md §10.8）

### 4.5 业务辐射面扫描（横向，强制）

纵向追踪完成后，**必须**扫描该业务涉及的所有横向功能域。

扫描方法：从 VVM 视图和 GAC Mgr 出发，搜索以下域的关键字，逐一确认是否涉及：

| 域 | 扫描关键字/模式 | 典型涉及点 |
|----|---------------|----------|
| 任务 | `TaskMgr`、`FestivalTask`、`FastTask`、`TrackTarget` | 业务专属任务、任务追踪、任务完成回调 |
| 道具/物品 | `ItemMgr`、`ItemSlot`、`CItemDisplay`、`Gac2Gas:UseItem` | 业务奖励物品、消耗道具、物品展示 |
| 货币/消耗 | `Currency`、`YuanBao`、`YinPiao`、`ConsumeSimple`、`Money` | 业务所需货币、消耗确认、价格 |
| 商店/充值 | `Shop`、`Recharge`、`GiftPack`、`FestivalGame_LimitAccRecharge` | 业务关联商城、充值礼包 |
| 红点 | `RedDot`、`RefreshRedDot`、`FestivalActive` | 业务红点触发/刷新逻辑 |
| 签到 | `SignIn`、`FestivalGame_SignIn`、`CommonSigninContainer` | 业务签到页、签到数据 |
| 成就 | `Achieve`、`Achievement`、`GetAchieveReward` | 业务关联成就/成就奖励 |
| 排行 | `Rank`、`Ranklist`、`RankMgr` | 业务排行榜 |
| 抽奖/扭蛋 | `ZhuanPan`、`Gacha`、`Lottery`、`Draw` | 业务抽奖/扭蛋玩法 |
| 剧情演出 | `CutScene`、`Movie`、`Dialogue`、`ScenarioDialogue` | 业务专属剧情/过场 |
| NPC | `NPCSystem`、`NpcScript`、`Spine`、`Live2D` | 业务 NPC 交互/动画 |
| 社交 | `Chat`、`Share`、`Friend`、`Team` | 业务社交分享/组队 |
| 时装/外观 | `Fashion`、`GuanCang`、`OOTD` | 业务关联时装/馆藏 |
| 引导 | `Guide`、`Tutorial`、`NewPlayerGuide` | 业务新手引导 |
| 特效/表现 | `Effect`、`FxGroup`、`Timeline`、`Animation` | 业务专属 UI 特效/动画 |
| 配置表 | `FestivalGame_*`、业务专属表 | 业务所有配置表清单 |

扫描结果以**业务辐射面清单**的形式写入 Module 文档（见 5.1 节）。

**只列出涉及的域**。不涉及的域无需列出——省略即表示"已扫描，不涉及"。

辐射面清单中每个涉及的域，**必须给出具体的配置表键或代码定位点**（函数名/红点ID/配置表行号等），而非仅写搜索关键字。

辐射面清单中的**配置表**引用，使用"表名+键"格式（如 `FestivalGame_FestivalTask[314]`），不写文件路径。若配置经过扩展/运行时转换，需标注转换路径。定位方法详见 `generator_pack/GUIDE-design-data-location.md`。

**反向更新规范（强制）**：在扫描过程中，若发现本业务涉及的域不在上述扫描表中，**必须**将该域追加到本规范的扫描表中（新增一行：域名 + 扫描关键字 + 典型涉及点），确保规范随实际业务演进。

## 5. 产出物要求

每个业务模块**至少**产出以下文档，且每个生成的知识文档标题后必须写 `> summary: <≤150字简介>`：

| 文档类型 | 数量 | 要求 |
|---------|------|------|
| Module | 1 | `<name>.module` — 业务边界、对外接口、配置路径 |
| Flow | ≥1 | 至少覆盖一个核心交互流（如"签到领取"、"兑换商品"） |
| Slice | ≥1 | 至少覆盖一个关键状态结构（如"Festival 状态"、"签到进度"） |

### 5.1 Module 文档要求

业务模块的 Module 文档必须包含：

- **UI 入口定位**：哪个 VVM 视图、哪个面板名、如何打开
- **全链路接口清单**：VVM 触发点 + 本业务专属参数 + 通用流程引用（按 §3 "通用 vs 专属分离原则"执行）。格式：
  ```markdown
  ### 链路 X：Festival 任务领奖
  - 本业务触发点：`OnXxxClick`（文件 + 搜索关键字）
  - 本业务专属参数：festivalId=366, fTaskId=1/2
  - 通用流程：搜索关键字 `RequestGetFestivalTaskReward`
  ```
  **禁止在业务模块中展开通用流程的服务端链路细节**（GAS handler、校验、DB 操作等）。这些属于通用流程文档的职责。
- **业务辐射面清单**：按 §4.5 扫描结果，只列出涉及的域，每域必须给出具体的配置表键或代码定位点
- **配置路径汇总**：策划表用"表名+Sheet名+列名"格式，定位方法详见 `generator_pack/GUIDE-design-data-location.md`

### 5.2 Flow 文档要求

业务 Flow 应聚焦**本业务专属的触发逻辑和参数**，通用流程用搜索关键字引用：

```markdown
## 主链路

### 1. 视频任务领奖
1. 用户点击任务奖励按钮 → `OnLinkageRewardItemLuaClick`
   - 代码定位：`program/game/gac/lua/vvm/ActivityIPEVA/Activity312EvaActivationVVM.lua`
   - 搜索关键字：`OnLinkageRewardItemLuaClick`
2. 本业务专属参数：festivalId=366, fTaskId=1
3. 通用领奖流程：搜索关键字 `RequestGetFestivalTaskReward`
4. 领奖成功回调 → VVM 刷新按钮状态
   - 代码定位：同上 VVM 文件
   - 搜索关键字：`MsgHub_PlayerFestivalTaskStateChanged`
```

**原则**：
- VVM 层的触发和回调是本业务专属的，必须展开
- GAC→GAS→DB 的通用链路，用一行搜索关键字引用，不展开
- 如果通用流程在知识库中尚不存在，确保已按 SPEC-write.md §3.1 追加至 `.context/TODO-knowledge-gaps.md`

### 5.3 Slice 文档要求

业务 Slice 只记录**本业务专属的状态字段**。通用状态结构（如 FestivalData、SignInData、AchieveData）由通用流程文档负责，业务 Slice 用搜索关键字引用：

```markdown
### 字段 1：Festival 任务奖励状态
- 本业务专属参数：festivalId=366, fTaskId=1/2
- 通用状态结构：搜索关键字 `FestivalData`（character_playdata blob）
- VVM 读取：`Activity312EvaActivationVVM.lua` → `GetFestivalTaskState`
```

**禁止**在业务 Slice 中展开通用状态结构的完整写入/读取点表格。

## 6. 生成顺序（强制）

1. **先追踪，后写文档** — 沿 §4.1~4.4 路径完整追踪一遍
2. **先查重，后引用** — 识别通用流程，查知识库是否已有，已有则引用，没有则追加到 TODO-knowledge-gaps.md
3. **先 Module，后 Flow/Slice** — Module 定边界，Flow/Slice 填细节
4. **先核心流，后边缘流** — 主交互流优先，异常/降级流后补

## 7. 判定何时拆分业务模块

以下情况**必须**拆分为独立业务模块：
- 该业务有独立的 RPC 链路（Gac2Gas / Gas2Gac）
- 该业务有独立的服务端状态管理
- 该业务有独立的配置表或配置分区
- 该业务可独立开关（通过 FestivalId 或 Feature gate）

以下情况**不需要**拆分：
- 仅是 UI 布局变体（如 Gacha 的 5 套模板），无独立业务逻辑
- 仅是同一业务的版本迭代（如 RebateJar v1/v2），共享数据结构

## 8. 自检清单

业务模块完成后，必须能回答：

- [ ] 玩家点击 UI 后，请求如何到达服务端？（通用流程用搜索关键字引用即可）
- [ ] 服务端校验失败时，客户端如何收到通知？（通用流程用搜索关键字引用即可）
- [ ] 该业务的状态字段在哪些文件中被写入、哪些文件中被读取？（仅列本业务专属的，通用的引用）
- [ ] 新增一个同类活动时，需要改哪些配置表、注册哪些面板？
- [ ] 该业务相关的关键日志/错误模式是什么？
- [ ] 业务辐射面清单是否只列涉及域？每个涉及域是否给出了具体的配置表键或代码定位点？
- [ ] 扫描中是否发现规范扫描表未覆盖的新域？如有，是否已反向更新规范？
- [ ] **通用 vs 专属分离**：文档中是否展开了不属于本业务的通用链路细节？如有，改为搜索关键字引用
- [ ] **缺失内容追踪**：引用的通用流程在知识库中是否已存在？不存在的是否已按 SPEC-write.md §3.1 追加至 `.context/TODO-knowledge-gaps.md`？
