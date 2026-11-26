shader_type canvas_item;

uniform sampler2D stage_matte : filter_linear, repeat_disable;

void fragment() {
    // 保留原图的建筑和地面颜色，只从 ImageGen 抠图读取透明度。
    float coverage = texture(stage_matte, UV).a;
    COLOR.a *= clamp((coverage - 0.01) / 0.96, 0.0, 1.0);
}
