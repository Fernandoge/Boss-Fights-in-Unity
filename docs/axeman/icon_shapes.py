"""Shapes and PNG writing shared by the Axeman icon scripts (make_kick_icon.py and make_cartoon_icons.py): signed distance functions in pixels (negative inside), plus a small PNG writer.
Needs only numpy.
"""
import os
import struct
import zlib

import numpy as np

SIZE = 128
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "BossFights", "Assets", "Placeholders", "Spell Icons", "Axeman")

Y, X = np.mgrid[0:SIZE, 0:SIZE].astype(np.float64)
X += 0.5
Y += 0.5


# --- shapes: each returns a distance in pixels (negative inside) ---

def circle(cx, cy, r):
    return np.hypot(X - cx, Y - cy) - r


def ring(cx, cy, r, w):
    return np.abs(np.hypot(X - cx, Y - cy) - r) - w / 2


def ellipse_ring(cx, cy, rx, ry, w):
    # an approximation that is good enough for a thin ring: scale space so the ellipse becomes a circle
    d = np.hypot((X - cx) / rx, (Y - cy) / ry)
    return (np.abs(d - 1.0) * min(rx, ry)) - w / 2


def arc_ring(cx, cy, r, w, a0, a1):
    # a piece of a ring between two angles in degrees (0 = right, 90 = down on screen)
    angle = np.degrees(np.arctan2(Y - cy, X - cx)) % 360
    return np.where((angle >= a0) & (angle <= a1), ring(cx, cy, r, w), 1e3)


def capsule(ax, ay, bx, by, w):
    px, py = X - ax, Y - ay
    dx, dy = bx - ax, by - ay
    t = np.clip((px * dx + py * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(px - t * dx, py - t * dy) - w / 2


def polygon(points):
    d = np.full(X.shape, 1e9)
    inside = np.zeros(X.shape, dtype=bool)
    n = len(points)
    for i in range(n):
        ax, ay = points[i]
        bx, by = points[(i + 1) % n]
        px, py = X - ax, Y - ay
        ex, ey = bx - ax, by - ay
        t = np.clip((px * ex + py * ey) / (ex * ex + ey * ey), 0, 1)
        d = np.minimum(d, np.hypot(px - t * ex, py - t * ey))
        crosses = ((ay > Y) != (by > Y)) & (X < (bx - ax) * (Y - ay) / (by - ay + 1e-12) + ax)
        inside ^= crosses
    return np.where(inside, -d, d)


def union(*ds):
    return np.minimum.reduce(ds)


def subtract(a, b):
    return np.maximum(a, -b)


def rotate_points(points, cx, cy, angle_deg, scale):
    a = np.radians(angle_deg)
    c, s = np.cos(a), np.sin(a)
    return [(cx + scale * (x * c - y * s), cy + scale * (x * s + y * c)) for x, y in points]


def axe(cx, cy, angle_deg, scale):
    # Drawn in the axe's own space (handle along y from -1 at the butt to +1 at the top, blade on the +x side near the top, a small spike on the other side),
    # then mapped back. y points down on screen, so angle 0 has the head at the bottom and 180 has it up.
    a = np.radians(angle_deg)
    c, s = np.cos(a), np.sin(a)
    px, py = (X - cx) / scale, (Y - cy) / scale
    lx, ly = px * c + py * s, -px * s + py * c

    def local_capsule(ax, ay, bx, by, w):
        qx, qy = lx - ax, ly - ay
        dx, dy = bx - ax, by - ay
        t = np.clip((qx * dx + qy * dy) / (dx * dx + dy * dy), 0, 1)
        return np.hypot(qx - t * dx, qy - t * dy) - w / 2

    handle = local_capsule(0, -1.0, 0, 1.0, 0.2)
    blade = np.maximum(np.hypot(lx - 0.02, ly - 0.66) - 0.62, 0.08 - lx)
    spike = np.maximum(np.abs(ly - 0.8) * 0.9 + np.maximum(0, -lx) * 1.0 - 0.22, lx - 0.08)
    return union(handle, blade, spike) * scale


def arrow_head(tip_x, tip_y, angle_deg, size):
    return polygon(rotate_points([(0, 0), (-1.0, 0.55), (-0.7, 0), (-1.0, -0.55)], tip_x, tip_y, angle_deg, size))


# --- rendering ---

def coverage(d):
    return np.clip(0.5 - d, 0.0, 1.0)


def gaussian_blur(img, sigma):
    radius = int(sigma * 3)
    k = np.exp(-0.5 * (np.arange(-radius, radius + 1) / sigma) ** 2)
    k /= k.sum()
    out = np.apply_along_axis(lambda row: np.convolve(row, k, mode="same"), 1, img)
    return np.apply_along_axis(lambda col: np.convolve(col, k, mode="same"), 0, out)


class Icon:
    def __init__(self, hue):
        self.hue = np.array(hue, dtype=np.float64) / 255.0
        self.glow = np.zeros((SIZE, SIZE))
        self.core = np.zeros((SIZE, SIZE))
        self.accent = np.zeros((SIZE, SIZE))

    def add(self, d, strength=1.0, accent=False):
        cov = coverage(d) * strength
        self.glow = np.maximum(self.glow, cov)
        if accent:
            self.accent = np.maximum(self.accent, cov)
        else:
            self.core = np.maximum(self.core, cov)

    def render(self):
        # dark tile: a deep tint of the hue, brighter in the middle, darker at the edges
        r = np.hypot(X - SIZE / 2, Y - SIZE / 2) / (SIZE / 2)
        base = self.hue * 0.10 + 0.02
        tile = base[None, None, :] * (1.15 - 0.55 * np.clip(r, 0, 1.2)[:, :, None])
        tile += np.array([0.0, 0.01, 0.03])[None, None, :]
        glow = gaussian_blur(self.glow, 5.5)[:, :, None] * self.hue[None, None, :] * 0.85
        wide = gaussian_blur(self.glow, 12)[:, :, None] * self.hue[None, None, :] * 0.35
        body = self.hue * 0.55 + 0.45
        accent = self.hue * 0.25 + 0.75
        rgb = tile + glow + wide
        rgb = rgb * (1 - self.core[:, :, None]) + body[None, None, :] * self.core[:, :, None]
        rgb = rgb * (1 - self.accent[:, :, None]) + accent[None, None, :] * self.accent[:, :, None]
        # a thin lit border so the icons read as tiles on the bar
        edge = np.minimum.reduce([X, Y, SIZE - X, SIZE - Y])
        rgb += (self.hue * 0.55)[None, None, :] * (np.clip(1.0 - np.abs(edge - 2.0) / 1.2, 0, 1) * 0.5)[:, :, None]
        return (np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8)


def save_png(path, rgb):
    h, w, _ = rgb.shape
    raw = b"".join(b"\x00" + rgb[y].tobytes() for y in range(h))

    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)
