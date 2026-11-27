shader_type spatial;
render_mode unshaded, cull_disabled;

#include "res://assets/presentation/exploration_ink_noise.gdshaderinc"
#include "res://assets/presentation/exploration_ink_optics.gdshaderinc"

uniform sampler2D brush_atlas : filter_linear_mipmap, repeat_disable;
uniform sampler2D flow_noise : filter_linear_mipmap, repeat_enable;
uniform vec4 paper : source_color = vec4(0.953, 0.925, 0.863, 1.0);
uniform vec4 ink : source_color = vec4(0.102, 0.125, 0.121, 1.0);

varying vec3 paint_position;
varying vec3 form_normal;
varying float wing_weight;

void vertex() {
    paint_position = CUSTOM0.xyz;
    wing_weight = CUSTOM0.w;
    form_normal = CUSTOM1.xyz;
}

// Port of the original object-space wash and atlas sampling, using Godot's
// imported skin weights to separate the flying feathers from the body.
void fragment() {
    vec3 p = paint_position;
    vec3 n = normalize(form_normal);
    float wing = smoothstep(0.16, 0.81, wing_weight);
    vec2 lengthwise = vec2(p.z * 8.2 - p.x * wing * 0.72, p.y * 7.6 + abs(p.x) * (3.3 + wing * 4.3));
    vec2 flow = texture(flow_noise, lengthwise * 0.43 + vec2(0.0, TIME * 0.038)).gb - 0.5;
    float wash = ink_wash(lengthwise + flow * 0.27);
    float pooling = ink_wash(lengthwise * 2.07 + 7.4);
    vec2 stamp = lengthwise * vec2(1.0, 0.49) + flow * 0.38;
    vec2 cell = floor(stamp);
    float seed = ink_hash(cell);
    float slot = mod(floor(seed * 23.17), 8.0);
    vec2 tile = vec2(mod(slot, 4.0), floor(slot / 4.0));
    vec2 tip = clamp(fract(stamp), vec2(0.015), vec2(0.985));
    float brush = texture(brush_atlas, (tile + tip) / vec2(4.0, 2.0)).r;
    float dorsal = mix(n.y * 0.5 + 0.5, abs(n.y), wing);
    float pigment = mix(0.37 + 0.87 * pow(dorsal, 1.05),
        0.14 + 0.70 * smoothstep(0.24, 0.77, wash), wing);
    pigment *= 0.88 + 0.24 * wash;
    pigment *= 0.94 + 0.12 * pooling;
    pigment += (0.5 - 0.5 * dot(normalize(NORMAL), normalize(vec3(-0.44, 0.58, 0.68)))) * 0.060;
    float bristle = ink_noise(lengthwise * vec2(8.7, 17.0) + p.xz * vec2(43.0, 12.0));
    float broken = smoothstep(0.46, 0.69, bristle + 0.14 * (wash - 0.5));
    pigment *= 1.0 - broken * mix(0.13, 0.44, wing);
    float underside = smoothstep(0.05, 0.85, -n.y);
    pigment *= 1.0 - underside * mix(0.63, 0.10, wing);
    float reserve = wing * (0.08 + smoothstep(0.48, 0.82, abs(p.x) * 2.9) * 0.20)
        + (1.0 - wing) * underside * 0.096;
    float coverage = tideborn_ink_coverage(pigment, reserve, brush * mix(0.11, 0.13, wing),
        broken * wing * (1.0 - brush) * 0.042);
    ALBEDO = mix(paper.rgb, ink.rgb, coverage);
}
