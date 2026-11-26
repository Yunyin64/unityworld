shader_type sky;

float hash(vec2 p) {
    vec3 z = fract(vec3(p.xyx) * 0.1031);
    z += dot(z, z.yzx + 33.33);
    return fract((z.x + z.y) * z.z);
}

float cloud_noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x),
        mix(hash(i + vec2(0, 1)), hash(i + 1.0), u.x), u.y);
}

float clouds(vec2 p) {
    return cloud_noise(p) * 0.55 + cloud_noise(p * 2.11 + vec2(17, 5)) * 0.27
        + cloud_noise(p * 4.33 + vec2(4, 16)) * 0.12 + cloud_noise(p * 9.15) * 0.06;
}

void sky() {
    vec3 dir = EYEDIR;
    vec3 sun = normalize(vec3(-0.72, 0.24, 0.49));
    float horizon = exp(-max(dir.y, 0.0) * 5.1);
    float sunward = pow(max(dot(dir, sun), 0.0), 7.0);
    vec3 zenith = vec3(0.125, 0.225, 0.36);
    vec3 air = mix(vec3(0.40, 0.54, 0.67), vec3(0.86, 0.56, 0.41), sunward * 0.57);
    vec3 color = mix(zenith, air, horizon);
    if (dir.y > 0.017) {
        vec2 p = dir.xz * (0.66 / (dir.y + 0.105));
        p *= vec2(0.86, 1.6);
        float form = clouds(p + vec2(8.2, -3.8));
        float rim = clouds(p + vec2(8.28, -3.79)) - form;
        float cover = smoothstep(0.40, 0.63, form) * smoothstep(0.017, .13, dir.y);
        vec3 clouds_color = mix(vec3(0.34, 0.43, 0.56), vec3(0.84, 0.82, 0.76),
            clamp(0.21 + form * 0.67 + rim * 4.1 + sunward * 0.38, 0.0, 1.0));
        color = mix(color, clouds_color, cover);
    }
    COLOR = color + vec3(0.53, 0.32, 0.18) * pow(max(dot(dir, sun), 0.0), 28.0) * 0.22;
}
