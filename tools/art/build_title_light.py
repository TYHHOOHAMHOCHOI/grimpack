"""Build the lantern-pool "breathing" frames for the title backdrop (art bible 2.2 rule 2, 4.13).

The pool is the one lit object in the menu scene. It breathes by growing and
shrinking its warm edge in whole-pixel steps (stepped shells), never by alpha
or blur, so every frame uses only master-palette colours.

Frames (210 x 160 overlays placed at x=280, y=200 over env_title_bg_640x360.png):
  0 shrink 1 px   1 base   2 grow 1 px   3 grow 2 px
Loop order is 0 1 2 3 2 1 (see LanternPoolBreathing in the Unity project).

Rules: only INK-3 / INK-4 stone can warm up, so mortar and shadow (INK-0..2)
never light; a shrinking edge takes the colour of its commonest cool neighbour;
CANDLE highlights never move.

Run:  python -I tools/art/build_title_light.py
Needs: Assets/_Project/Art/Environment/env_title_bg_640x360.png
Writes: Assets/_Project/Art/Environment/env_title-pool-{0..3}_prop_210x160.png
        production/qa/evidence/title_pool_frames_2026-11-15.png
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

ENV = ROOT / "Assets/_Project/Art/Environment"
SRC = ENV / "env_title_bg_640x360.png"
EVIDENCE = ROOT / "production/qa/evidence"
BOX = (280, 200, 490, 360)  # x0, y0, x1, y1 of the overlay in backdrop pixels


def colour(name: str) -> np.ndarray:
    return np.array(P.rgb(name), np.uint8)


def neighbours4(mask: np.ndarray) -> np.ndarray:
    out = np.zeros_like(mask)
    out[1:] |= mask[:-1]
    out[:-1] |= mask[1:]
    out[:, 1:] |= mask[:, :-1]
    out[:, :-1] |= mask[:, 1:]
    return out


def make_frames(base: np.ndarray) -> list[np.ndarray]:
    h, w = base.shape[:2]
    is_ = lambda img, n: (img == colour(n)).all(-1)  # noqa: E731
    roi = np.zeros((h, w), bool)
    x0, y0, x1, y1 = BOX
    roi[y0:y1, x0:x1] = True
    warm = (is_(base, "CANDLE") | is_(base, "CANDLE-D")) & roi

    def grow(img: np.ndarray, mask: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
        stone = is_(img, "INK-3") | is_(img, "INK-4")
        ring = neighbours4(mask) & ~mask & roi & stone
        out = img.copy()
        out[ring] = colour("CANDLE-D")
        return out, mask | ring

    def shrink(img: np.ndarray, mask: np.ndarray) -> np.ndarray:
        edge = mask & neighbours4(~mask) & is_(img, "CANDLE-D")
        out = img.copy()
        for y, x in zip(*np.where(edge)):
            cols = []
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < h and 0 <= xx < w and not mask[yy, xx]:
                    cols.append(tuple(img[yy, xx]))
            if cols:
                out[y, x] = max(set(cols), key=cols.count)
        return out

    g1, mask1 = grow(base, warm)
    g2, _ = grow(g1, mask1)
    return [shrink(base, warm), base, g1, g2]


def main() -> int:
    base = np.array(Image.open(SRC).convert("RGB"))
    frames = make_frames(base)
    x0, y0, x1, y1 = BOX
    crops = [f[y0:y1, x0:x1] for f in frames]
    for i, c in enumerate(crops):
        Image.fromarray(c).convert("RGBA").save(ENV / f"env_title-pool-{i}_prop_{x1 - x0}x{y1 - y0}.png")
    sheet = Image.new("RGB", ((x1 - x0) * 2 + 4, (y1 - y0) * 2 + 4), P.rgb("INK-0"))
    for i, c in enumerate(crops):
        sheet.paste(Image.fromarray(c), ((i % 2) * (x1 - x0 + 4), (i // 2) * (y1 - y0 + 4)))
    sheet.save(EVIDENCE / "title_pool_frames_2026-11-15.png")
    pal = {tuple(P.rgb(n)) for n in P.PALETTE}
    for i, c in enumerate(crops):
        off = {tuple(p) for p in np.unique(c.reshape(-1, 3), axis=0)} - pal
        print(f"frame {i}: changed vs base {int((frames[i] != base).any(-1).sum())} px, off-palette colours {len(off)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
