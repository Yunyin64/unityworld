shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled;

uniform int profile_id = 0;
uniform bool from_terrain = false;

void fragment() {
    // 同一张地表网格里，山体按地形采样权重获得粗线，其余地面保留浅线。
    int label = from_terrain ? (UV.x >= 0.51 ? 1 : 4) : profile_id;
    ALBEDO = vec3(float(label & 1), float((label >> 1) & 1), float((label >> 2) & 1));
}
