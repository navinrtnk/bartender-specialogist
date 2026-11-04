// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_back;

uniform sampler2D scene_color : hint_screen_texture, repeat_disable, filter_linear_mipmap;
varying vec3 local_position;
varying float cube_seed;

float hash3(vec3 p) {
    p = fract(p * 0.3183099 + vec3(0.13, 0.27, 0.61));
    return fract(17.0 * p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3(vec3 p) {
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix(hash3(i), hash3(i+vec3(1,0,0)), f.x),
                   mix(hash3(i+vec3(0,1,0)), hash3(i+vec3(1,1,0)), f.x), f.y),
               mix(mix(hash3(i+vec3(0,0,1)), hash3(i+vec3(1,0,1)), f.x),
                   mix(hash3(i+vec3(0,1,1)), hash3(i+vec3(1,1,1)), f.x), f.y), f.z);
}

void vertex() {
    local_position = VERTEX;
    cube_seed = dot(MODEL_MATRIX[3].xyz, vec3(17.71, 31.13, 23.87));
}

void fragment() {
    vec3 p = local_position;
    float broad = noise3(p * 9.0 + cube_seed);
    float grain = noise3(p * 110.0 + cube_seed);
    float fissures = pow(max(0.0, 1.0 - abs(noise3(p*18.0+cube_seed) - 0.48) / 0.018), 3.0);
    vec3 ripple = vec3(sin(p.y*125.0 + sin(p.z*83.0)),
                       sin(p.z*117.0 + sin(p.x*91.0)), sin(p.x*131.0 + sin(p.y*89.0)));
    NORMAL = normalize(NORMAL + ripple * 0.025);
    float fresnel = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 4.0);
    vec2 uv = clamp(SCREEN_UV + NORMAL.xy * (0.004 + broad*0.004), vec2(0.001), vec2(0.999));
    vec3 refracted = textureLod(scene_color, uv, 0.1 + broad*0.35).rgb;
    ALBEDO = mix(vec3(0.22, 0.34, 0.38), vec3(0.73, 0.85, 0.86), fissures * 0.65);
    EMISSION = refracted * 0.3;
    ROUGHNESS = 0.045 + grain * 0.055 + fissures*0.08;
    SPECULAR = 0.9;
    METALLIC = 0.0;
    CLEARCOAT = 0.8;
    CLEARCOAT_ROUGHNESS = 0.06;
    ALPHA = clamp(0.28 + fresnel*0.56 + fissures*0.16 + broad*0.035, 0.20, 0.9);
}
