shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled, depth_test_disabled, depth_draw_never;

#include "res://assets/presentation/exploration_ink_noise.gdshaderinc"

uniform sampler2D scene_color : hint_screen_texture, filter_linear, repeat_disable;
uniform sampler2D scene_depth : hint_depth_texture, filter_nearest, repeat_disable;

void vertex() {
    POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

float ink_darkness(vec2 uv) {
    return 1.0 - clamp(dot(textureLod(scene_color, uv, 0.0).rgb, vec3(0.2126, 0.7152, 0.0722)), 0.0, 1.0);
}

float depth_of(float raw, mat4 inverse_projection) {
    return -(inverse_projection[2].z * raw + inverse_projection[3].z)
        / (inverse_projection[2].w * raw + inverse_projection[3].w);
}

void fragment() {
    float source_depth = textureLod(scene_depth, SCREEN_UV, 0.0).r;
    float depth = depth_of(source_depth, INV_PROJECTION_MATRIX);
    float center_ink = ink_darkness(SCREEN_UV);
    if (center_ink > 0.82) discard;
    vec2 fiber_position = FRAGCOORD.xy * vec2(0.19, 0.48);
    float fiber = ink_noise(fiber_position);
    vec2 radius = (1.0 + 2.2 * fiber) / VIEWPORT_SIZE;
    float blur_ink = center_ink * 2.0;
    // Tideborn's eight-tap wet edge. Treat an empty receiver as paper and stop
    // a mark from painting through nearer geometry.
    for (int i = 0; i < 8; i++) {
        float angle = float(i) * 0.78539816;
        vec2 uv = SCREEN_UV + vec2(cos(angle), sin(angle)) * radius;
        float raw = textureLod(scene_depth, uv, 0.0).r;
        float neighbor = depth_of(raw, INV_PROJECTION_MATRIX);
        float allowed = source_depth < 0.000001 ? 1.0
            : 1.0 - smoothstep(0.02, 0.09, abs(neighbor - depth) / max(depth, 0.01));
        blur_ink += mix(center_ink, ink_darkness(uv), allowed);
    }
    float spread = max(0.0, blur_ink * 0.1 - center_ink - 0.025);
    ALBEDO = vec3(0.045, 0.053, 0.046);
    ALPHA = spread * (0.42 + 0.42 * fiber);
}
