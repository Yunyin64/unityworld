# Godot 美术资源

- `art/backgrounds/`：完整场景背景，不含 UI 或角色。
- `art/characters/`：带透明通道的示例人物立绘（静态单帧）。
- `art/battle/`：正式战斗的月门论剑台、李铁柱和苏灵儿的动作图集及结印素材。战斗场景通过 `AtlasTexture` 选择各姿态和头像区域。
- `art/ui/`：储物背包、战斗记录、人物属性面板底图；不含文字和数值。
- `art/icons/artifacts/`：四张带透明通道的法宝图标。
- `audio/battle/`：本项目合成的战斗短音效，使用带限噪声、短扫频和衰减共振。场景资源可以按槽位替换这些音效。

所有贴图通过 `res://assets/...` 引用，面板可左右复用。不要把图片或客户端资源放入 `UnityWorld/`。

背包格子、物品占格底色、文字、属性条都是测试场景中的独立 Godot 控件，不烘焙在贴图中。UI 底图为整张纹理，当前按测试尺寸使用；不是经过手工切片的九宫格皮肤。

旧版静态示例及提示词见 `UnityWorld/Docs/art/`。战斗舞台用内置 ImageGen 生成，提示词要点如下。

- 场地采用月夜高山石台、青铜月门、远峰和雾。留空前景供人物交手，画面两侧保持较暗。
- 李铁柱用青灰披风、白衣和碧剑，苏灵儿用朱衣、紫黑马尾和铜符。采用约三头身、粗黑边与硬边赛璐璐阴影，四格分别排列待机、攻击、防御和受击。
- 结印和脚步图沿用两人的造型。剑修以空手捏诀，灵修持符抬掌。脚步分前压和面向对手后撤，衣摆根据移动方向倾斜。角色图集采用透明背景。
- 招式图仍以两人的原有动作图为参考，保持人物比例、配色和武器。透明四格按左上、右上、左下、右下排列横斩、重击、射击、闪身。李铁柱向右，横挥碧剑、举剑劈砍、并指放剑气、带剑低身撤步。苏灵儿向左，挥符刃、过顶重劈、抬掌发符、俯身避招。每格显示完整身体，给衣摆和武器留裁切间距。
- 败退图使用内置 ImageGen，以两人的原动作图为造型参考。透明双格里，左侧李铁柱向右单膝撑剑，右侧苏灵儿向左单膝撑地、持符垂腕。保留原头身比、粗边线、硬阴影和服色。每格显示完整身体和武器，两格留透明间距，粒子和光效由 Godot 绘制。
- 全景图以项目原论剑台为参照，拉远月门并露出完整石台。蓄劲与收势图沿用两名角色的服饰和兵器。两张新图用内置 ImageGen 生成。
- `duel_landing_poses.png` 以两人的原动作图为参照，由内置 ImageGen 生成。上排李铁柱向右，下排苏灵儿向左。左列是挨打后压低重心、后脚撑住地面的姿态，右列是闪回后屈膝落地的姿态。衣服、发色、兵器和粗边线沿用原造型，图里只有透明背景、人物与手持兵器，遁光和碎尘由舞台绘制。
- 论剑台在 `observer_arena.tscn` 里分为舞台、山脉、天空。舞台读取 `moon_gate_arena_wide.png` 的原画颜色，用 ImageGen 提取的 `moon_gate_stage_matte.png` 保留建筑及石台，露出门洞和栏杆间隙。
- `moon_gate_mountain_panorama.png` 为 4344×724，左半是用户给的 2172×724 山图。右半由内置 ImageGen 续画，两侧各用 80 像素过渡到原图边缘。1920 画宽下每秒向左滚动 9 像素。
- `moon_gate_sky_plate.png` 是从原画分离的天空底图。暂用 `moon_gate_original_cloud_matte.png` 控制云纹活动范围，按 20 秒周期往返。天空动画素材可独立替换。

<details>
<summary>论剑台分层与山脉扩图提示词</summary>

**舞台抠图**

Use the original attached ORIGINAL game art as an edit target. Produce the exact registration 16:9 ALPHA CUTOUT for this scene's stationary architectural foreground, same whole canvas, no recrop and no position or scale change. KEEP every pixel of the full stone terrace floor with round engraving, balustrade railing posts and rails, the two glowing lanterns, the two border columns and their hanging ornate beams, the oval jade moon gate ring and its attached stone at the foot, the little open pavilion and the rock base physically attached to it. Keep all silhouette geometry and every hole, including the oval moon gate opening, open space between pavilion pillars, and gaps between railings. REMOVE to TRUE TRANSPARENT ALPHA: ALL blue sky, moon, fog and all unrelated mountain range, including the landscape inside the ring and through the little pavilion and railing holes. Select architecture by actual edge, down to carved tips and rail gaps. All clear apertures including the center of the moon gate must be 100% alpha zero, no colored residue, dark silhouette, blue halo or baked backdrop. Original dark clean cut edges, same pixels and placement wherever kept. The pavement from y=62% down should be completely opaque. Alpha for everything other than architecture and terrace. No added content or arbitrary changes. Produce an exact-size registered alpha matte as PNG RGBA, with the whole original frame retained.

**天空底板**

Edit the uploaded original Moon Gate battle arena painting into an exactly registered sky backplate only. STRICT: full same 16:9 canvas and pixel-registered upper sky, keep the original moon at its exact original center and size x=76%, y=28% including the thin dark cloud across it. Keep all of the ORIGINAL dark navy night sky and irregular wispy indigo brushwork in the top 35% where there is sky, same positions, brightness, and quiet feathered depths. Remove the two side columns and top ornaments and fill their vacated space with matching dark night sky. Remove every mountain, all architecture, moon gate, foreground floor and horizon structures. Seamlessly extend only the existing night sky and low-contrast blue atmospheric haze to the lower half as a scene background plate. Entire image opaque, no hills, horizon peaks, sharp shapes in the lower half, no terrace or UI. Match old sky near-exactly, especially dark open space, muted feathered clouds not new bands. Preserve the precise original moon shape, size, light and position. Original painter textures, 16:9.

**背景山脉续画**

Generate the adjacent continuation panel to the wide transparent mountain plate (second image), extending that painting horizontally into the same world. Use the first full scene solely as the brushwork and palette reference. Match the second image exactly in width-to-height ratio 3:1, painted scale, upper edge altitude, cold dark blue levels and alpha coverage: irregular silhouettes begin around y=18–32%, dark craggy midground and soft layered blue valley fog fill down to and across the bottom. The two endpoints of this new image should join to the ends of the second picture in either order: at each edge jagged mountain silhouette meets at y≈28%, with comparable ridge color and fog intensity and no distinctive huge peak positioned touching the edges. Between them add more of the same distinctive jagged sharp alpine ridges and small rocky peaks with gentle blue mist, related to the original landmarks. Painterly small repeated mountains with perspective; retain separation and rocky details across full width. True alpha transparent above the jagged mountain outlines. Entire background plate landscape only, no night sky, buildings, people, railings, moon, text, terrace, game UI.

</details>

<details>
<summary>老图云层提取提示词</summary>

Create an ALPHA CUTOUT of ONLY THE EXISTING CLOUDS from this exact original landscape. This output will be a pixel-registered cloud animation matte, not a redesigned scene. Output the SAME complete 16:9 canvas including its lower empty area, with cloud placement and scale registered to the source. Separate the three broad softly painted cloud clusters in the upper dark blue area: the broken overhead cloud to the left and occasional wisps to the right, and the large dark-blue brush clouds centered around quarter-width and quarter-height. Keep these clouds where they are, including dark brushwork. Keep softly feathered transparency around the individual cloud banks, with a little softness around their edges, avoiding any hard outlines or any new cloud shapes. Completely remove to transparent alpha: the whole moon and its nearby halo and thin wisps crossing it, all the original dark empty sky, EVERY mountain ridge and valley, both architectural borders including top beam elements, the pavilion, moon gate and foreground terrace. Every pixel in the lower two-thirds (below y=33.3%) must be transparent. Leave the left and right 7% width fully transparent. The open space within a soft circular exclusion around the moon center at 76% width, 28% height must be true alpha zero for a fixed moon. Export one full-canvas true-alpha cloud cutout, 16:9 and uncropped. No text, no UI, no checkerboard, no extra shapes and no background.

</details>

<details>
<summary>全景与衔接姿态生成提示词</summary>

Create a new widescreen 16:9 game stage background, for a distant establishing shot of the SAME ORIGINAL moon-gate mountain dueling terrace in the supplied project illustration. Maintain its slate blue stone, pale cyan mist, nuanced original painted 2D fantasy style, ancient inset bronze details and quiet moonlight. Pull the camera backwards by about twofold and slightly upwards. The scene should read as a very broad mountain-top martial arts courtyard. The original circular stone moon gate is now visibly small and far away along the BACK rail, near x=67%, with its full circular contour visible, about 26% of canvas height. Show more distant ridgelines, side terraces and a low carved stone rail extending across to both edges. Small lanterns remain architectural accents at the far edges with warm dim light. The continuous foreground sparring floor occupies the bottom 43% of the entire canvas, all the way from left to right, made of flat stone paving with subtle large tile structure and an understated shallow circular inscription in the central-third floor around y=79%. Keep the entire wide foreground unobstructed, natural and moderately dark so small figures and glow read on it. Preserve the project's cool restrained palette and directional lighting. Compose to support a punch-in toward the center of this same floor using generous tile detail and depth rather than adding foreground obstructions. Wide range, clean details, landscape. Only the original environment. No people or silhouettes, weapons, stairs in the foreground, text, UI, symbols floating in air, or HUD borders.

Create a single original transparent 2×2 in-game sprite atlas. The two supplied game sprites are identity and rendering references only. Keep their proportions, hairstyles, outfit construction and weapons and use the same clean thick black contours and flat angular cel-shaded textures. Both rows have two clear, DIFFERENT animation poses. Top row is Li Tiezhu facing right, with jade topknot, muted blue-green cape, ivory top and the pale jade straight sword. TOP LEFT: stationary anticipation before a melee strike, sword hand and blade pulled backward toward screen left, bent back elbow, flat front foot, weight on bent rear leg, eyes toward screen right. TOP RIGHT: planted melee finish, torso leaning forward toward screen right, front knee bent deep, rear toe supporting, sword arm follows through low across the right shin with blade tip on a low forward diagonal, cape lagging behind the shoulders. Bottom row is Su Ling'er facing left, dark mulberry ponytail and cinnabar ribbon, russet robe and bronze hand talisman. BOTTOM LEFT: stationary melee anticipation, both knees flexed and weight back, bronze talisman drawn back and up by shoulder on screen right, opposite hand extends screen left to gauge distance. BOTTOM RIGHT: planted finish with bent left front knee and weight forward, bronze talisman swept down to screen left around knee height, free arm counterbalances back, ponytail and hem still sweeping. Put each full figure in one quadrant with ample fully transparent gutters and border, keep weapon and feet entirely inside its cell. A balanced 3 to 3.5 heads tall, feet on common baselines by row. The existing original project style and color palettes must match. True alpha throughout the empty spaces. Draw only the characters and held weapons. No light, smears, visual effects, background, titles, crop guides or separate props.

</details>

<details>
<summary>败退姿态生成提示词</summary>

Use the two supplied ORIGINAL game character sheets as IDENTITY REFERENCES in listed order. Create a new landscape TWO CELL transparent character sprite atlas, one pose per character, simply side by side. LEFT CELL: the jade swordsman Li Tiezhu from reference 1, facing screen right in the original three-quarter side view. On his front knee, body sinking forward after losing a duel, one hand braced on the pommel of the original pale jade jian with the tip planted on the ground. The white tunic, teal cloak, black wrist wraps, jade belt and topknot crest match. RIGHT CELL: the vermilion cultivator Su Ling'er from reference 2 facing screen left in the original three-quarter side view. On one knee, head bowed slightly, free hand braced flat on ground, original square bronze fire talisman held low in her other hand. Keep angular maroon ponytail and red ribbon, russet tunic, cream sleeves and wrapped boots. Create a readable low full-body silhouette for each and keep approximately the same head size and chibi proportions in both cells. Match the sharp line weight, muted palette, strongly defined dark contours and crisp hand-drawn cel shadows of the supplied game sprite art. Each character fits completely inside its own half canvas, with ample fully transparent clearance between both cells, and complete boots, weapon, hand, cloth and hair. True transparent alpha inside all negative spaces; no ground, shadow or haze, no flames or particles, no frame or text, no additional objects. Keep the two figures' contact points near a shared low baseline.

</details>

`presentation/card_visuals.json` 的 `castElement` 可为卡牌开启施法演出并指定五行颜色。主动生效且没有拼点关联时读取这一项。
