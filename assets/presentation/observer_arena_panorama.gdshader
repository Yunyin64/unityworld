shader_type canvas_item;

uniform float panorama_offset = 0.0;
varying vec4 canvas_tint;

void vertex() {
    canvas_tint = COLOR;
}

void fragment() {
    // 全景图有两屏宽。采样点向右走，画面里的山脉向左走。
    COLOR = texture(TEXTURE, vec2(UV.x * 0.5 + panorama_offset, UV.y)) * canvas_tint;
}
