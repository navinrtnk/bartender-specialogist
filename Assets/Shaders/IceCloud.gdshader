// Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
shader_type spatial;
render_mode blend_mix, cull_back, shadows_disabled;
varying vec3 local_position;
varying float seed;

float hash3(vec3 p) {
    p = fract(p * 0.3183099 + vec3(0.13, 0.27, 0.61));
    return fract(17.0 * p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3(vec3 p) {
    vec3 i = floor(p), f = fract(p);
    f = f*f*(3.0-2.0*f);
    return mix(mix(mix(hash3(i),hash3(i+vec3(1,0,0)),f.x),
                   mix(hash3(i+vec3(0,1,0)),hash3(i+vec3(1,1,0)),f.x),f.y),
               mix(mix(hash3(i+vec3(0,0,1)),hash3(i+vec3(1,0,1)),f.x),
                   mix(hash3(i+vec3(0,1,1)),hash3(i+vec3(1,1,1)),f.x),f.y),f.z);
}

void vertex() {
    local_position = VERTEX;
    seed = dot(MODEL_MATRIX[3].xyz, vec3(77.0, 13.0, 41.0));
}

void fragment() {
    float fibers = noise3(local_position*vec3(52.0,15.0,52.0)+seed);
    float cloud = noise3(local_position*9.0+seed) * 0.65 + fibers * 0.35;
    // Approximate optical thickness through the sphere: dense center, feathered silhouette.
    float edge = pow(clamp(dot(normalize(NORMAL),normalize(VIEW)),0.0,1.0),2.8);
    ALBEDO = vec3(0.78,0.87,0.89);
    ROUGHNESS = 0.55;
    SPECULAR = 0.2;
    ALPHA = edge * (0.22 + cloud*0.22);
}
