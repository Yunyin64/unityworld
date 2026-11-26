shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx, alpha_to_coverage;

uniform sampler2D foliage : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
uniform float wind_strength = 0.034;
uniform float alpha_cut = 0.30;
uniform bool outline_profile_capture = false;
varying float height_fraction;
varying float leaf_light;

void vertex() {
    vec3 anchor = MODEL_MATRIX[3].xyz;
    float elevation = clamp(VERTEX.y, 0.0, 1.0);
    float gust = sin(TIME * 1.05 + dot(anchor.xz, vec2(1.95, 0.99)));
    float sway = gust * 0.67 + sin(TIME * 2.07 + anchor.z * 2.9) * 0.24;
    VERTEX.xz += vec2(1.0, 0.58) * wind_strength * elevation * elevation * sway;
    height_fraction = elevation;
    leaf_light = (gust + 1.0) * 0.5;
}

void fragment() {
    vec4 leaf = texture(foliage, UV);
    if (outline_profile_capture && leaf.a < alpha_cut) discard;
    // 类别缓冲只写离散值，半透明叶尖不与背后山体的 ID 混色。
    ALPHA = outline_profile_capture ? 1.0 : leaf.a;
    ALPHA_SCISSOR_THRESHOLD = outline_profile_capture ? 0.5 : alpha_cut;
    ALPHA_ANTIALIASING_EDGE = outline_profile_capture ? 0.0 : alpha_cut * 0.45;
    ALPHA_TEXTURE_COORDINATE = UV * vec2(textureSize(foliage, 0));
    vec3 undertone = vec3(0.060, 0.095, 0.070);
    vec3 pigment = leaf.rgb * vec3(0.79, 1.04, 0.82) + undertone * 0.21;
    ALBEDO = outline_profile_capture ? vec3(0.0)
        : pigment * COLOR.rgb * (0.72 + 0.36 * height_fraction + leaf_light * 0.08);
    EMISSION = outline_profile_capture ? vec3(0.0, 1.0, 0.0) : vec3(0.0);
    BACKLIGHT = ALBEDO * vec3(0.31, 0.39, 0.26);
    ROUGHNESS = mix(0.92, 0.67, height_fraction);
    SPECULAR = 0.22;
    AO = 0.65 + height_fraction * 0.32;
}
