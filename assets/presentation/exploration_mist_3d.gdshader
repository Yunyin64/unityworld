shader_type fog;

float hash31(vec3 p) {
    p = fract(p * vec3(0.1031, 0.11369, 0.13787));
    p += dot(p, p.yxz + 19.19);
    return fract((p.x + p.y) * p.z);
}

float volume_noise(vec3 p) {
    vec3 cell = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(hash31(cell), hash31(cell + vec3(1, 0, 0)), f.x);
    float b = mix(hash31(cell + vec3(0, 1, 0)), hash31(cell + vec3(1, 1, 0)), f.x);
    float c = mix(hash31(cell + vec3(0, 0, 1)), hash31(cell + vec3(1, 0, 1)), f.x);
    float d = mix(hash31(cell + vec3(0, 1, 1)), hash31(cell + vec3(1, 1, 1)), f.x);
    return mix(mix(a, b, f.y), mix(c, d, f.y), f.z);
}

void fog() {
    vec3 p = WORLD_POSITION * vec3(0.74, 2.16, 0.80) + vec3(TIME * -0.08, 0, TIME * 0.043);
    float turbulence = volume_noise(p) * 0.60 + volume_noise(p * 2.14 + 6.3) * 0.28
        + volume_noise(p * 4.17 + 15.1) * 0.12;
    float cloud = smoothstep(0.31, 0.71, turbulence);
    float volume_falloff = smoothstep(0.0, 0.29, -SDF);
    float height = 1.0 - smoothstep(0.41, 0.91, UVW.y);
    DENSITY = cloud * volume_falloff * height * 0.90;
    ALBEDO = vec3(0.73, 0.81, 0.88);
}
