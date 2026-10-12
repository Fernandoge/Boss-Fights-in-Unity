"""Turns the ninja's kick icon (Placeholders/Spell Icons/iconKick.png) into the Brute's: same tile, speed lines and burst, the ninja's body recoloured to skin and trousers,
the scarf replaced by a braid, a beard, and an axe in the raised hand.

Run:  python docs/axeman/make_kick_icon.py      (numpy only; writes Spell Icons/Axeman/HammerKickIcon.png)
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import icon_shapes as m
from pngio import read_png

ROOT = os.path.join(os.path.dirname(__file__), "..", "..", "BossFights", "Assets", "Placeholders", "Spell Icons")
SKIN = np.array([222, 158, 112], float)
SKIN_SHADE = np.array([150, 92, 66], float)
PANTS = np.array([112, 98, 78], float)
BOOT = np.array([84, 56, 38], float)
HAIR = np.array([62, 40, 28], float)
CREAM = np.array([255, 232, 196], float)
STEEL = np.array([205, 214, 228], float)
HANDLE = np.array([150, 98, 58], float)

Y, X = m.Y, m.X


def blend(base, color, cov):
    return base * (1 - cov[:, :, None]) + color[None, None, :] * cov[:, :, None]


def inpaint(img, mask, iterations=300):
    out = img.copy()
    out[mask] = out[~mask].mean(axis=0)
    for _ in range(iterations):
        padded = np.pad(out, ((1, 1), (1, 1), (0, 0)), mode="edge")
        avg = (padded[:-2, 1:-1] + padded[2:, 1:-1] + padded[1:-1, :-2] + padded[1:-1, 2:]) / 4
        out[mask] = avg[mask]
    return out


def main():
    src = read_png(os.path.join(ROOT, "iconKick.png"))[:, :, :3].astype(float)
    r, g, b = src[:, :, 0], src[:, :, 1], src[:, :, 2]
    lum = 0.3 * r + 0.59 * g + 0.11 * b

    scarf = (r > 170) & (g < 90) & (b < 110) & (X < 50)
    scarf = scarf | (np.zeros_like(scarf))
    bluish = b > r + 8
    navy = bluish & (lum < 100)
    outline = bluish & (lum >= 100)
    neutral = (np.abs(r - b) < 10) & (np.abs(r - g) < 12) & (lum < 60)
    boots = neutral & (((X > 26) & (X < 58) & (Y > 104)) | ((X > 82) & (X < 108) & (Y > 44) & (Y < 72)))

    # the scarf tails are removed from the background first
    scarf_big = scarf.copy()
    for _ in range(2):
        pad = np.pad(scarf_big, 1)
        scarf_big = scarf_big | pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]
    img = inpaint(src, scarf_big)

    # regions of the body, from where the ninja's limbs are in the 128 px image
    head = np.hypot(X - 37.5, Y - 40) < 12.5
    kick_leg = (X >= 56) & (Y >= 52)
    pants_zone = (Y >= 76) | kick_leg
    skin_zone = ~pants_zone
    shade = np.clip(lum / 62.0, 0.55, 1.45)[:, :, None]

    out = img.copy()
    body_pants = navy & pants_zone & ~head
    body_skin = navy & (skin_zone | head)
    skin_color = SKIN[None, None, :] * np.clip(shade, 0.6, 1.25)
    pants_color = PANTS[None, None, :] * np.clip(shade, 0.6, 1.25)
    region = np.where((pants_zone & ~head)[:, :, None], pants_color, skin_color)
    # every bluish pixel (the body, its pale outline and the blend between them) takes the region colour mixed with a warm rim by how light it is
    t = np.clip((lum - 60) / 110, 0, 1)[:, :, None]
    warm = region * (1 - t) + CREAM[None, None, :] * t
    bl = bluish[:, :, None]
    out = np.where(bl, warm, out)
    out[boots] = BOOT

    # a belt where the trousers start
    belt = (Y >= 74) & (Y < 79) & (X > 31) & (X < 57)
    belt &= navy | outline
    out[belt] = HANDLE * 0.8

    # head: hair on top, a beard, eyes (all drawn over the ninja's head)
    head_mask = m.coverage(m.circle(37.5, 40, 12.2))
    hair = head_mask * m.coverage(m.circle(37.5, 40, 12.2)) * np.clip((Y - 33) * -0.5 + 0.5, 0, 1) * 1.0
    out = blend(out, HAIR, hair)
    beard = m.coverage(m.subtract(m.circle(37.5, 40, 12.2), m.circle(37.5, 33.5, 10.5))) * np.clip((Y - 44) * 0.5 + 0.2, 0, 1)
    out = blend(out, HAIR, beard * 0.95)
    face = m.coverage(m.circle(39, 40, 5.8)) * (Y < 44)
    out = blend(out, SKIN, face * head_mask)
    for ex in (35.5, 42.5):
        out = blend(out, np.array([40, 22, 18], float), m.coverage(m.circle(ex, 38.5, 1.1)))
    out = blend(out, HAIR, m.coverage(m.capsule(32.5, 35.6, 45.5, 35.6, 1.6)) * head_mask)
    mustache = m.coverage(m.capsule(35, 42.5, 43, 42.5, 2.2))
    out = blend(out, HAIR, mustache * head_mask)

    # two braids streaming back where the scarf was (dense dots along a wavy line, thinner toward the tip)
    for (x0, y0, x1, y1) in ((29, 34, 7, 24), (29, 38, 6, 43)):
        points = []
        for k in range(24):
            u = k / 23
            points.append((x0 + (x1 - x0) * u, y0 + (y1 - y0) * u + np.sin(u * 4) * 1.2, 3.3 - 1.7 * u))
        for px, py, rad in points:
            out = blend(out, CREAM, m.coverage(m.circle(px, py, rad + 1.0)) * 0.9)
        for px, py, rad in points:
            out = blend(out, HAIR, m.coverage(m.circle(px, py, rad)))
        for px, py, rad in points[2::5]:
            out = blend(out, np.array([104, 70, 46], float), m.coverage(m.circle(px, py, 1.1)))

    # the axe in the raised hand: a handle and a flared blade with a lit cutting edge
    handle_d = m.capsule(61, 41, 80, 8, 3.2)
    blade_d = m.polygon([(79.5, 4), (91, -1), (96, 9), (92, 20), (79.5, 15)])
    spike_d = m.polygon([(79.5, 6), (72, 9), (79.5, 12.5)])
    axe_d = m.union(handle_d, blade_d, spike_d)
    out = blend(out, CREAM, m.coverage(axe_d - 1.4))
    out = blend(out, HANDLE, m.coverage(handle_d))
    out = blend(out, STEEL, m.coverage(m.union(blade_d, spike_d)))
    edge = m.capsule(91, -1, 96, 9, 2.2)
    edge = m.union(edge, m.capsule(96, 9, 92, 20, 2.2))
    out = blend(out, np.array([250, 252, 255], float), m.coverage(edge) * m.coverage(blade_d + 0.5))
    out = blend(out, np.array([120, 132, 152], float), m.coverage(m.capsule(80, 9.5, 90, 9.5, 1.3)) * m.coverage(blade_d + 1.5) * 0.7)

    result = (np.clip(out, 0, 255) + 0.5).astype(np.uint8)
    folder = os.path.join(ROOT, "Axeman")
    os.makedirs(folder, exist_ok=True)
    m.save_png(os.path.join(folder, "HammerKickIcon.png"), result)
    print("wrote HammerKickIcon.png")


if __name__ == "__main__":
    main()
