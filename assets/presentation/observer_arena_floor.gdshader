shader_type canvas_item;

// 只保留原图的石台、栏杆和近处建筑。月门内圈交给远处山脉。
void fragment() {
    vec2 p = UV;
    float arch_outer = length((p - vec2(0.673, 0.520)) / vec2(0.067, 0.113));
    float arch_inner = length((p - vec2(0.673, 0.522)) / vec2(0.048, 0.085));
    float opening = (1.0 - smoothstep(0.96, 1.035, arch_inner)) *
        (1.0 - smoothstep(0.600, 0.608, p.y));
    float arch = (1.0 - smoothstep(0.979, 1.012, arch_outer)) * (1.0 - opening);
    float terrace = smoothstep(0.570, 0.588, p.y) * (1.0 - opening);
    float edge = min(p.x, 1.0 - p.x);
    float beams = 1.0 - smoothstep(0.032, 0.044, edge);
    float corner = 1.0 - smoothstep(0.085, 0.104, edge + p.y * 0.85);
    float pavilion_width = smoothstep(0.536, 0.546, p.x) * (1.0 - smoothstep(0.611, 0.625, p.x));
    float roof_line = 0.423 + abs(p.x - 0.597) * 0.95;
    float pavilion = pavilion_width * smoothstep(roof_line - 0.004, roof_line + 0.006, p.y);
    float lantern_1 = 1.0 - smoothstep(0.013, 0.025, abs(p.x - 0.070));
    float lantern_2 = 1.0 - smoothstep(0.013, 0.025, abs(p.x - 0.930));
    float lanterns = max(lantern_1, lantern_2) * smoothstep(0.516, 0.533, p.y);
    float alpha = max(max(terrace, arch), max(max(beams, corner), max(pavilion, lanterns)));
    COLOR.a *= alpha;
}
