# Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
"""Fit CC0 clothing and bake a seated pose, preserving the source UV atlases.

Run scripts/fetch_customer.py first. Only exposed head/neck/chest and hands are
exported from the anatomical base. No unclothed full body or runtime rig is shipped.
The fit, skinning and subdivision below are original project code.
"""
from collections import defaultdict
from pathlib import Path
import json
import math
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / ".tools" / "customer-source"
OUTPUT = ROOT / "Assets" / "Customer"
SEAT_OFFSET = (0, .89, 0)


def add(a, b): return tuple(x + y for x, y in zip(a, b))
def sub(a, b): return tuple(x - y for x, y in zip(a, b))
def mul(a, n): return tuple(x * n for x in a)
def dot(a, b): return sum(x * y for x, y in zip(a, b))
def cross(a, b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def unit(a): return mul(a, 1 / max(1e-10, math.sqrt(dot(a, a))))
def mean(points):
    points = list(points)
    return tuple(sum(p[j] for p in points) / len(points) for j in range(len(points[0])))


def read_obj(path):
    vertices, uvs, faces = [], [], []
    group = ""
    for line in path.read_text().splitlines():
        words = line.split()
        if not words: continue
        if words[0] == "v": vertices.append(tuple(map(float, words[1:4])))
        elif words[0] == "vt": uvs.append(tuple(map(float, words[1:3])))
        elif words[0] == "g": group = " ".join(words[1:])
        elif words[0] == "f":
            face = [tuple(int(x) - 1 for x in word.split("/")[:2]) for word in words[1:]]
            faces.append((face, group))
    return vertices, uvs, faces


def rotation_between(a, b):
    a, b = unit(a), unit(b)
    v, c = cross(a, b), dot(a, b)
    def rotate(p):
        # Rodrigues' shortest-arc rotation; the seated pose has no antiparallel bones.
        return add(p, add(cross(v, p), mul(cross(v, cross(v, p)), 1 / max(.00001, 1 + c))))
    return rotate


def align_frames(forward, across, target_forward, target_across):
    f = unit(forward)
    x = unit(sub(across, mul(f, dot(across, f))))
    n = cross(f, x)
    tf = unit(target_forward)
    tx = unit(sub(target_across, mul(tf, dot(target_across, tf))))
    tn = cross(tf, tx)
    return lambda p: add(add(mul(tx, dot(p, x)), mul(tf, dot(p, f))), mul(tn, dot(p, n)))


def subdivision(vertices, uvs, faces):
    """One Catmull-Clark step, keeping UV seams separate from geometry seams."""
    face_centers = [mean(vertices[i] for i, _ in f) for f, _ in faces]
    edges = defaultdict(list)
    adjacent = defaultdict(list)
    for fi, (face, _) in enumerate(faces):
        for j, (a, _) in enumerate(face):
            b = face[(j + 1) % len(face)][0]
            edges[tuple(sorted((a, b)))].append(fi)
            adjacent[a].append(fi)
    neighbors = defaultdict(set)
    boundary = defaultdict(list)
    for (a, b), attached in edges.items():
        neighbors[a].add(b); neighbors[b].add(a)
        if len(attached) == 1:
            boundary[a].append(b); boundary[b].append(a)
    points = list(vertices)
    for i, attached in adjacent.items():
        p = vertices[i]
        if len(boundary[i]) == 2:
            points[i] = mul(add(mul(p, 6), add(vertices[boundary[i][0]], vertices[boundary[i][1]])), .125)
        elif len(attached) >= 3:
            f = mean(face_centers[j] for j in attached)
            r = mean(mul(add(p, vertices[j]), .5) for j in neighbors[i])
            points[i] = mul(add(add(f, mul(r, 2)), mul(p, len(attached)-3)), 1 / len(attached))
    edge_ids = {}
    for edge, attached in edges.items():
        a, b = edge
        edge_ids[edge] = len(points)
        points.append(mean([vertices[a], vertices[b]] + [face_centers[j] for j in attached])
                      if len(attached) == 2 else mean([vertices[a], vertices[b]]))
    center_start = len(points)
    points += face_centers
    texcoords = list(uvs)
    result = []
    for fi, (face, group) in enumerate(faces):
        middle_uv = len(texcoords)
        texcoords.append(mean(uvs[t] for _, t in face))
        for j, (v, t) in enumerate(face):
            before, after = face[(j-1) % len(face)], face[(j+1) % len(face)]
            uv_after = len(texcoords); texcoords.append(mean([uvs[t], uvs[after[1]]]))
            uv_before = len(texcoords); texcoords.append(mean([uvs[t], uvs[before[1]]]))
            result.append(([(v, t), (edge_ids[tuple(sorted((v, after[0])))], uv_after),
                            (center_start+fi, middle_uv), (edge_ids[tuple(sorted((before[0], v)))], uv_before)], group))
    return points, texcoords, result


def write_obj(path, vertices, uvs, faces, smooth=True):
    if smooth: vertices, uvs, faces = subdivision(vertices, uvs, faces)
    used = sorted({i for face, _ in faces for i, _ in face})
    mapping = {old: new + 1 for new, old in enumerate(used)}
    lines = ["# Derived CC0 MakeHuman graphics. Original fitting and seated pose. See SOURCES.md."]
    lines += ["v " + " ".join(f"{x:.7f}" for x in vertices[i]) for i in used]
    lines += [f"vt {u:.7f} {v:.7f}" for u, v in uvs]
    lines += ["s 1"]
    for face, _ in faces:
        lines.append("f " + " ".join(f"{mapping[v]}/{t+1}" for v, t in face))
    path.write_text("\n".join(lines) + "\n")
    print(path.name, len(used), "vertices", len(faces), "faces")


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    base, skin_uvs, body_faces = read_obj(SOURCE / "base.obj")
    rig = json.loads((SOURCE / "default.mhskel").read_text())
    source_weights = json.loads((SOURCE / "default_weights.mhw").read_text())["weights"]
    base = [mul(v, .1) for v in base]
    weights = defaultdict(dict)
    for bone, entries in source_weights.items():
        for i, w in entries: weights[i][bone] = w
    def joint(name): return mean(base[i] for i in rig["joints"][name])
    def bone_head(name): return joint(rig["bones"][name]["head"])
    def bone_tail(name): return joint(rig["bones"][name]["tail"])
    identity = lambda p: p
    transforms = {}
    neck = bone_head("neck01")
    gaze_angle = -.23
    def gaze(p):
        c, s = math.cos(gaze_angle), math.sin(gaze_angle)
        return (p[0]*c+p[2]*s, p[1], -p[0]*s+p[2]*c)
    for bone in rig["bones"]:
        ancestor = bone
        while ancestor:
            if ancestor == "neck01":
                transforms[bone] = (neck, neck, gaze)
                break
            ancestor = rig["bones"][ancestor]["parent"]
    for side, sign in [("L", 1), ("R", -1)]:
        shoulder = bone_head("upperarm01." + side)
        elbow, wrist = bone_head("lowerarm01." + side), bone_head("wrist." + side)
        target_elbow = (sign*.270, 1.230-SEAT_OFFSET[1], .125)
        target_wrist = (sign*.235, 1.160-SEAT_OFFSET[1], .350)
        upper_rotation = rotation_between(sub(elbow, shoulder), sub(target_elbow, shoulder))
        lower_rotation = rotation_between(sub(wrist, elbow), sub(target_wrist, target_elbow))
        transforms.update({"upperarm01."+side: (shoulder, shoulder, upper_rotation),
                           "upperarm02."+side: (shoulder, shoulder, upper_rotation),
                           "lowerarm01."+side: (elbow, target_elbow, lower_rotation),
                           "lowerarm02."+side: (elbow, target_elbow, lower_rotation)})
        finger = bone_tail("finger3-3." + side)
        across = sub(bone_tail("finger2-3."+side), bone_tail("finger5-3."+side))
        hand_rotation = align_frames(sub(finger, wrist), across, (0, .10, 1), (-sign, 0, 0))
        for name in rig["bones"]:
            if name.endswith("."+side) and any(name.startswith(n) for n in ["wrist", "metacarpal", "finger"]):
                transforms[name] = (wrist, target_wrist, hand_rotation)
        hip, knee, ankle = bone_head("upperleg01."+side), bone_head("lowerleg01."+side), bone_head("foot."+side)
        target_knee = (sign*.16, .84-SEAT_OFFSET[1], .41)
        target_ankle = (sign*.16, .43-SEAT_OFFSET[1], .37)
        thigh_rotation = rotation_between(sub(knee, hip), sub(target_knee, hip))
        shin_rotation = rotation_between(sub(ankle, knee), sub(target_ankle, target_knee))
        for name in ["upperleg01", "upperleg02"]: transforms[name+"."+side] = (hip, hip, thigh_rotation)
        for name in ["lowerleg01", "lowerleg02"]: transforms[name+"."+side] = (knee, target_knee, shin_rotation)
        for name in rig["bones"]:
            if name.endswith("."+side) and (name.startswith("foot") or name.startswith("toe")):
                transforms[name] = (ankle, target_ankle, identity)
    def posed(v, influences):
        total = sum(influences.values())
        if not total: return add(v, SEAT_OFFSET)
        result = (0, 0, 0)
        for bone, weight in influences.items():
            origin, target, rotate = transforms.get(bone, ((0,0,0), (0,0,0), identity))
            result = add(result, mul(add(target, rotate(sub(v, origin))), weight / total))
        return add(result, SEAT_OFFSET)
    posed_base = [posed(v, weights[i]) for i, v in enumerate(base)]
    # Include clavicles and the open shirt's chest area; their lower edge is well inside the shirt.
    face_faces = [(f,g) for f,g in body_faces if g == "body" and
                  all(base[i][1] > .445 and abs(base[i][0]) < .19 for i,_ in f)]
    hand_faces = [(f,g) for f,g in body_faces if g == "body" and
                  all(abs(base[i][0]) > .391 and base[i][1] > .05 for i,_ in f)]
    write_obj(OUTPUT / "head.obj", posed_base, skin_uvs, face_faces)
    write_obj(OUTPUT / "hands.obj", posed_base, skin_uvs, hand_faces)

    def fit_surface(stem, output, smooth=True):
        vertices, uvs, faces = read_obj(SOURCE / (stem+".obj"))
        mappings, scales, section = [], [1,1,1], ""
        for line in (SOURCE / (stem+".mhclo")).read_text().splitlines():
            w = line.split()
            if not w or w[0].startswith("#"): continue
            if w[0] in ["x_scale", "y_scale", "z_scale"]:
                axis = "xyz".index(w[0][0]); a, b = int(w[1]), int(w[2])
                scales[axis] = abs(base[a][axis]-base[b][axis]) / (float(w[3])*.1)
            elif w[0] == "verts": section = "verts"
            elif w[0] == "delete_verts": section = "delete"
            elif section == "verts" and w[0].isdigit():
                if len(w) == 1: mappings.append(([int(w[0])], [1.], (0,0,0)))
                else: mappings.append(([int(i) for i in w[:3]], list(map(float,w[3:6])), tuple(float(x)*.1 for x in w[6:9])))
        assert len(mappings) == len(vertices)
        fitted = []
        for ids, barycentric, offset in mappings:
            v = (0,0,0); influences = defaultdict(float)
            for i, amount in zip(ids, barycentric):
                v = add(v, mul(base[i], amount))
                for bone, weight in weights[i].items(): influences[bone] += weight*amount
            v = add(v, tuple(x*s for x,s in zip(offset,scales)))
            fitted.append(posed(v, influences))
        write_obj(OUTPUT / output, fitted, uvs, faces, smooth)
    fit_surface("male_casualsuit01","clothing.obj")
    fit_surface("eyebrow001","brows.obj",False)
    fit_surface("eyelashes01","lashes.obj",False)

    for source, target in [("short04.obj", "hair.obj"), ("high-poly.obj", "eyes.obj")]:
        v, uv, faces = read_obj(SOURCE / source)
        if source == "high-poly.obj":
            v = [(x, y+7.2842-(15.6+15.91)/2, z+1.2453-(1.22+1.46)/2) for x,y,z in v]
            faces = [(f,g) for f,g in faces if not all(uv[t][0]>.85 and uv[t][1]<.15 for _,t in f)]
        write_obj(OUTPUT / target, [add(add(neck,gaze(sub(mul(p,.1),neck))),SEAT_OFFSET) for p in v], uv, faces, smooth=False)
    for source, target in [("middleage_lightskinned_male_diffuse.png","skin.png"),
                           ("short04_diffuse.png","hair.png"), ("brown_eye.png","eyes.png"),
                           ("male_casualsuit01_diffuse.png","clothing-albedo.png"),
                           ("male_casualsuit01_normal.png","clothing-normal.png"),
                           ("male_casualsuit01_ao.png","clothing-ao.png"),
                           ("eyebrow001.png","brows.png"), ("eyelashes01.png","lashes.png")]:
        shutil.copyfile(SOURCE/source, OUTPUT/target)
    print("Prepared one consistently fitted, seated customer; neck and garment share the same reference pose.")
