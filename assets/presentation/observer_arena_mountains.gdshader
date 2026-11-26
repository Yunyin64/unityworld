shader_type canvas_item;

uniform float pan = 0.0;

void fragment() {
    // 镜接长卷的接缝连续，移动方向由场景中的速度控制。
    float x = 1.0 - abs(mod(UV.x * 0.38 + 0.18 + pan, 2.0) - 1.0);
    vec4 terrain = texture(TEXTURE, vec2(x, UV.y));
    COLOR = vec4(terrain.rgb * vec3(0.78, 0.86, 0.95), terrain.a);
}
