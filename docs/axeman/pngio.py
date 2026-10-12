"""Tiny PNG reader for the icon scripts (8 bit, non interlaced, RGB or RGBA), so they need nothing but numpy."""
import struct
import zlib

import numpy as np


def read_png(path):
    data = open(path, "rb").read()
    pos = 8
    idat = b""
    while pos < len(data):
        length, tag = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + length]
        if tag == b"IHDR":
            w, h, depth, ctype, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert depth == 8 and interlace == 0 and ctype in (2, 6)
        elif tag == b"IDAT":
            idat += body
        pos += 12 + length
    channels = 4 if ctype == 6 else 3
    raw = zlib.decompress(idat)
    stride = w * channels
    out = np.zeros((h, stride), np.uint8)
    prev = np.zeros(stride, np.int32)
    i = 0
    for y in range(h):
        ft = raw[i]
        line = np.frombuffer(raw[i + 1:i + 1 + stride], np.uint8).astype(np.int32)
        i += 1 + stride
        if ft == 0:
            cur = line
        elif ft == 1:
            cur = line.copy()
            for x in range(channels, stride):
                cur[x] = (cur[x] + cur[x - channels]) & 255
        elif ft == 2:
            cur = (line + prev) & 255
        elif ft == 3:
            cur = line.copy()
            for x in range(stride):
                left = cur[x - channels] if x >= channels else 0
                cur[x] = (cur[x] + ((left + prev[x]) >> 1)) & 255
        else:
            cur = line.copy()
            for x in range(stride):
                a = cur[x - channels] if x >= channels else 0
                b = prev[x]
                c = prev[x - channels] if x >= channels else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                cur[x] = (cur[x] + pr) & 255
        out[y] = cur
        prev = cur
    img = out.reshape(h, w, channels)
    if channels == 3:
        img = np.dstack([img, np.full((h, w, 1), 255, np.uint8)])
    return img
