"""Keyword, stat, status, intent and UI glyphs (art bible 7.8, 8.7).

One verb, one drawing, several skins. Each glyph has a 12 px master (a 10 x 10
drawing plus a 1 px margin or outline) and an 8 px reduction (6 x 6 plus margin).
Skins are recolours of the same mask:

  bone    fill Bone, 1 px shade Fog on the lower and right edges, 1 px INK-0 outline
  blood   heart only: fill Blood, shade Blood-D (HP, 4.6)
  fog     shield only: fill Fog, shade INK-4 (armour, 4.6)
  fog-hi  flat Fog-hi, no shade, no outline (intent glyph on the pennant tab, bare when it lifts off)

Reserved-shape audit (3.2, 7.8): no bare circle, bare diamond, chevron, pennant,
toothed row, hanging tag or lattice. The coin is the one circle (currency).

Run:  python -I tools/art/ui_glyphs.py     (writes Assets/_Project/Art/UI/ui_glyph-*.png)
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

UI = ROOT / "Assets/_Project/Art/UI"


def raster(strokes: list[tuple], size: int) -> np.ndarray:
    """Draw line strokes (x0, y0, x1, y1, thickness) on a size x size grid; coordinates are in 0..size-1."""
    m = np.zeros((size, size), bool)
    for x0, y0, x1, y1, t in strokes:
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1):
            x = round(x0 + (x1 - x0) * i / n)
            y = round(y0 + (y1 - y0) * i / n)
            m[max(0, y):min(size, y + t), max(0, x):min(size, x + t)] = True
    return m


def ascii_mask(rows: list[str], size: int) -> np.ndarray:
    m = np.zeros((size, size), bool)
    h = len(rows)
    w = max(len(r) for r in rows)
    ox, oy = (size - w) // 2, (size - h) // 2
    for j, r in enumerate(rows):
        for i, c in enumerate(r):
            if c == "#":
                m[oy + j, ox + i] = True
    return m


def scaled(strokes: list[tuple], size: int) -> list[tuple]:
    """Reduce 10 px strokes to a smaller grid, keeping thickness 1 for the small size."""
    k = (size - 1) / 9
    return [(round(a * k), round(b * k), round(c * k), round(d * k), 1 if size < 10 else t) for a, b, c, d, t in strokes]


# ---- line glyphs: (x0, y0, x1, y1, thickness) on a 10 x 10 grid
LINES: dict[str, list[tuple]] = {
    "attack": [(8, 0, 3, 5, 2), (1, 2, 6, 7, 1), (3, 6, 0, 9, 1)],                # blade, guard, grip
    "multi": [(5, 0, 1, 9, 2), (9, 0, 6, 9, 2)],                                  # two claw marks
    "sweep": [(0, 8, 2, 4, 1), (2, 4, 4, 2, 1), (4, 2, 6, 2, 1), (6, 2, 8, 4, 1), (8, 4, 9, 8, 1), (3, 4, 6, 4, 1)],
    "windup": [(1, 0, 8, 0, 1), (1, 9, 8, 9, 1), (1, 1, 8, 8, 1), (8, 1, 1, 8, 1)],  # hourglass: coiling, no damage yet
    "damage": [(4, 0, 4, 9, 2), (0, 4, 9, 4, 2), (1, 1, 8, 8, 1), (8, 1, 1, 8, 1)],  # burst
    "menu": [(0, 1, 9, 1, 2), (0, 4, 9, 4, 2), (0, 7, 9, 7, 2)],
}
# ---- bitmap glyphs: 10 px drawing and 6 px reduction
ASCII10: dict[str, list[str]] = {
    "heart": [".###..###.", "##########", "##########", "##########", ".########.", "..######..", "...####...", "....##...."],
    "shield": ["########", "########", "########", "########", ".######.", ".######.", "..####..", "...##..."],
    "speed": ["....###", "...###.", "..###..", ".######", "....##.", "...##..", "..##...", ".##...."],
    "droplet": ["...##...", "...##...", "..####..", ".######.", "########", "########", "########", ".######.", "..####.."],
    "seal": ["..####..", "...##...", "...##...", "########", "########", "#..##..#", "########", "########"],
}
ASCII6: dict[str, list[str]] = {
    "heart": [".#.##.", "######", "######", ".####.", "..##.."],
    "shield": ["#####", "#####", "#####", ".###.", "..#.."],
    "speed": ["...##", "..##.", ".####", "..##.", ".##.."],
    "droplet": ["..#..", ".###.", "#####", "#####", ".###."],
    "seal": [".###.", "..#..", "#####", "#.#.#", "#####"],
}
INTENT = ["attack", "multi", "sweep", "windup"]
ALL = list(LINES) + list(ASCII10)


def mask(name: str, size: int) -> np.ndarray:
    """Return the drawing mask on a size x size grid (size 10 or 6)."""
    if name in ASCII10:
        return ascii_mask(ASCII10[name] if size == 10 else ASCII6[name], size)
    return raster(LINES[name] if size == 10 else scaled(LINES[name], size), size)


def skin(m: np.ndarray, kind: str) -> np.ndarray:
    """Place the mask on a canvas with a 1 px margin and colour it. Returns RGBA."""
    h, w = m.shape
    out = np.zeros((h + 2, w + 2, 4), np.uint8)
    inner = np.zeros((h + 2, w + 2), bool)
    inner[1:-1, 1:-1] = m
    if kind == "fog-hi":
        out[inner] = (*P.rgb("FOG-HI"), 255)
        return out
    grown = inner.copy()
    for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        grown |= np.roll(np.roll(inner, dy, 0), dx, 1)
    out[grown & ~inner] = (*P.rgb("INK-0"), 255)
    shade = inner & (~np.roll(inner, -1, 0) | ~np.roll(inner, -1, 1))
    fill, edge = {"bone": ("BONE", "FOG"), "blood": ("BLOOD", "BLOOD-D"), "fog": ("FOG", "INK-4")}[kind]
    out[inner] = (*P.rgb(fill), 255)
    out[shade] = (*P.rgb(edge), 255)
    return out


def coin(size: int = 12) -> np.ndarray:
    out = np.zeros((size, size, 4), np.uint8)
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = ((x - c) ** 2 + (y - c) ** 2) ** 0.5
            if d <= size / 2:
                out[y, x] = (*P.rgb("INK-0"), 255)
            if d <= size / 2 - 1:
                out[y, x] = (*P.rgb("CANDLE"), 255)
            if size - 1 > d >= 0 and d <= size / 2 - 3:
                out[y, x] = (*P.rgb("CANDLE-D"), 255)
    return out


def chevrons() -> dict[str, np.ndarray]:
    """Turn marker (7 x 4, Candle or Fog-hi) and buff / debuff corner marks (6 x 4, Bone)."""
    def make(rows: list[str], colour: str) -> np.ndarray:
        a = np.zeros((len(rows), len(rows[0]), 4), np.uint8)
        for j, r in enumerate(rows):
            for i, c in enumerate(r):
                if c == "#":
                    a[j, i] = (*P.rgb(colour), 255)
        return a
    down = ["#.....#", ".#...#.", "..#.#..", "...#..."]
    return {
        "ui_chevron-turn_hero_7x4": make(down, "CANDLE"),
        "ui_chevron-turn_foe_7x4": make(down, "FOG-HI"),
        "ui_chevron-buff_up_6x4": make(["..##..", ".#..#.", "#....#", "......"], "BONE"),
        "ui_chevron-debuff_down_6x4": make(["......", "#....#", ".#..#.", "..##.."], "BONE"),
    }


def keycap(label_w: int = 12) -> np.ndarray:
    """Plain square-cornered keycap (7.8): INK-2 bed, INK-4 frame, 12 px."""
    a = np.zeros((12, label_w, 4), np.uint8)
    a[:, :] = (*P.rgb("INK-4"), 255)
    a[1:-1, 1:-1] = (*P.rgb("INK-2"), 255)
    a[-2, 1:-1] = (*P.rgb("INK-1"), 255)
    return a


def save(a: np.ndarray, name: str) -> None:
    Image.fromarray(a, "RGBA").save(UI / name)


def main() -> int:
    UI.mkdir(parents=True, exist_ok=True)
    written = 0
    for g in ALL:
        for size, px in ((10, 12), (6, 8)):
            save(skin(mask(g, size), "bone"), f"ui_glyph-{g}_bone_{px}x{px}.png"); written += 1
            for extra_skin, owner in (("blood", "heart"), ("fog", "shield")):
                if g == owner:
                    save(skin(mask(g, size), extra_skin), f"ui_glyph-{g}_{extra_skin}_{px}x{px}.png"); written += 1
            if g in INTENT:
                save(skin(mask(g, size), "fog-hi"), f"ui_glyph-{g}_fog-hi_{px}x{px}.png"); written += 1
    save(coin(12), "ui_glyph-coin_default_12x12.png"); written += 1
    for name, a in chevrons().items():
        save(a, name + ".png"); written += 1
    save(keycap(12), "ui_keycap_default_12x12.png"); written += 1
    print(f"wrote {written} files to {UI}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
