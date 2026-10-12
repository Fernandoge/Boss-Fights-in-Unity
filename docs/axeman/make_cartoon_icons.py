"""The Axeman's other skill icons (W, R, D and Space), drawn in the style of the ninja's kick icon: a dark vignette tile, a cartoon Brute with a pale rim and soft shading, speed lines and a burst.
The kick icon (F) comes from make_kick_icon.py, which edits the ninja's sprite itself.

Run:  python docs/axeman/make_cartoon_icons.py      (numpy only; writes into BossFights/Assets/Placeholders/Spell Icons/Axeman/)
A pose is a few joint positions in 128 px image coordinates (y down); change them and rerun.
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import icon_shapes as m

X, Y = m.X, m.Y
OUT = m.OUT

SKIN = np.array([222, 158, 112], float)
PANTS = np.array([112, 98, 78], float)
BOOT = np.array([84, 56, 38], float)
HAIR = np.array([62, 40, 28], float)
CREAM = np.array([255, 232, 196], float)
STEEL = np.array([205, 214, 228], float)
HANDLE = np.array([150, 98, 58], float)
BELT = np.array([120, 78, 46], float)


def blend(base, color, cov):
    return base * (1 - cov[:, :, None]) + np.asarray(color, float)[None, None, :] * cov[:, :, None]


def tile(center, edge):
    r = np.clip(np.hypot(X - 64, Y - 64) / 90.0, 0, 1)[:, :, None] ** 1.4
    return np.asarray(center, float)[None, None, :] * (1 - r) + np.asarray(edge, float)[None, None, :] * r


def limb(a, b, w0, w1):
    ax, ay = a
    bx, by = b
    px, py = X - ax, Y - ay
    dx, dy = bx - ax, by - ay
    t = np.clip((px * dx + py * dy) / (dx * dx + dy * dy), 0, 1)
    radius = (w0 + (w1 - w0) * t) / 2
    d = np.hypot(px - t * dx, py - t * dy) - radius
    return d, radius


class Scene:
    def __init__(self, center, edge):
        self.img = tile(center, edge)
        self.parts = []

    def speed_lines(self, lines, color=(214, 180, 190), alpha=0.55):
        for (x0, y0, x1, y1) in lines:
            self.img = blend(self.img, color, m.coverage(m.capsule(x0, y0, x1, y1, 3.2)) * alpha)

    def add(self, d, color, radius=None, shade=True):
        self.parts.append((d, np.asarray(color, float), radius, shade))

    def limb(self, a, b, w0, w1, color):
        d, radius = limb(a, b, w0, w1)
        self.add(d, color, radius)

    def draw(self):
        silhouette = np.minimum.reduce([p[0] for p in self.parts])
        self.img = blend(self.img, CREAM, m.coverage(silhouette - 1.5))
        for d, color, radius, shade in self.parts:
            cov = m.coverage(d)
            if shade and radius is not None:
                depth = np.clip(-d / np.maximum(radius, 1e-3), 0, 1)
                bright = (0.74 + 0.26 * np.sqrt(depth))[:, :, None]
                self.img = self.img * (1 - cov[:, :, None]) + color[None, None, :] * bright * cov[:, :, None]
            else:
                self.img = blend(self.img, color, cov)
        self.parts = []

    def burst(self, cx, cy, big, small, points=9, rotation=0.0):
        pts = []
        for i in range(points * 2):
            a = np.pi * i / points + rotation
            rad = big if i % 2 == 0 else small
            pts.append((cx + rad * np.cos(a), cy + rad * np.sin(a)))
        star = m.polygon(pts)
        self.img = blend(self.img, (150, 60, 10), m.coverage(star - 1.6))
        self.img = blend(self.img, (255, 205, 60), m.coverage(star))
        self.img = blend(self.img, (255, 250, 220), m.coverage(m.circle(cx, cy, small * 0.9)) * 0.9)

    def png(self):
        return (np.clip(self.img, 0, 255) + 0.5).astype(np.uint8)


# --- the Brute ---

def brute(scene, pose, facing=1):
    """pose: head, neck, hip, plus (shoulder, elbow, hand) and (hip, knee, foot) chains for the near and far side."""
    for key in ("far_leg", "far_arm"):
        draw_chain(scene, pose, key)
    scene.limb(pose["hip"], pose["neck"], 17, 25, SKIN)
    scene.add(m.circle(pose["hip"][0], pose["hip"][1], 10.5), PANTS, 10.5)
    for key in ("near_leg", "near_arm"):
        draw_chain(scene, pose, key)
    axis = (pose["neck"] - pose["hip"]) / np.linalg.norm(pose["neck"] - pose["hip"])
    across = np.array([-axis[1], axis[0]])
    belt = pose["hip"] + axis * 7
    scene.add(m.capsule(belt[0] - across[0] * 8.5, belt[1] - across[1] * 8.5, belt[0] + across[0] * 8.5, belt[1] + across[1] * 8.5, 4.5), BELT, shade=False)
    return head(scene, pose["head"], facing)


def draw_chain(scene, pose, key):
    chain = pose[key]
    if key.endswith("leg"):
        hip, knee, foot = chain
        scene.limb(hip, knee, 14, 11, PANTS)
        scene.limb(knee, foot, 11, 9.5, PANTS)
        toe = foot + (np.array([1.0, 0.0]) * pose.get("facing", 1)) * 4
        scene.limb(foot + (knee - foot) * 0.0, toe + np.array([0.0, 1.0]), 9, 9, BOOT)
    else:
        shoulder, elbow, hand = chain
        scene.limb(shoulder, elbow, 11, 9.5, SKIN)
        scene.limb(elbow, hand, 9.5, 8.5, SKIN)


def head(scene, center, facing):
    cx, cy = center
    scene.add(m.circle(cx, cy, 11.5), SKIN, 11.5)
    # drawn after the silhouette: hair, beard, face, eyes (they only add detail inside the head)
    return lambda img: head_details(img, cx, cy, facing)


def head_details(img, cx, cy, facing):
    inside = m.coverage(m.circle(cx, cy, 11.4))
    hair = inside * np.clip((cy - 3 - Y) * 0.6 + 0.5, 0, 1)
    img = blend(img, HAIR, hair)
    beard = m.coverage(m.subtract(m.circle(cx, cy, 11.4), m.circle(cx, cy - 6.5, 10.0))) * np.clip((Y - (cy + 3.5)) * 0.5 + 0.2, 0, 1)
    img = blend(img, HAIR, beard * 0.95)
    img = blend(img, SKIN, m.coverage(m.circle(cx + facing * 1.5, cy, 5.4)) * (Y < cy + 4) * inside)
    for ex in (-3.4, 3.2):
        img = blend(img, (40, 22, 18), m.coverage(m.circle(cx + facing * 1.5 + ex, cy - 1.4, 1.05)))
    img = blend(img, HAIR, m.coverage(m.capsule(cx + facing * 1.5 - 5.5, cy - 4.2, cx + facing * 1.5 + 5.5, cy - 4.2, 1.5)) * inside)
    img = blend(img, HAIR, m.coverage(m.capsule(cx + facing * 1.5 - 4, cy + 2.6, cx + facing * 1.5 + 4, cy + 2.6, 2.0)) * inside)
    # a braid down the back of the head
    back = -facing
    for k in range(9):
        u = k / 8
        px, py = cx + back * (9 + 11 * u), cy - 3 + 6 * u + np.sin(u * 5) * 1.2
        img = blend(img, CREAM, m.coverage(m.circle(px, py, 3.3 - 1.4 * u + 1.0)) * 0.9)
    for k in range(9):
        u = k / 8
        px, py = cx + back * (9 + 11 * u), cy - 3 + 6 * u + np.sin(u * 5) * 1.2
        img = blend(img, HAIR, m.coverage(m.circle(px, py, 3.3 - 1.4 * u)))
    return img


def axe_prop(scene, butt, top, flip=1, blade_scale=1.0):
    """A handle from butt to top with a flared blade at the top; flip = +1 puts the blade on the right of the handle's direction, -1 on the left."""
    butt = np.asarray(butt, float)
    top = np.asarray(top, float)
    direction = (top - butt) / np.linalg.norm(top - butt)
    normal = np.array([-direction[1], direction[0]]) * flip
    scene.add(m.capsule(butt[0], butt[1], top[0], top[1], 3.4), HANDLE, shade=False)

    def point(along, across):
        p = top + direction * along * blade_scale + normal * across * blade_scale
        return (p[0], p[1])

    blade = m.polygon([point(1.5, 0), point(-3, 8), point(-9, 15), point(-14, 9), point(-12, 2), point(-9, -1)])
    spike = m.polygon([point(-3, -0.5), point(-6, -8), point(-8.5, -0.5)])
    scene.add(m.union(blade, spike), STEEL, shade=False)
    return blade


def finish(scene, details, axe_blades=()):
    scene.draw()
    for fn in details:
        scene.img = fn(scene.img)
    for blade, edge_from, edge_to in axe_blades:
        pass
    return scene.png()


# --- the icons ---

def pose_vec(**kw):
    return {k: ([np.array(p, float) for p in v] if isinstance(v, (list, tuple)) and isinstance(v[0], (list, tuple)) else np.array(v, float)) for k, v in kw.items()}


def draw_rush():
    # Rushing Cleave: a lunge with a huge slash trailing the axe
    scene = Scene((120, 52, 14), (40, 12, 8))
    scene.speed_lines([(6, 70, 34, 70), (10, 92, 40, 92), (4, 112, 28, 112)])
    # the slash: a wide pale crescent behind the swing
    for radius, shift, glow in ((56, 5, 0.5), (46, 4, 0.35)):
        slash = m.subtract(m.circle(66, 60, radius), m.circle(66 + shift, 60 - shift, radius))
        scene.img = blend(scene.img, (255, 170, 80), m.coverage(slash - 2.0) * glow)
        scene.img = blend(scene.img, (255, 245, 220), m.coverage(slash + 0.4) * 0.95)
    pose = pose_vec(
        head=(70, 46), neck=(66, 56), hip=(46, 80), facing=1,
        far_leg=[(46, 80), (30, 92), (14, 106)], far_arm=[(66, 58), (52, 66), (40, 60)],
        near_leg=[(46, 80), (70, 88), (80, 108)], near_arm=[(66, 58), (84, 58), (98, 46)],
    )
    pose["facing"] = 1
    details = [brute(scene, pose, 1)]
    axe_prop(scene, (86, 64), (112, 22), flip=-1)
    return finish(scene, details)


def draw_leap():
    # Execution Leap: down onto the ring of the impact with the axe overhead
    scene = Scene((96, 20, 24), (30, 6, 10))
    scene.speed_lines([(20, 6, 20, 40), (108, 4, 108, 38), (122, 26, 122, 52)])
    ring = m.ellipse_ring(64, 108, 52, 13, 5)
    scene.img = blend(scene.img, (255, 190, 90), m.coverage(m.ellipse_ring(64, 108, 52, 13, 9)) * 0.4)
    scene.img = blend(scene.img, (255, 240, 200), m.coverage(ring))
    for cx, cy, r in ((16, 108, 7), (112, 106, 6), (30, 116, 5), (98, 116, 5)):
        scene.img = blend(scene.img, (190, 150, 120), m.coverage(m.circle(cx, cy, r)) * 0.8)
    pose = pose_vec(
        head=(64, 44), neck=(64, 56), hip=(64, 82), facing=1,
        far_leg=[(60, 82), (44, 92), (52, 104)], far_arm=[(60, 58), (48, 48), (54, 32)],
        near_leg=[(68, 82), (84, 92), (74, 104)], near_arm=[(68, 58), (84, 46), (76, 30)],
    )
    details = [brute(scene, pose, 1)]
    axe_prop(scene, (40, 38), (92, 14), flip=1)
    return finish(scene, details)


def draw_throw():
    # Axe Throw: the arm snaps forward and the axe spins away
    scene = Scene((70, 40, 110), (18, 10, 40))
    scene.speed_lines([(6, 96, 36, 96), (4, 112, 30, 112)], color=(190, 180, 220))
    for start, end in ((200, 300), (20, 120)):
        scene.img = blend(scene.img, (230, 220, 255), m.coverage(m.arc_ring(100, 30, 22, 3.5, start, end)) * 0.85)
    pose = pose_vec(
        head=(44, 44), neck=(48, 56), hip=(44, 86), facing=1,
        far_leg=[(44, 86), (28, 100), (20, 116)], far_arm=[(48, 58), (34, 66), (24, 56)],
        near_leg=[(44, 86), (64, 98), (72, 116)], near_arm=[(48, 58), (68, 54), (84, 40)],
    )
    details = [brute(scene, pose, 1)]
    axe_prop(scene, (94, 50), (112, 12), flip=-1)
    return finish(scene, details)


def draw_dash():
    # Escape jump: a long leap with dust at the take off
    scene = Scene((40, 90, 110), (8, 20, 36))
    scene.speed_lines([(6, 40, 40, 40), (4, 60, 30, 60), (12, 84, 40, 84)], color=(190, 215, 235))
    for cx, cy, r in ((18, 112, 9), (34, 118, 7), (8, 118, 6), (46, 120, 5)):
        scene.img = blend(scene.img, (200, 215, 225), m.coverage(m.circle(cx, cy, r)) * 0.85)
    pose = pose_vec(
        head=(86, 40), neck=(76, 50), hip=(54, 70), facing=1,
        far_leg=[(54, 70), (70, 88), (50, 96)], far_arm=[(76, 52), (62, 40), (46, 38)],
        near_leg=[(54, 70), (36, 78), (20, 74)], near_arm=[(76, 52), (60, 56), (44, 50)],
    )
    details = [brute(scene, pose, 1)]
    return finish(scene, details)


ICONS = {
    "RushingCleaveIcon.png": draw_rush,
    "ExecutionLeapIcon.png": draw_leap,
    "AxeThrowIcon.png": draw_throw,
    "EscapeJumpIcon.png": draw_dash,
}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for name, draw in ICONS.items():
        m.save_png(os.path.join(OUT, name), draw())
        print("wrote", name)
