"""Turn a generated character image into a master-palette sprite (art bible 4.13, 5.4, 5.9, 8.11).

Steps: key to binary alpha, drop the generator's own outline, place on the
canvas with the feet on the ground line, map body colour to a three-step INK
ramp, then add the 1 px outline and the 1 px key-side rim by rule.

  hero  : body INK-2/3/4, skin Candle-D, eyes INK-0, rim Candle, outline INK-0
  enemy : body INK-1/2/3, no warm pixel, sockets INK-0, rim Fog, outline INK-4

Run as a module:  python -I tools/art/process_character.py IN.png OUT.png hero|enemy CANVAS
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

GROUND_MARGIN = 4  # pivot is 4 px above the canvas bottom (art bible 8.4)


def luminance(rgb: np.ndarray) -> np.ndarray:
    c = rgb.astype(np.float64) / 255.0
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]


def nb4(m: np.ndarray) -> np.ndarray:
    o = np.zeros_like(m)
    o[1:] |= m[:-1]
    o[:-1] |= m[1:]
    o[:, 1:] |= m[:, :-1]
    o[:, :-1] |= m[:, 1:]
    return o


def erode4(m: np.ndarray) -> np.ndarray:
    return m & ~nb4(~m)


def count8(m: np.ndarray) -> np.ndarray:
    n = np.zeros(m.shape, int)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dy or dx:
                n += np.roll(np.roll(m, dy, 0), dx, 1)
    return n


def process(src: Path, kind: str, canvas: int, dark_cut: float = 0.14, skin_gap: int = 18) -> np.ndarray:
    im = np.array(Image.open(src).convert("RGBA"))
    alpha = im[..., 3] >= 128
    rgb = im[..., :3]
    interior = erode4(alpha)  # the outermost ring is the generator's outline: replaced below
    ys, xs = np.where(interior)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    sub_rgb = rgb[y0:y1 + 1, x0:x1 + 1]
    sub = interior[y0:y1 + 1, x0:x1 + 1]
    h, w = sub.shape
    out = np.zeros((canvas, canvas, 4), np.uint8)

    lum = luminance(sub_rgb)
    r, g, b = (sub_rgb[..., i].astype(int) for i in range(3))
    # Only the head is warm (art bible 5.2.2: the face is the one Candle-D patch); hands and boots join the ramp.
    head = np.arange(h)[:, None] < h * 0.32
    skin = sub & (r - b > skin_gap) & (r > 90) & head if kind == "hero" else np.zeros_like(sub)
    eye = np.zeros_like(sub)
    body = sub & ~skin
    dark = body & (lum < dark_cut)
    if kind == "hero":
        eye = dark & (count8(skin) >= 3)
    else:
        eye = dark & (count8(body & (lum > dark_cut)) >= 0) & (lum < dark_cut * 0.75)
    ramp_px = body & ~eye
    ramp = ["INK-2", "INK-3", "INK-4"] if kind == "hero" else ["INK-1", "INK-2", "INK-3"]
    lo, hi = np.percentile(lum[ramp_px], [38, 78])
    idx = np.where(lum < lo, 0, np.where(lum < hi, 1, 2))

    colour = np.zeros((h, w, 3), np.uint8)
    for k, name in enumerate(ramp):
        colour[ramp_px & (idx == k)] = P.rgb(name)
    colour[skin] = P.rgb("CANDLE-D")
    colour[eye] = P.rgb("INK-0")

    # place: horizontally centred, feet on the ground line
    ox = (canvas - w) // 2
    oy = canvas - GROUND_MARGIN - h
    sil = np.zeros((canvas, canvas), bool)
    body_img = np.zeros((canvas, canvas, 3), np.uint8)
    sil[oy:oy + h, ox:ox + w] = sub
    body_img[oy:oy + h, ox:ox + w] = colour

    # rim: inside edge facing the key light (top and left), 1 px
    up = np.zeros_like(sil); up[1:] = sil[:-1]
    left = np.zeros_like(sil); left[:, 1:] = sil[:, :-1]
    rim = sil & (~up | ~left)
    body_img[rim & ~(body_img == np.array(P.rgb("INK-0"))).all(-1)] = P.rgb("CANDLE" if kind == "hero" else "FOG")

    outline = nb4(sil) & ~sil
    body_img[outline] = P.rgb("INK-0" if kind == "hero" else "INK-4")
    out[..., :3] = body_img
    out[..., 3] = np.where(sil | outline, 255, 0)
    return out


def main() -> int:
    src, dst, kind, canvas = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3], int(sys.argv[4])
    Image.fromarray(process(src, kind, canvas), "RGBA").save(dst)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
