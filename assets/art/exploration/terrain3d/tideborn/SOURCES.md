# 潜鳞资产来源

- `yinglong-rigged-v2-gray.glb` 来自《潜鳞》线上构建的 `yinglong-rigged-v2-gray-VG121D_R.glb`。源文件以原始字节保存，SHA-256 为 `bb03333621e9a58fe53e148549aa15461fbe2e900ba634b0dc2c5a63c854bff0`。
- `chapter-cliff-ink-c76.webp` 来自同一构建的 `chapter-cliff-ink-c76-VC9FQUAv.webp`，SHA-256 为 `242bdd6f05256b4d833a98e7fdbbf349191d5478a2196f9626ad0aac9f89107c`。
- 笔刷图集、流向噪声和来源清单来自 `Ryeblablabla/tideborn` 的 `1d1a753e04cc453ed7a96cdd8cd62499b14755d3`。笔刷的原素材与 CC0-1.0 许可见 [来源清单](shared-ink-resources.json)。
- Godot 的积墨合成和湿边采样移植自同一提交的 `src/ink/shared-ink/ink-wash.js`，模型上的笔刷取样参考了 `surface-brush.js`。上游代码许可见 [MIT 许可](TIDEBORN-LICENSE.txt)。
- 山门巡飞移植了 `flight-stroke.js`、`flight-limbs.js`、`spine-beat.js` 和 `dynamic-plumage.js` 的翼相位、躯干延迟、翼节和羽梢弹簧。`dragon.js` 和 `curve-field.js` 的龙首轨迹取样在 Godot 中直接驱动原应龙脊骨。
