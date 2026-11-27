shader_type canvas_item;
render_mode unshaded, blend_mul;

#include "res://assets/presentation/exploration_ink_noise.gdshaderinc"

void fragment() {
    vec2 p = FRAGCOORD.xy;
    float cloud = ink_wash(p * vec2(0.011, 0.019));
    float fiber = ink_noise(p * vec2(0.18, 0.71));
    float tooth = ink_hash(floor(p));
    float value = 1.0 - (0.026 * cloud + 0.033 * fiber + 0.015 * tooth);
    COLOR = vec4(vec3(value, value * 0.999, value * 0.995), 1.0);
}
