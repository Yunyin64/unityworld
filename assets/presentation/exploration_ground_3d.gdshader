shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

uniform sampler2D rock_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_surface : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D litter_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;

varying vec3 world_position;
varying vec3 world_normal;

float hash21(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float grain(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    vec2 t = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(cell), hash21(cell + vec2(1.0, 0.0)), t.x),
        mix(hash21(cell + vec2(0.0, 1.0)), hash21(cell + 1.0), t.x), t.y);
}

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
    vec3 base_normal = normalize(world_normal);
    float talus = 1.0 - base_normal.y;
    vec2 p = world_position.xz;
    float terrain_scale = grain(p * 0.84);
    float blotches = grain(p * 5.2);
    float flecks = grain(p * 24.0);
    // 三平面投影把岩纹贴在垂直崖面，按法线平滑过渡到坡顶。
    vec3 w = pow(abs(base_normal), vec3(5.0));
    w /= max(dot(w, vec3(1.0)), 0.001);
    vec3 sample_pos = world_position * 1.05 + vec3(terrain_scale * 0.11, blotches * 0.035, terrain_scale * 0.081);
    vec2 rx = sample_pos.zy + vec2(0.13, 0.42);
    vec2 ry = sample_pos.xz;
    vec2 rz = sample_pos.xy + vec2(0.49, 0.17);
    vec3 stone_sample = texture(rock_albedo, rx).rgb * w.x + texture(rock_albedo, ry).rgb * w.y
        + texture(rock_albedo, rz).rgb * w.z;
    vec2 drm_x = texture(rock_surface, rx).rg, drm_y = texture(rock_surface, ry).rg;
    vec2 drm = drm_x * w.x + drm_y * w.y + texture(rock_surface, rz).rg * w.z;
    vec3 stone = mix(vec3(0.090, 0.103, 0.098), stone_sample * vec3(0.75, 0.88, 0.91), 0.72);
    stone *= 0.75 + 0.48 * terrain_scale;
    vec2 nx = texture(rock_normal, rx).rg * 2.0 - 1.0;
    vec2 ny = texture(rock_normal, ry).rg * 2.0 - 1.0;
    vec2 nz = texture(rock_normal, rz).rg * 2.0 - 1.0;
    vec3 rock_gradient = vec3(0, nx.y, nx.x) * w.x + vec3(ny.x, 0, ny.y) * w.y + vec3(nz.x, nz.y, 0) * w.z;

    vec2 grass_uv = p * 2.3 + vec2(0.07, 0.08) * terrain_scale;
    vec3 photo_grass = texture(grass_albedo, grass_uv).rgb;
    vec2 grass_bump = texture(grass_normal, grass_uv).rg * 2.0 - 1.0;
    vec3 litter = texture(litter_albedo, p * 1.9 + vec2(0.36, 0.12)).rgb;
    float grass_detail = dot(photo_grass, vec3(0.2126, 0.7152, 0.0722));
    vec3 grass = COLOR.rgb * (0.38 + 1.46 * grass_detail) * (0.66 + 0.43 * terrain_scale + 0.13 * blotches);
    grass = mix(grass, photo_grass * vec3(0.68, 0.91, 0.60), 0.32);
    float trail = UV2.x;
    float damp = smoothstep(-0.16, 0.0, UV2.y);
    vec3 earth = mix(COLOR.rgb * (0.65 + blotches * 0.25), litter * vec3(1.12, 1.16, 0.98), 0.45);
    grass = mix(grass, earth, max(trail * 0.94, damp * 0.72));

    float rock_amount = smoothstep(0.20, 0.51, talus + UV.x * 0.21 + (blotches - 0.5) * 0.22);
    rock_amount = max(rock_amount, damp * smoothstep(0.03, 0.45, talus));
    float snow_line = 1.64 + (terrain_scale - 0.5) * 0.37 + (blotches - 0.5) * 0.22;
    float snow = smoothstep(snow_line, snow_line + 0.23, world_position.y)
        * smoothstep(0.28, 0.78, UV.y) * (1.0 - smoothstep(0.46, 0.83, talus));
    vec3 snow_color = mix(vec3(0.50, 0.64, 0.72), vec3(0.84, 0.89, 0.91), 0.57 + 0.34 * flecks);
    ALBEDO = mix(mix(grass, stone, rock_amount), snow_color, snow);
    vec3 gradient = mix(vec3(grass_bump.x, 0, grass_bump.y) * (0.32 - trail * 0.13), rock_gradient * 0.72, rock_amount);
    gradient *= 1.0 - snow * 0.77;
    gradient -= base_normal * dot(gradient, base_normal);
    NORMAL = normalize(mat3(VIEW_MATRIX) * (base_normal + gradient));
    AO = mix(0.88 + grass_detail * 0.16, drm.x, rock_amount * (1.0 - snow * 0.8));
    AO_LIGHT_AFFECT = 0.19;
    ROUGHNESS = mix(0.91 - damp * 0.28, mix(0.52, 0.96, drm.y), rock_amount);
    SPECULAR = 0.31;
}
