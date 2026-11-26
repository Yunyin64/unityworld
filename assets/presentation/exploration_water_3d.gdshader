shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform bool outer_ocean = false;
varying vec3 world_position;

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 p = world_position.xz;
    float depth = outer_ocean ? 0.8 : UV.x;
    float current = sin(p.x * 0.66 + sin(p.y * 0.23)) * cos(p.y * 0.52 + p.x * 0.32);
    float swell = dot(p, vec2(3.5, 5.2)) - TIME * 0.94 + current * 4.3;
    float chop = dot(p, vec2(-2.7, 7.8)) + TIME * 0.65 + sin(p.x * 1.6) * 1.7;
    float fine = dot(p, vec2(11.4, -15.6)) - TIME * 0.87 + sin(p.y * 8.5) * 1.6;
    float absorption = 1.0 - exp(-depth * 4.75);
    vec3 deep = vec3(0.008, 0.040, 0.065) * (0.94 + current * 0.12);
    vec3 shallow = vec3(0.058, 0.210, 0.167);
    vec3 water = mix(shallow, deep, absorption);
    float bank = 1.0 - smoothstep(0.035, 0.38, depth);
    float surf_crest = cos(depth * 130.0 - TIME * 2.1 + sin(swell) * 1.8 + sin(chop) * 0.62);
    float broken_foam = smoothstep(0.40, 0.89, surf_crest + cos(fine) * 0.16);
    float shore = (1.0 - smoothstep(0.002, 0.028, depth)) * (0.40 + broken_foam * 0.42);
    shore += (1.0 - smoothstep(0.025, 0.105, depth)) * broken_foam * (0.19 + sin(chop) * 0.16);
    float ripple_visibility = 1.0 - smoothstep(0.08, 0.27, length(fwidth(p)));
    float caustic = pow(1.0 - abs(cos(swell * 2.0 + sin(chop) * 1.2) * cos(fine * 0.92 + current * 3.6)), 9.0);
    water += vec3(0.034, 0.072, 0.052) * caustic * bank * (1.0 - shore) * ripple_visibility;
    ALBEDO = mix(water, vec3(0.64, 0.75, 0.74), clamp(shore, 0.0, 0.82));
    vec2 slope = vec2(0.034, 0.049) * cos(swell) + vec2(-0.022, 0.048) * cos(chop)
        + vec2(0.019, -0.011) * cos(fine) * (0.59 + bank * 0.41);
    NORMAL = normalize(mat3(VIEW_MATRIX) * vec3(slope.x * ripple_visibility, 1.0, slope.y * ripple_visibility));
    ROUGHNESS = mix(0.17 + 0.07 * (current + 1.0), 0.48, clamp(shore * 1.6, 0.0, 1.0));
    SPECULAR = 0.48;
}
