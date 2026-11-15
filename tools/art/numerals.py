"""Numeral face: digits and a few symbols in authored cuts (art bible 7.7, 8.9).

Numerals grow by changing cut, never by scaling. Each cut is re-drawn from the same
skeleton at its own size and stroke width, so the look stays one family:

  cut   digit h  stroke  outline (baked, INK-0)   used for
    8      7       1       none                    HP, tab magnitude, chips, prices
   12     10       1       1 px                    smallest damage, stat lines
   16     14       2       1 px                    damage step
   24     21       3       2 px                    damage step, result panel
   32     28       4       2 px                    damage step
   48     42       6       3 px                    payoff peak only

Colour is Bone; damage taken carries a Blood minus, drawn by the caller.
Output per cut: one atlas PNG and a JSON with each glyph's rect and advance.

Run:  python -I tools/art/numerals.py
Writes Assets/_Project/Art/Fonts/grimpack-numeral/font_grimpack-numeral_num_{cut}.png and .json
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

OUT = ROOT / "Assets/_Project/Art/Fonts/grimpack-numeral"
CUTS = {8: (7, 1, 0), 12: (10, 1, 1), 16: (14, 2, 1), 24: (21, 3, 2), 32: (28, 4, 2), 48: (42, 6, 3)}
ORDER = "0123456789+-x/.,%KM"

# skeletons in 0..1 (x right, y down); each glyph is a list of polylines
SK: dict[str, list[list[tuple[float, float]]]] = {
    "0": [[(0, .12), (.15, 0), (.85, 0), (1, .12), (1, .88), (.85, 1), (.15, 1), (0, .88), (0, .12)]],
    "1": [[(.15, .2), (.55, 0), (.55, 1)], [(.1, 1), (1, 1)]],
    "2": [[(0, .15), (.2, 0), (.8, 0), (1, .15), (1, .35), (0, 1), (1, 1)]],
    "3": [[(0, 0), (1, 0), (.45, .45), (.85, .45), (1, .6), (1, .88), (.85, 1), (.15, 1), (0, .88)]],
    "4": [[(.78, 1), (.78, 0), (0, .7), (1, .7)]],
    "5": [[(1, 0), (.1, 0), (0, .45), (.8, .42), (1, .58), (1, .88), (.85, 1), (.15, 1), (0, .88)]],
    "6": [[(.9, .05), (.5, 0), (0, .3), (0, .88), (.15, 1), (.85, 1), (1, .88), (1, .62), (.85, .5), (.15, .5), (0, .62)]],
    "7": [[(0, 0), (1, 0), (.35, 1)]],
    "8": [[(.15, 0), (.85, 0), (1, .12), (1, .35), (.85, .47), (.15, .47), (0, .35), (0, .12), (.15, 0)],
          [(.15, .47), (.85, .47), (1, .6), (1, .88), (.85, 1), (.15, 1), (0, .88), (0, .6), (.15, .47)]],
    "9": [[(.1, .95), (.5, 1), (1, .7), (1, .12), (.85, 0), (.15, 0), (0, .12), (0, .38), (.15, .5), (.85, .5), (1, .38)]],
    "+": [[(.5, .25), (.5, .75)], [(.1, .5), (.9, .5)]],
    "-": [[(.1, .5), (.9, .5)]],
    "x": [[(.1, .3), (.9, .8)], [(.9, .3), (.1, .8)]],
    "/": [[(.9, 0), (.1, 1)]],
    ".": [[(.4, .92), (.6, .92)]],
    ",": [[(.55, .85), (.4, 1.1)]],
    "%": [[(.9, 0), (.1, 1)], [(.05, .05), (.3, .05), (.3, .3), (.05, .3), (.05, .05)], [(.7, .7), (.95, .7), (.95, .95), (.7, .95), (.7, .7)]],
    "K": [[(0, 0), (0, 1)], [(.9, 0), (0, .55)], [(.2, .45), (.95, 1)]],
    "M": [[(0, 1), (0, 0), (.5, .6), (1, 0), (1, 1)]],
}
# Cut 8 is hand drawn (4 wide, 7 tall): a skeleton at that size is too coarse to read.
HAND8 = {
    "0": ".##. #..# #..# #..# #..# #..# .##.", "1": "..#. .##. ..#. ..#. ..#. ..#. .###",
    "2": ".##. #..# ...# ..#. .#.. #... ####", "3": ".##. #..# ...# ..#. ...# #..# .##.",
    "4": "...# ..## .#.# #..# #### ...# ...#", "5": "#### #... ###. ...# ...# #..# .##.",
    "6": ".##. #... ###. #..# #..# #..# .##.", "7": "#### ...# ..#. ..#. .#.. .#.. .#..",
    "8": ".##. #..# .##. #..# #..# #..# .##.", "9": ".##. #..# #..# .### ...# ..#. .#..",
    "+": ".... .#.. ###. .#.. .... .... ....", "-": ".... .... ###. .... .... .... ....",
    "x": ".... #..# .##. .##. #..# .... ....", "/": "...# ...# ..#. ..#. .#.. #... #...",
    ".": ".... .... .... .... .... .... .#..", ",": ".... .... .... .... .... .#.. #...",
    "%": "#..# ...# ..#. ..#. .#.. #... #..#", "K": "#..# #.#. ##.. ##.. #.#. #..# ....",
    "M": "#...# ##.## #.#.# #...# #...# #...# .....",
}

WIDTH = {"0": .55, "1": .45, ".": .15, ",": .15, "-": .45, "+": .55, "x": .55, "/": .45, "%": .8, "K": .55, "M": .8}


def line(m: np.ndarray, p0: tuple[int, int], p1: tuple[int, int], t: int) -> None:
    n = max(abs(p1[0] - p0[0]), abs(p1[1] - p0[1]), 1)
    for i in range(n + 1):
        x = round(p0[0] + (p1[0] - p0[0]) * i / n)
        y = round(p0[1] + (p1[1] - p0[1]) * i / n)
        m[y:y + t, x:x + t] = True


def glyph_mask(ch: str, h: int, t: int) -> np.ndarray:
    if h == 7:
        rows = HAND8[ch].split()
        return np.array([[c == "#" for c in r] for r in rows], bool)
    w = max(3, round(h * WIDTH.get(ch, .55)))
    m = np.zeros((h + 4 * t, w + 2 * t), bool)
    for poly in SK[ch]:
        pts = [(round(x * (w - t)), round(y * (h - t))) for x, y in poly]
        for a, b in zip(pts, pts[1:]):
            line(m, a, b, t)
    ys, xs = np.where(m)
    return m[:ys.max() + 1, :xs.max() + 1] if len(xs) else m


def outlined(m: np.ndarray, o: int) -> np.ndarray:
    h, w = m.shape
    pad = np.zeros((h + 2 * o, w + 2 * o), bool)
    pad[o:o + h, o:o + w] = m
    grown = pad.copy()
    for _ in range(o):
        g = grown.copy()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                g |= np.roll(np.roll(grown, dy, 0), dx, 1)
        grown = g
    img = np.zeros((*pad.shape, 4), np.uint8)
    img[grown & ~pad] = (*P.rgb("INK-0"), 255)
    img[pad] = (*P.rgb("BONE"), 255)
    return img


def build_cut(cut: int) -> tuple[np.ndarray, dict]:
    h, t, o = CUTS[cut]
    glyphs = {}
    for ch in ORDER:
        m = glyph_mask(ch, h, t)
        glyphs[ch] = outlined(m, o)
    gap = max(1, t)
    cell_h = max(g.shape[0] for g in glyphs.values()) + 2
    x = 0
    meta = {}
    total_w = sum(g.shape[1] + gap + 1 for g in glyphs.values())
    atlas = np.zeros((cell_h, total_w, 4), np.uint8)
    for ch, g in glyphs.items():
        atlas[:g.shape[0], x:x + g.shape[1]] = g
        meta[ch] = {"ch": ch, "x": x, "y": 0, "w": int(g.shape[1]), "h": int(g.shape[0]), "advance": int(g.shape[1] - 2 * o + gap)}
        x += g.shape[1] + gap + 1
    return atlas, {"cut": cut, "digitHeight": h, "stroke": t, "outline": o, "lineHeight": int(cell_h), "glyphs": list(meta.values())}


def render(text: str, cut: int) -> np.ndarray:
    """Compose text from a cut's atlas (RGBA, tightly cropped). For mock-ups and tests."""
    meta = json.loads((OUT / f"font_grimpack-numeral_num_{cut}.json").read_text(encoding="utf-8"))
    atlas = np.array(Image.open(OUT / f"font_grimpack-numeral_num_{cut}.png").convert("RGBA"))
    g = {r["ch"]: r for r in meta["glyphs"]}
    o = meta["outline"]
    width = sum(g[c]["advance"] for c in text) + 2 * o
    out = np.zeros((meta["lineHeight"], width, 4), np.uint8)
    x = 0
    for c in text:
        r = g[c]
        piece = atlas[r["y"]:r["y"] + r["h"], r["x"]:r["x"] + r["w"]]
        region = out[:r["h"], x:x + r["w"]]
        mask = piece[..., 3] > 0
        region[mask] = piece[mask]
        x += r["advance"]
    return out


COLOURWAYS = {"num-fog": "FOG", "num-fog-hi": "FOG-HI", "num-blood": "BLOOD", "num-ink-4": "INK-4"}


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    for cut in CUTS:
        atlas, meta = build_cut(cut)
        Image.fromarray(atlas, "RGBA").save(OUT / f"font_grimpack-numeral_num_{cut}.png")
        bone = (atlas[..., :3] == np.array(P.rgb("BONE"))).all(-1) & (atlas[..., 3] > 0)
        for variant, colour in COLOURWAYS.items():  # recolours of the same mask, so no tint (and no off-palette product) at run time
            recoloured = atlas.copy()
            recoloured[bone, :3] = P.rgb(colour)
            Image.fromarray(recoloured, "RGBA").save(OUT / f"font_grimpack-numeral_{variant}_{cut}.png")
        (OUT / f"font_grimpack-numeral_num_{cut}.json").write_text(json.dumps(meta, indent=1), encoding="utf-8")
    print(f"wrote {len(CUTS)} cuts to {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
