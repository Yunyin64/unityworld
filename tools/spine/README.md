# 试战人形骨架

生成和接入 NPC 新素材、随机器验证和 Spine 装配见 [人形 NPC 美术与 Spine 动画规范](NPC_ART_AND_ANIMATION.md)。

男女骨架使用相同的 24 根骨骼和 23 个绘制插槽。近镜头的是角色左侧 `_L`，远侧持武器的是角色右侧 `_R`。编辑动作时左右名称跟角色走，战斗右侧演员由 Godot 镜像绘制。

```text
Root
└─ Hips
   ├─ Spine → Chest
   │  ├─ Neck → Head → hair
   │  ├─ Clavicle_L → UpperArm_L → Forearm_L → Hand_L
   │  ├─ Clavicle_R → UpperArm_R → Forearm_R → Hand_R → weapon
   │  └─ cape
   ├─ Thigh_L → Shin_L → Foot_L
   ├─ Thigh_R → Shin_R → Foot_R
   └─ cloth
```

旧 `root`、`hip`、`torso`、`head` 分别对应 `Root`、`Hips`、`Spine`、`Head`。旧 `armFront`、`elbowFront`、`handFront` 骨骼对应左大臂、左小臂和左手，`Back` 对应右侧。旧 `thighFront`、`shinFront` 和 `Back` 按同样方式对应左右。四根扩展骨骼保留原名。换装用的插槽 ID 继续保留旧名字，例如 `armFront` 和 `elbowFront` 分别挂到 `UpperArm_L` 和 `Forearm_L`。

## 复制到 Spine 4.2

在仓库根执行下列命令。`runtime` 是可用于 Godot 的副本，`Spine-Import` 里的两份 JSON 和 `parts/` 可以解压后独立使用。

```powershell
python tools/spine/build_duel_rigs.py --from-atlas assets/art/battle/spine --output .local-verification/spine/runtime --import-copy .local-verification/spine/Spine-Import
```

在 Spine 4.2 选择 **Import Data** 并打开其中一份 JSON。图片目录使用同目录下的 `parts`。导入后可另存为 `.spine` 工程。复制关键帧前先核对男女的关节位置和动作朝向。同名骨骼允许 Spine 对应动画轨道，3D 动作需要按二维视角重新取姿态。 [Spine 导入说明](https://esotericsoftware.com/spine-import)

## 绘制与装配

- 衣身画到肩口，并把领口和露出的脖颈画在同一张图上，由 `robe` 插槽显示。`Neck` 骨骼控制头部，图片交给衣身统一绘制。衣摆和披风各自成片。大臂以肩为基准，小臂以肘为基准，手以腕为基准。各部件画完整，接头要能被相邻片遮住。
- 远侧上下臂穿过衣身后面的层，靠镜头的上下臂与手在衣身前面。衣摆在腰部被躯干遮住。鞋随脚部骨骼转动，护腿随小腿骨骼转动。
- 插槽有默认绘制深度，数值越大越靠前。例如披风 `-100`、远侧大臂 `-42`、衣身 `0`、近侧大臂 `14`。各图可覆盖自己的深度，未填写就继承插槽。同深度按 Spine 的先后顺序。斩击可用 Spine 绘制顺序关键帧把持剑臂移到衣身前，素材的深度差仍然生效。
- `Appearance` 可分别填写 `armBack` 和 `elbowBack`。省略小臂时优先从相邻大臂找对应的内置衣装。显式填写的款式可混搭。既有 Mod 的衣身和鞋继续用原有插槽坐标。旧外观中的 `neck` 选项在装配时会跳过，颈部美术请包含在 `robe` 图片里。
- 外部素材在 `Data/Appearance` 的每条图片声明里可填整数 `Depth`。例如后发默认在 `-90`，一张需要被后发挡住的图可以填 `"Depth": -95`。常规换装保留默认值即可。用 Spine 的 Draw Order 维护工程内的遮挡，把 Mod 单图的覆盖值留在外观声明里。
- 重画的源图和制作提示词见 [分件原画](art/README.md)。图集按透明间隔收取独立组件，保留各部件画好的轮廓。运行时资源发布在 `assets/art/battle/spine/`。
