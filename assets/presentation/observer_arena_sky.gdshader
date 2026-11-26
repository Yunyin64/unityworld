shader_type canvas_item;

uniform sampler2D cloud_matte : filter_linear, repeat_disable;
uniform float cloud_offset = 0.0;
uniform float upper_cloud_offset = 0.0;
varying vec4 canvas_tint;

void vertex() {
    canvas_tint = COLOR;
}

void fragment() {
    // 提取图的透明度只决定原画里哪些云纹可以移动，画面仍全部取自同一张原图。
    // 让云的位移在画幅边缘归零，保留老图的边框。
    float edge_distance = min(UV.x, 1.0 - UV.x);
    float inset = smoothstep(0.085, 0.128, edge_distance) * smoothstep(0.05, 0.09, UV.y);
    float layer_offset = mix(upper_cloud_offset, cloud_offset, smoothstep(0.12, 0.22, UV.y));
    float drift = layer_offset * inset * smoothstep(0.27, 0.85, texture(cloud_matte, UV).a);
    if (abs(drift) > 0.000001) {
        COLOR = texture(TEXTURE, UV - vec2(drift, 0.0)) * canvas_tint;
    }
}
