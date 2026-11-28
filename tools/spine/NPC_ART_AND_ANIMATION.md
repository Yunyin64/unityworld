# UnityWorld 人形 NPC 美术与 Spine 动画规范

## 交付规格

| 项目 | 项目约定 |
| --- | --- |
| 导入格式 | Spine 4.2 骨架 JSON、同目录 `parts/` 的拆件 PNG。已换装的 `NPC_XX_*.json` 打开后可从默认皮肤查看完整角色。 |
| 游戏资源 | `duel_male.json`、`duel_female.json`、`duel_parts.atlas`、`duel_parts.png`。游戏由 Godot 共用纹理和 Spine C# 运行时装配。 |
| 视角与标尺 | 面朝画面右侧的三分之四视角。`_L` 是镜头近侧，`_R` 是远侧。站立标尺为 234 骨架单位，地面为 `Y=0`，Spine 的 `Y` 向上。右侧战斗演员由客户端镜像。 |
| 素材粒度 | 每个可换插槽一张独立 RGBA PNG。给肩、肘、腕、膝、踝和颈口保留完整被遮区域。换装前后共用关节标记。 |
| 美术验收图 | 一张完整组装图、透明拆件、关节或接点位置、动作关键帧和随机换装复测图。 |

## 出图和装配

1. 用 `duel_male` 或 `duel_female` 的一套已装配外观做底图，锁定地面、人物朝向和像素密度。先画完整轮廓，再按现有插槽交付拆件。透明边缘预留约 2 像素干净边，保留所有接缝内侧。整理图集时逐片裁透明区，并同步换算裁切后的锚点。
2. 拆件图在原始像素坐标里提供关节标记，或用从左上到右下的归一化 `(u,v)`。装配尺寸用骨架单位。记录一个控制尺寸的值，按源图纵横比换算另一维。两个关节端点都要对齐的袖管、裤腿用两个点定位并确定旋转。艺术压扁或拉长时写明目的并在最大角度复看。
3. `robe` 一张包含领口与露出的脖颈。披风、衣摆、近远侧的大小臂袖、手、腿及鞋分别画。`hairFront` 画头顶和刘海，`hairRear` 单独覆盖后脑或发束，并在发束根部与前发叠住。发饰跟头，后发的甩动绕发根。脸图留完整肤色底，眼、眉、嘴和前发由各自插槽提供。饰品上属于其他槽的手指、旧武器或轮廓要在交付前清掉。
4. 持物 PNG 标两个点，握点和物体朝向点。剑尖、刀尖、枪头标在有效轮廓内。符佩以绳结或吊环为握点，朝向点放在穗端。握点贴合 `Hand_R` 子骨 `weapon` 的原点。以女骨架的替换玉剑为例，图内握点约 `(0.16, 0.51)`，剑尖约 `(0.97, 0.51)`，骨架宽度为 76 单位。转动时手覆盖握点，剑尖跟随动作。
5. 肘腕和膝踝要同时看设定姿势及最大弯曲。现有素材可在 234 单位标尺下从 6–12 单位的接缝重叠起步，按半透明边缘和遮挡实际调整。一种材质的同一件衣服共用光向和线宽，随机搭配保留轮廓可读性。

### 生成图片时的任务单

```text
目标：替换现有 female 骨架的 robe、armFront 和 elbowFront，保留骨架、动作和关节标记。
输入：完整待机参考 PNG、目标插槽的原始拆件 PNG、肩肘腕三个标记、动作极限帧。
画法：三分之四侧面，向右，统一光向。领口含脖颈，小臂袖画齐肘端和袖口的遮挡部分。
交付：一张组装预览、每槽独立透明 PNG、每图的尺寸和归一化关节点。输出原件同等像素密度。
验收：待机接缝、Slash 0.074/0.075/0.100 秒、Guard 高举、镜像、随机搭配。
```

## 层级和动作

| 从后向前 | 规则 |
| --- | --- |
| 远侧 | 披风、后发、远侧腿、远臂、武器、握手。护腿和鞋接缝由裤腿或绑腿上沿覆盖。 |
| 躯干 | 衣摆在腰带后，衣身在近侧袖与自由手后。待机时远臂在衣身后。 |
| 头部 | 后发、脸、眼眉嘴、前发、发饰。用头和发根控制部件运动。 |
| 攻击 | `Strike`、`Slash`、`Smash` 到 `0.075 s` 把持物臂移到衣身前。`FollowThrough` 的 `0.085 s` 恢复待机层级。抓关键帧前后各一张图，确认握手挡住剑柄，剑锋和脸部的遮挡清楚。 |

- Spine 的设定姿势、附件坐标和 Draw Order 是内置角色的装配依据。客户端根据战斗结算时间采样已有动作，负责镜像、缩放和舞台地线。视觉测试在 `0.100 s` 比较直刺命中帧，外部 Mod 的深度覆盖用 `Data/Appearance` 中的 `Depth` 表达。
- 分开判断预备、接触、重心转移和收势。先锁脚和手，再加衣摆、披风和头发的迟滞。检查循环第一帧、末帧和再起步的速度。Hero 的 `idle-from fall` 把双脚接触键集中在 `0.0333 s` 附近，躯干和头的收势延续到约 `0.53–0.57 s`。这个先软着陆再回稳的时序可用于检查本项目的 `Land`，各时间按本项目打击与镜头时钟调整。 [RigForge 落地参考](https://rigforge.app/assets/hero/idle-from%20fall) · [Spine 4.2 Hero 数据](https://github.com/EsotericSoftware/spine-runtimes/blob/4.2/examples/hero/export/hero-pro.json)
- `Kneel` 的收势以近脚承重、远膝落下、持物手垂下为准。前鞋 24 单位高，脚踝标在从鞋图顶部量下 40% 的位置，平放时脚踝约离地 14.4 单位。用同一地线看正向和镜像，避免两脚缩起的腾空轮廓。
- Spineboy 的脚固定、上身瞄准和 Celestial Circus 的头发、裙摆延迟可作为下一轮动画打磨的参考。目前运行时绘制的是独立的 Region 附件，新出的软布或长发先按拆件规格装配。带权重网格的工程应在运行时完成网格支持和验证后接入。 [Spineboy](https://esotericsoftware.com/spine-examples-spineboy) · [Celestial Circus](https://esotericsoftware.com/spine-examples-celestial-circus)

## 工具和交接

| 工具 | 本项目的使用位置 |
| --- | --- |
| Spine-SKILL | 定接触帧、保留关节重叠、放大复核穿插、对照编辑器与运行画面。 [手册](https://github.com/zxzAndyMAC/Spine-SKILL/blob/main/skills/spine-animation/SKILL.md) |
| Spine-Mcp | 本机已能读 Spine JSON，包括 `weapon` 的旋转轨、表情换图、Draw Order。修改 `.spine` 用授权版 CLI，在副本导入后再回读。 [使用说明](https://github.com/HinataYoki/Spine-Mcp/blob/main/docs/USAGE.md) |
| Genie Reskin Studio | 换色、重画局部、拆图集后回贴。试拆本项目图集时导出 149 个区域，可将拆件交给美术复修后重装。 [Reskin Studio](https://github.com/GenielabsOpenSource/spine-animation-ai/tree/main/reskin-app) |
| rydspine-mcp | 在副本里用 `bone_update`、`attachment_update`、`anim_set_keys` 修了女角直刺，留了 0.05/0.10/0.18 秒前后图。按它的 4.3 运行时测试，导入当前人形副本时将 `Root` 临时映射到 `root`。 [工具文档](https://github.com/ryderdinh/rydspine-mcp/blob/main/TOOLS.md) |
| RigForge 的 4.2 本地工具 | 用官方 `spine-core` 重读并绘制导入包，检查相同时间的附件、绘制顺序与坐标。复查部件接缝时使用固定视口。 [公开仓库](https://github.com/otey247/rigforge) |

内置外观使用相同的槽键装配各个皮肤。先看设定姿势、再换成其他同槽组件，最后用关键动作验证。帽子、前后发和持物应能单独换，完整衣装可提供跨槽的预设。 [Mix-and-match](https://esotericsoftware.com/spine-examples-mix-and-match)

## 复测和导入

在 UnityWorld 仓库根目录执行。`$godot` 使用当前项目的 Godot Mono 可执行文件。PNG 截图需要图形驱动。省略种子会抽新的一组，复测时用报告中的 `seed`。

```powershell
dotnet build Xianxia.Client.csproj
$review = Join-Path $PWD '.local-verification/spine/npc-outfits'
& $godot --path . --rendering-method gl_compatibility --audio-driver Dummy -- "--check-npc-appearance-visual=$review" --appearance-seed=1144477094
python tools/spine/build_duel_rigs.py --from-atlas assets/art/battle/spine `
  --output .local-verification/spine/npc-runtime `
  --import-copy .local-verification/spine/npc-spine-import `
  --appearance-report "$review/report.json"
```

1. 连续抽取 10 人，男女骨架各 5 人，失败样例保留在原位。检查每格实际 PNG、区域高度、脚线、层级、发根、肩肘腕膝踝、武器握点和镜像。报告保存每人的种子、外观配置、每帧的像素范围、贴图、深度和顶点。通过阈值以后逐帧看重点动作，其中 `0.074 s` 到 `0.075 s` 能比较持物臂换层的前后画面。
2. 将导入目录和 `parts/` 一并复制，在 Spine 4.2 用 **Import Data** 打开 `NPC_01_*.json`。确认 **Images** 路径为 `./parts/`。进 Animate 播放 `Ready`、`Strike`、`Slash`、`Smash` 和 `Land`。在授权版中另存 `.spine`，再导出同版本 JSON 和图集复测。 [Spine 导入指南](https://esotericsoftware.com/spine-import) · [版本兼容说明](https://esotericsoftware.com/spine-versioning)
3. 2026-11-28 的验收种子 `1144477094` 连续 10 人、420 帧通过。导入包用官方 4.2 运行时逐帧回读，附件和绘制顺序相同，顶点差小于 `0.001` 骨架单位。当前机器的 Spine Trial 未完成原生编辑器保存回环，交付的是 4.2 JSON 与独立素材。
