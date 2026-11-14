"""Quantise an image to the master palette (art bible 4.13 rule 1, 8.11 step 4).

Nearest colour in OKLab, no dithering. Prints observations (off-palette share,
colour usage, APL); it does not pass or fail the image.

Run:  python -I tools/art/quantise.py IN.png OUT.png [COLOUR ...]

With no COLOUR arguments the title-scene subset is used: the ink ramp, Fog,
Bone and Candle. Pass names from palette.py to widen or narrow it.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

# Dark, ink-heavy scenes (menus): no Moss, because warm light pools on stone
# otherwise snap to green.
SCENE_ALLOW = ["INK-0", "INK-1", "INK-2", "INK-3", "INK-4", "FOG", "FOG-HI", "BONE", "CANDLE", "CANDLE-D"]


def _lin(c: np.ndarray) -> np.ndarray:
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def oklab(rgb: np.ndarray) -> np.ndarray:
    l = _lin(rgb.astype(np.float64))
    r, g, b = l[..., 0], l[..., 1], l[..., 2]
    lc = np.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b)
    mc = np.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b)
    sc = np.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b)
    return np.stack(
        [
            0.2104542553 * lc + 0.7936177850 * mc - 0.0040720468 * sc,
            1.9779984951 * lc - 2.4285922050 * mc + 0.4505937099 * sc,
            0.0259040371 * lc + 0.7827717662 * mc - 0.8086757660 * sc,
        ],
        -1,
    )


def quantise(src: np.ndarray, names: list[str]) -> tuple[np.ndarray, np.ndarray]:
    """Return (indexed RGB image, per-pixel OKLab distance to the chosen colour)."""
    pal = np.array([P.rgb(n) for n in names], np.uint8)
    d = ((oklab(src)[:, :, None, :] - oklab(pal)[None, None, :, :]) ** 2).sum(-1)
    idx = d.argmin(-1)  # ties go to the lower index
    return pal[idx], np.sqrt(d.min(-1)), idx


def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    names = sys.argv[3:] or SCENE_ALLOW
    src = np.array(Image.open(sys.argv[1]).convert("RGB"))
    out, dist, idx = quantise(src, names)
    Image.fromarray(out).save(sys.argv[2])
    print(f"off-palette intent (>0.10 OKLab): {(dist > 0.10).mean() * 100:.1f}% (art bible 8.11 reject limit: 15%)")
    cnt = np.bincount(idx.ravel(), minlength=len(names))
    for n, c in zip(names, cnt):
        print(f"  {n:9s} {c / idx.size * 100:5.1f}%")
    lin = _lin(out.astype(float))
    y = 0.2126 * lin[..., 0] + 0.7152 * lin[..., 1] + 0.0722 * lin[..., 2]
    print(f"APL {y.mean():.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
