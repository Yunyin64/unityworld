shader_type spatial;
render_mode unshaded, cull_disabled;

#include "res://assets/presentation/exploration_ink_noise.gdshaderinc"
#include "res://assets/presentation/exploration_ink_optics.gdshaderinc"

uniform sampler2D cliff_brush : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec4 paper : source_color;
uniform vec4 pigment : source_color;
uniform vec4 mineral : source_color;
uniform int surface_kind : hint_enum("地形", "远峰", "物件", "水面", "外海") = 0;
uniform bool use_vertex_color = false;
varying vec3 world_position;
varying vec3 world_normal;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
    vec3 n = normalize(world_normal);
    vec2 p = world_position.xz;
    float pooling = ink_wash(p * 0.77 + world_position.y * vec2(0.34, -0.23));
    vec3 dye = pigment.rgb;
    float coverage;
    if (surface_kind >= 3) {
        // 地形网格已有每个顶点的水深，水纹从真实岸线往外化开。
        float depth = surface_kind == 4 ? 0.7 : UV.x;
        float bank = 1.0 - smoothstep(0.015, 0.19, depth);
        vec2 current = vec2(p.x * 0.19 + p.y * 0.075, p.y * 1.68 - p.x * 0.34);
        float tide = ink_wash(current * vec2(1.0, 0.47) + vec2(TIME * -0.032, TIME * 0.065));
        float line = abs(sin(current.y * 1.8 + tide * 3.0 - TIME * 0.53));
        float broken = (1.0 - smoothstep(0.035, 0.25 + fwidth(current.y), line))
            * smoothstep(0.36, 0.67, ink_noise(current * vec2(0.65, 2.6)));
        float edge = (1.0 - smoothstep(0.012, 0.10, depth)) * (0.45 + 0.55 * pooling);
        float crest = bank * smoothstep(0.30, 0.83, ink_noise(current + TIME * 0.12));
        coverage = (0.025 + tide * 0.075 + broken * 0.105 + edge * 0.19) * (1.0 - crest * 0.28);
        // 远水与天空使用同一纸色，收掉外海网格在天边留下的硬线。
        coverage *= 1.0 - smoothstep(24.0, 68.0, -VERTEX.z);
        dye = mix(pigment.rgb, mineral.rgb, 0.64);
    } else if (surface_kind == 2) {
        float turning = 0.5 + 0.5 * dot(n, normalize(vec3(-0.55, 0.78, 0.27)));
        float fibers = ink_noise(p * vec2(8.0, 19.0) + world_position.y * vec2(0.7, 6.1));
        float tint = use_vertex_color ? clamp(dot(COLOR.rgb, vec3(0.22, 0.70, 0.08)), 0.0, 1.0) : 0.28;
        coverage = clamp(0.75 - turning * 0.38 + pooling * 0.28 + (fibers - 0.5) * 0.09 - tint * 0.24, 0.0, 1.0);
        dye = mix(pigment.rgb, mineral.rgb, 0.52 + 0.16 * turning);
    } else {
        float far_peak = float(surface_kind == 1);
        float slope = clamp(1.0 - n.y, 0.0, 1.0);
        vec2 face = vec2(dot(p, vec2(0.83, -0.35)) + world_position.y * 0.45,
            dot(p, vec2(0.39, 0.87)) + world_position.y * 1.16);
        vec2 warp = vec2(pooling, ink_wash(p * 0.61 - 11.3)) - 0.5;
        vec3 brush = texture(cliff_brush, face * vec2(0.115, 0.16) + warp * 0.24).rgb;
        float brush_dark = 1.0 - smoothstep(0.13, 0.67, dot(brush, vec3(0.22, 0.70, 0.08)));
        float dry = smoothstep(0.65, 0.84, ink_noise(face * vec2(7.9, 1.57) + warp));
        float deposits = smoothstep(0.23, 0.70, 0.49 * pooling + 0.13 * brush_dark + slope * 0.37);
        float mineral_trace = smoothstep(0.24, 0.66, pooling + brush_dark * 0.18)
            * smoothstep(0.05, 0.56, slope + UV.x * 0.24);
        float shoreline = (1.0 - far_peak) * smoothstep(-0.18, -0.018, UV2.y)
            * (1.0 - smoothstep(0.015, 0.13, UV2.y));
        float reserve = max(dry * 0.28, UV2.x * 0.26) + smoothstep(0.4, 0.75, UV.y) * 0.17;
        float cliff = smoothstep(0.25, 0.73, slope + UV.x * 0.22);
        float ink = 0.024 + 0.049 * pooling + deposits * 0.29 + shoreline * 0.058;
        coverage = tideborn_ink_coverage(ink, reserve, cliff * brush_dark * 0.32, dry * cliff * 0.046);
        if (surface_kind == 1) {
            float foot = smoothstep(-0.35, 4.4, world_position.y);
            coverage *= (0.23 + 0.77 * foot) * 0.74;
        }
        dye = mix(pigment.rgb, mineral.rgb, 0.23 + mineral_trace * 0.42);
        dye = mix(dye, brush, cliff * brush_dark * 0.23);
    }
    ALBEDO = mix(paper.rgb, dye, clamp(coverage, 0.0, 0.97));
}
