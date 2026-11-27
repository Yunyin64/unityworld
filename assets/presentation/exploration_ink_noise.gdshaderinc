// 低频决定落墨，较高频的纤维只打断笔锋，避免整张画都发花。
float ink_hash(vec2 p) {
    vec3 q = fract(vec3(p.xyx) * 0.1031);
    q += dot(q, q.yzx + 33.33);
    return fract((q.x + q.y) * q.z);
}

float ink_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(ink_hash(i), ink_hash(i + vec2(1, 0)), f.x),
        mix(ink_hash(i + vec2(0, 1)), ink_hash(i + 1.0), f.x), f.y);
}

float ink_wash(vec2 p) {
    return ink_noise(p) * 0.59 + ink_noise(p * 2.07 + 13.4) * 0.28
        + ink_noise(p * 4.39 - 7.6) * 0.13;
}
