---
name: AppFacade Manager 全景
description: GameEntry→AppFacade→~100个Manager 的启动链路与分类职责概览
type: concept
---

# GameEntry → AppFacade → Manager 体系概览

## 启动链路

```
GameEntry.Awake()              → 创建持久化 GameObject
  └─ OnGameStartUp() 协程       → 初始化资源/Shader/SDK/输入系统
       └─ AppFacade.instance.StartUp()
            └─ StartUpCommand.Execute()
                 └─ AddAllManagers()  → 注册 ~100 个 Manager
                 └─ 各 Manager.Init()
```

`GameEntry` 是 Unity MonoBehaviour 场景入口，负责引擎层初始化（资源管线、Shader warmup、反作弊 SDK、崩溃监控、输入系统加载），最后调用 `AppFacade.StartUp()` 启动所有业务 Manager。

`AppFacade` 继承 `Facade`，是全局单例服务容器，通过 `AddManager<T>()` 注册 Manager，通过 `AppFacade.instance.xxxManager` 静态属性访问。

## 全部 Manager 分类一览

### 1. 核心/基础设施（12 个）

| Manager | 职责 |
|---------|------|
| **CoreManager** | 逻辑帧驱动（15FPS 固定帧）、渲染 Tick、Lua GC 回调、URP 管线钩子 |
| **GameManager** | 重量级场景/游戏生命周期：场景加载卸载、画质/环境/草水风GI/Shader Uniform 全局设置 |
| **LuaManager** | XLua VM 宿主：Lua 环境初始化、脚本加载、GC 管理、~200 个 C#→Lua 回调桥接 |
| **CoroutineManager** | 协程注册与清理，按实体/Owner ID 跟踪防泄漏 |
| **TimeManager** | 游戏时间缩放：多系统可 push/pop 时间倍率，计算最终有效倍速 |
| **AsyncRequestManager** | 异步资源请求节流（每帧最多 3 个），管理请求生命周期 |
| **MainThreadDispatcher** | 线程安全 Action 队列，后台线程向主线程调度工作 |
| **ScriptUpdateManager** | 按类型分组的 Update/LateUpdate 调度器，双缓冲安全增删 |
| **UpdateCheckManager** | 跟踪命名 Update 检查点，监控帧循环执行状态 |
| **GameSettingMgr** | 游戏配置键值对：加密数据加载 + 本地/服务器热补丁覆盖 |
| **InputManager** | 统一输入处理：Unity InputSystem 分发导航/镜头/角色拖拽等事件到 Lua |
| **NpcPushMgr** | NPC 近距推送：16×16 空间网格分区，追踪玩家附近 NPC 进入/停留/离开事件 |

### 2. UI 系统（5 个）

| Manager | 职责 |
|---------|------|
| **UIManager** | UI 窗口/面板生命周期、资源加载、UI 栈导航、SGUI 集成、输入路由 |
| **UISceneManager** | 3D UI 场景渲染到 RenderTexture：加载/卸载状态机、灯光切换 |
| **HUDManager** | 世界空间 HUD（血条、名牌等）：ID 字典管理、配置加载、投影相机 |
| **LHUDManager** | 旧版/兼容 HUD 管理器，挂渲染管线回调切换新旧渲染路径 |
| **UIEffectManager** | UI 挂点粒子/动画特效：生成、播放、按时长自动销毁 |

### 3. 渲染/视觉（22 个）

| Manager | 职责 |
|---------|------|
| **CameraManager** | 主相机静态访问器 + cameraJumpEvent 通知 |
| **CameraMgr** | 主相机控制器：跟随/旋转/缩放模式、相机穿模隐藏实体 |
| **VirtualCameraManager** | 虚拟相机优先级切换：主相机/全屏UI/过场等 |
| **DynamicResolutionMgr** | 相机高速移动时动态缩放渲染分辨率，适配 DLSS/FSR |
| **QualityMgr** | 画质中枢：设备检测(PC/iOS/Android)、DLSS/FSR/AFME/OFRC 等特性开关 |
| **PerformanceManager** | FPS 监控：滑动窗口帧时间采集，低帧回调 |
| **GameProfilerManager** | 运行时 Profiler：每帧统计渲染对象/特效/三角面/Batch 数 |
| **CProfilerHelper** | 调试后门：运行时桩函数注入 + SGUI 引导/教程覆盖参数 |
| **ShadowManager** | 阴影渲染：级联距离、Shadow Map 质量、URP 自定义 Shadow Pass |
| **CEffectMgr** | 核心特效引擎：FxGameObject 字典、特效池(线/通用/PCG/GPU粒子)、多线程并行更新 |
| **ScreenEffectManager** | 屏幕后处理 Shader：望气涟漪、扫描光、NPC 高亮 + 渐隐定时器 |
| **RenderObjectManager** | 渲染对象系统：GPU Job 距离剔除、HideByCamera/LookAtFace 命令 |
| **RenderObjectPoolMgr** | 渲染对象对象池：最大数量/120s 生命周期/定期销毁 |
| **LruRenderObjPoolMgr** | LRU 渲染对象池（代码中引用但源文件未定位） |
| **InstanceSortManager** | 按距主玩家距离排序渲染对象，处理透明/Overdraw |
| **TextureManager** | Texture2D 缓存(5MB 上限) + HTTP 远程纹理 + 内容审核状态 |
| **StreamingTextureArrayManager** | 流式纹理数组管理（源文件在外部程序集中） |
| **HLODManager** | 分层 LOD：场景块 LOD0 更新 + 父块剔除（新旧双路径） |
| **EntityLODManager** | 角色/实体 LOD 切换与流式加载，按类型配置(玩家/NPC/宠物等) |
| **WatermarkManager** | 非发布版水印：300×150 账号名纹理 |
| **NativeMaterialPropertyBlockAnimationManager** | GPU 侧 Shader 属性动画：C# Jobs + NativeCollections 批量更新材质 |
| **SceneDifferenceConfigMgr** | 按场景差异化配置：从 ScriptableObject 加载场景特定渲染/行为覆盖 |

### 4. 场景/环境（14 个）

| Manager | 职责 |
|---------|------|
| **StreamManager** | 场景流式加载：地形/光照图/草/体积GI 流式 LOD，多帧加载 |
| **EnvManager** | 初始化外部 EnvSDK（内容审核/环境 SDK） |
| **EnvSettingRuntimeMgr** | 运行时环境系统：日夜循环/天气/光照/体积雾/云海/过场环境插值（外部程序集） |
| **TerrainMgr** | 地形系统：Clipmap 虚拟纹理、碰撞体数据流式、光照图预加载 |
| **WaterSystemMgr** | 水体渲染：湿度/高度图/平面反射/SSR/水下雾/涟漪/烘焙水影 |
| **ProceduralGrassManager** | GPU 程序化草地：按场景加载草资产（设置/着色/四叉树/材质） |
| **ChemistryMgr** | 网格化化学反应系统：元素交互/材质属性/GPU 纹理 + Jobs/Burst |
| **MoonLightAreaManager** | 月光区域特效：月相动画、月光物体/遮罩纹理沿路径移动 |
| **ChameleonManager** | GPU 变色龙渲染：URP Render Pass + 双缓冲数组(≤64) |
| **FluidSimulationMgr** | GPU 流体模拟(Navier-Stokes)：平流/力/散度/压力求解/边界 |
| **TwoDLevelManager** | 2D 横版关卡：静态/动态 Prefab、虚拟相机层级、重力、动画回调 |
| **UgcEnvMgr** | UGC 场景环境设置：时刻/天气/特殊天气转换+渐入 |
| **LoopMoveManager** | 循环/平铺世界滚动：速度缩放/隐藏标记/移动开关 |
| **GenericDecalMgr** | 通用贴花加载管理，场景卸载时销毁 |

### 5. 角色/外观（13 个）

| Manager | 职责 |
|---------|------|
| **AvatarMgr** | 角色渲染对象：创建/销毁/LOD/动画/碰撞回调 |
| **CCharacterRenderObjectMgr** | 角色渲染对象核心：创建/销毁/LOD 过渡/碰撞/动画控制 |
| **CharModAssetManager** | 角色模型组件资产加载：时装/发型/面容/饰品资源生命周期 |
| **FaceEditMgr** | 角色捏脸 UI：预览相机/Shader 特效/资产缓存 |
| **FashionMgr** | 时装/装备数据：穿脱/DIY/Schema 缓存/BDD 命令执行 |
| **FashionAbilityMgr** | 时装技能运行时激活：跟踪哪些 RO 有活跃技能，分派任务执行 |
| **FacialAnimationManager** | 面部动画队列（口型/表情）：池化 + 优先级播放 |
| **DynamicBoneReferenceManager** | DynamicBone 引用追踪：按角色驱动 LateUpdate 布料物理 |
| **DynamicLargeRibbonManager** | GPU/Job 大飘带/长发物理：NativeArrays + C# Jobs |
| **HitDetectionMgr** | 每帧打击检测：Tick 活跃 RO + 批量并行查询调度 |
| **CRVOMgr** | RVO 碰撞避让：NPC 和玩家间的连续互斥速度障碍 |
| **BlastObjectMgr** | NVBlast 破坏物体：创建/销毁回调 + Lua 通知 |
| **PhysicsForceMgr** | 物理力场列表：每帧更新/过期力效果 |

### 6. 音频/视频（8 个）

| Manager | 职责 |
|---------|------|
| **AudioManager** | FMOD 音频中枢：BGM/SFX/语音/环境音、音质设置、分类路由 |
| **AudioAreaManager** | 空间音区触发器：按区域分层环境音播放 |
| **VoiceManager** | 语音消息上传/下载：录制/AMR-WB 编码/缓存/播放 |
| **CCVoiceManager** | 网易云信实时语音：流式音频捕获/变声/Buffer Hook/PB 协议 |
| **StreamingAudioPlayerMgr** | TTS 流式音频：并发队列缓冲 + AudioSource 播放 + 唇同步进度 |
| **SceneVideoManager** | 场景内视频播放：创建/销毁播放器实例(Unity原生/L36后端) |
| **VideoDonwloadManager** | 视频下载：任务队列(≤3并发)、本地缓存、引用计数回调 |
| **WebResManager** | 远程 Web 资源获取：音频/脸型等 + 多语言支持 |

### 7. 网络/SDK/平台（7 个）

| Manager | 职责 |
|---------|------|
| **HttpManager** | HTTP 请求池：GET/POST/纹理下载队列 + SSL 证书验证 |
| **WebManager** | 内嵌 WebView：URL/尺寸/关闭按钮/旋转/JS Bridge |
| **UniSdkManager** | 网易 UniSDK：平台初始化/登录/支付/分析/推送 |
| **NotificationPushManager** | 移动端推送：定时本地通知(iOS)、周计划、活动推送规则 |
| **AdaptiveServiceManager** | 自适应性能：MAGT(联发科) + Pixelworks SDK，监控温度/GPU 动态调画质 |
| **FRCMgr** | 帧率转换(Pixelworks)：40→120FPS 插帧，支持设备开/关 |
| **AndroidAIDLManager** | Android AIDL 服务桥：与原生后台服务 IPC |

### 8. 玩法/场景交互（14 个）

| Manager | 职责 |
|---------|------|
| **CutsceneManager** | 过场动画/Playable：视频预加载/DOF/玩家 Transform 管理 |
| **FreeWalkManager** | 自由漫游/调试模式：无战斗约束启动核心系统 |
| **SceneObjectManager** | 场景物体：可见性/动画状态/加载完成回调 |
| **OpenDoorManager** | 开门交互：追踪已开门对象及旋转状态 |
| **CutTreeManager** | 砍树玩法：已砍/燃烧/旋转状态 + 异步替换模型 |
| **CLegoBuildingMgr** | 乐高建筑管理（源文件未定位） |
| **LegoComponentMgr** | 乐高组件特效/灯光/呼吸灯：距离+屏幕数量剔除 |
| **CGardenFarmMgr** | 农场玩法：土壤/作物渲染对象，按网格坐标追踪 |
| **LegoPaintMgr** | 乐高涂色：射线检测 + Shader 材质属性涂色 |
| **BatchPrefabManager** | 批量实例化 Prefab：GPU Instancing/合并渲染 |
| **AutoChessManager** | 自走棋环境：加载/应用棋盘场景设置 |
| **SLGManager** | SLG 策略玩法：六角格对象池 |
| **LiteWorldViewManager** | 轻世界/沙盒视图：世界创建/对象池/ID 生成 |
| **PhotoStudioManager** | 照相馆模式：资产节点/相机/场景配置 |

### 9. 其他工具类（5 个）

| Manager | 职责 |
|---------|------|
| **DebugSocketManager** | 本地 TCP 调试服务(39393端口)：远程调试/GM 命令（仅 Editor/Dev） |
| **DaShenMsgMgr** | 大神云游戏消息：队列处理云游戏客户端消息 |
| **Lp6LhyhMgr** | 映画(LP6)引擎桥接：资产/协议系统 + Lua 查询函数 |
| **LocationBasedMgr** | GPS 位置服务：地理围栏检测(如中国多边形) |
| **ScreenshotMgr** | 截图：NativeArray 编码/相册保存/可选上传 |
| **FuXiAOPManager** | 伏羲 AOP AI 运营平台：流式 TTS/语音/文本服务 Agent + Protobuf |

## 架构要点

- **生命周期**：所有 Manager 实现 `IManager`，继承 `BaseManager`，按 `Init() → EarlyUpdate/CommonUpdate/LateUpdate → Destroy()` 运行
- **Lua 暴露**：大部分 Manager 通过 `[ExportToLua]` 暴露给 Lua，Lua 端用 `CS.Pangu.AppFacade.xxxManager` 访问
- **注册顺序**：`AddAllManagers()` 的顺序决定初始化和 Update 调用顺序，有依赖关系（如 LuaManager 优先于 UIManager）
- **部分 Manager 在外部程序集**：EnvSettingRuntimeMgr、StreamingTextureArrayManager 等定义在编译好的 DLL 中，源码不在 Scripts 目录下

## 源文件未定位的 Manager

- **LruRenderObjPoolMgr** — 代码中引用但源文件未在 Assets/Scripts 下找到
- **CLegoBuildingMgr** — 同上
- **StreamingTextureArrayManager** — 定义在外部程序集中
