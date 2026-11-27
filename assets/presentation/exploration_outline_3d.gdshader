shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled, depth_test_disabled, depth_draw_never;

#include "res://assets/presentation/exploration_ink_noise.gdshaderinc"

uniform sampler2D scene_depth : hint_depth_texture, filter_nearest, repeat_disable;
uniform sampler2D scene_normal : hint_normal_roughness_texture, filter_nearest, repeat_disable;
uniform sampler2D outline_profiles : filter_nearest, repeat_disable;
uniform vec4 ink : source_color = vec4(0.082, 0.065, 0.058, 1.0);
uniform float mountain_width_px : hint_range(1.0, 6.0, 0.05) = 3.35;
uniform float building_width_px : hint_range(0.5, 4.0, 0.05) = 1.85;
uniform float foliage_width_px : hint_range(0.0, 2.5, 0.05) = 0.78;
uniform float overall_weight : hint_range(0.25, 1.75, 0.05) = 1.0;
uniform bool ink_mode = false;

// 屏幕像素轴和对角线共同处理，避免只在横竖边上稳定。
const vec2 axes[4] = { vec2(1, 0), vec2(0, 1), vec2(0.707107, 0.707107), vec2(-0.707107, 0.707107) };

struct Stroke {
    float silhouette_width;
    float silhouette_opacity;
    float crease_opacity;
    vec3 tint;
};

void vertex() {
    POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

int profile_at(vec2 uv) {
    vec3 label = step(vec3(0.5), textureLod(outline_profiles, uv, 0.0).rgb);
    return int(dot(label, vec3(1.0, 2.0, 4.0)));
}

Stroke profile_stroke(int profile) {
    if (ink_mode) {
        if (profile == 1) return Stroke(2.15, 0.40, 0.15, vec3(0.031, 0.048, 0.035));
        if (profile == 2) return Stroke(0.85, 0.48, 0.0, vec3(0.012, 0.025, 0.009));
        if (profile == 3) return Stroke(1.75, 0.72, 0.26, vec3(0.016, 0.012, 0.007));
        if (profile == 4) return Stroke(1.05, 0.37, 0.13, vec3(0.025, 0.035, 0.025));
        if (profile == 5) return Stroke(0.0, 0.0, 0.0, vec3(0.0));
    }
    if (profile == 1) return Stroke(mountain_width_px, 0.87, 0.36, vec3(0.0));
    if (profile == 2) return Stroke(foliage_width_px, 0.62, 0.0, vec3(0.003, 0.010, 0.003));
    if (profile == 3) return Stroke(building_width_px, 0.86, 0.54, vec3(0.007, 0.003, 0.001));
    if (profile == 4) return Stroke(1.10, 0.41, 0.16, vec3(0.003));
    if (profile == 5) return Stroke(0.65, 0.29, 0.0, vec3(0.001, 0.007, 0.010));
    if (profile == 6) return Stroke(1.35, 0.86, 0.25, vec3(0.003));
    return Stroke(1.15, 0.63, 0.23, vec3(0.004));
}

float raw_depth(vec2 uv) {
    return textureLod(scene_depth, uv, 0.0).r;
}

float linear_depth(float raw, mat4 inv_projection) {
    // 两种镜头的 view.z / view.w 都只依赖 NDC.z，省掉每个采样点的完整矩阵乘法。
    return -(inv_projection[2].z * raw + inv_projection[3].z)
        / (inv_projection[2].w * raw + inv_projection[3].w);
}

float depth_at(vec2 uv, mat4 inv_projection) {
    return linear_depth(raw_depth(uv), inv_projection);
}

vec3 normal_at(vec2 uv) {
    return normalize(textureLod(scene_normal, uv, 0.0).xyz * 2.0 - 1.0);
}

float contour_pair(vec2 uv, vec2 tap, float center, float step_world, int profile, mat4 inv_projection) {
    float a = depth_at(uv - tap, inv_projection) - center;
    float b = depth_at(uv + tap, inv_projection) - center;
    float near_gap = min(abs(a), abs(b));
    float far_gap = max(a, b);

    // 深度二阶差分抑制连续坡面，只在靠前的几何表面落笔。
    float shoulder = 1.0 - smoothstep(step_world * 2.4 + 0.012, step_world * 5.1 + 0.035, near_gap);
    float jump = far_gap - near_gap * 1.38;
    float edge = smoothstep(step_world * 1.65 + 0.011, step_world * 4.0 + 0.052, jump) * shoulder;
    if (edge <= 0.015 || profile != 2) return edge;

    // 同一片树冠内部、重叠枝叶之间的深度断层不画；留下树冠和其它表面的分界。
    vec2 outward = b > a ? tap : -tap;
    if (profile_at(uv + outward) == 2) return 0.0;
    vec2 tangent = vec2(-outward.y, outward.x);
    float support = float(profile_at(uv - outward) == 2)
        + float(profile_at(uv - outward + tangent) == 2)
        + float(profile_at(uv - outward - tangent) == 2);
    return edge * smoothstep(0.7, 2.2, support);
}

float contour_ring(vec2 uv, vec2 texel, float radius, float center, float world_px, int profile, mat4 inv_projection) {
    float edge = 0.0;
    for (int direction = 0; direction < 4; direction++) {
        vec2 tap = axes[direction] * texel * radius;
        edge = max(edge, contour_pair(uv, tap, center, world_px * radius, profile, inv_projection));
    }
    return edge;
}

float crease_pair(vec2 uv, vec2 tap, float center, vec3 normal, float world_px, mat4 inv_projection) {
    vec2 a = uv - tap;
    vec2 b = uv + tap;
    float raw_a = raw_depth(a), raw_b = raw_depth(b);
    if (min(raw_a, raw_b) <= 0.000001) return 0.0;
    float da = abs(linear_depth(raw_a, inv_projection) - center);
    float db = abs(linear_depth(raw_b, inv_projection) - center);
    float support = 1.0 - smoothstep(world_px * 2.8 + 0.022, world_px * 5.0 + 0.055, max(da, db));
    if (support <= 0.01) return 0.0;
    float turn_a = 1.0 - dot(normal, normal_at(a));
    float turn_b = 1.0 - dot(normal, normal_at(b));
    return smoothstep(0.23, 0.69, max(turn_a, turn_b) - min(turn_a, turn_b) * 1.46) * support;
}

float atmosphere_at(int profile, float depth, bool perspective) {
    return perspective ? 1.0 - smoothstep(profile == 1 ? 13.0 : 8.5, 31.0, depth) : 1.0;
}

float edge_coverage(vec2 uv, vec2 texel, float center, float pixel, bool perspective, mat4 inv_projection) {
    float coverage = 0.0;
    for (int direction = 0; direction < 4; direction++) {
        vec2 tap = axes[direction / 2] * texel * (direction % 2 == 0 ? 1.0 : -1.0);
        vec2 neighbor = uv + tap;
        float adjacent = depth_at(neighbor, inv_projection);
        float step_world = pixel * (perspective ? adjacent : 1.0);
        float separation = smoothstep(step_world * 1.9 + 0.018, step_world * 4.7 + 0.075,
            center - adjacent);
        if (separation <= 0.03) continue;
        int family = profile_at(neighbor);
        if (family != 1 && family != 3) continue;
        float width = profile_stroke(family).silhouette_width * overall_weight;
        // 留很薄的一层外侧覆盖，接住 MSAA 在明亮山脊和屋檐上留下的子像素边。
        if (profile_at(uv + tap * 2.0) == family)
            coverage = max(coverage, separation * atmosphere_at(family, adjacent, perspective) * min(width, 1.0));
    }
    return coverage * 0.49;
}

void fragment() {
    float raw = raw_depth(SCREEN_UV);
    int profile = profile_at(SCREEN_UV);
    float center = linear_depth(raw, INV_PROJECTION_MATRIX);
    bool perspective = abs(PROJECTION_MATRIX[3][3]) < 0.5;
    float pixel = 2.0 / (VIEWPORT_SIZE.y * abs(PROJECTION_MATRIX[1][1]));
    float world_px = pixel * (perspective ? center : 1.0);
    vec2 texel = 1.0 / VIEWPORT_SIZE;
    vec3 stroke_ink = ink_mode ? vec3(0.033, 0.045, 0.034) : ink.rgb;
    float fibers = ink_mode ? 0.49 + 0.51 * smoothstep(0.20, 0.68,
        ink_noise(FRAGCOORD.xy * vec2(0.21, 0.47))) : 1.0;

    if (profile == 0) {
        // 草丛也写入 ID 缓冲的深度，只有空背景接受山体和建筑的外侧覆盖。
        if (raw > 0.000001) discard;
        ALBEDO = stroke_ink;
        ALPHA = edge_coverage(SCREEN_UV, texel, center, pixel, perspective, INV_PROJECTION_MATRIX) * ink.a * fibers;
    } else {
        Stroke stroke = profile_stroke(profile);
        if (stroke.silhouette_width <= 0.0) discard;

        // 900 像素画高为美术基准。整数半径之间按覆盖率过渡，细线可小于一个像素。
        float resolution_scale = clamp(VIEWPORT_SIZE.y / 900.0, 0.65, 1.65);
        float width = stroke.silhouette_width * overall_weight * resolution_scale;
        float inner_radius = max(1.0, floor(width));
        float contour = contour_ring(SCREEN_UV, texel, inner_radius, center, world_px, profile, INV_PROJECTION_MATRIX);
        if (width > inner_radius) {
            float outer = contour_ring(SCREEN_UV, texel, inner_radius + 1.0, center, world_px, profile, INV_PROJECTION_MATRIX);
            contour = mix(contour, max(contour, outer), fract(width));
        }
        contour *= min(width, 1.0);

        // 形体内的折线单独控制，宽山体轮廓也不会加粗岩纹和屋瓦的每条细节。
        float crease = 0.0;
        if (stroke.crease_opacity > 0.0 && contour < 0.81) {
            vec3 normal = normal_at(SCREEN_UV);
            float crease_scale = max(1.0, resolution_scale);
            for (int direction = 0; direction < 4; direction++) {
                vec2 tap = axes[direction] * texel * crease_scale;
                crease = max(crease, crease_pair(SCREEN_UV, tap, center, normal, world_px * crease_scale, INV_PROJECTION_MATRIX));
            }
        }

        float atmosphere = atmosphere_at(profile, center, perspective);
        float foliage_distance = profile == 2 ? 1.0 - smoothstep(0.013, 0.047, world_px) * 0.62 : 1.0;
        float fill = 0.0;
        if ((profile == 1 || profile == 3 || profile == 5) && contour < 0.40)
            fill = edge_coverage(SCREEN_UV, texel, center, pixel, perspective, INV_PROJECTION_MATRIX);
        ALBEDO = stroke_ink + stroke.tint;
        float inside = max(contour * stroke.silhouette_opacity, crease * stroke.crease_opacity);
        ALPHA = max(inside * atmosphere * foliage_distance, fill) * ink.a * fibers;
    }
}
