"""Item plates for the six tiers (art bible 3.4, 4.4, 5.10.2, 8.5).

A plate is the item's footprint as one contour: a 1 px tier-edge outline, a lit
1 px tier frame inside it, and an INK-2 cell bed. Tier is read from form first:

  White  square corners
  Green  one chamfer (anchor corner, top-left)
  Blue   two chamfers, a diagonal pair (anchor and last convex corner)
  Purple all convex corners chamfered
  Gold   all chamfered plus a short inner bracket line at each convex corner
  Red    all chamfered plus a serrated top edge (teeth Red-hi, 2 x 2 on a 4 px pitch, hanging inward)

States: rest (frame at tier base), hi (frame at tier highlight; pulse and echo).
1x1 only: lift (1 px INK-0 shadow lower-right, canvas 22 x 22), lifthi, liftdim (avatar pop-in and exit).
The curse tag (Gold and Red) is a separate hanging piece and is not drawn here.

Run:  python -I tools/art/plates.py     (writes Assets/_Project/Art/Items/Plates/plate_*.png)
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

OUT = ROOT / "Assets/_Project/Art/Items/Plates"
EVIDENCE = ROOT / "production/qa/evidence"
C = 20
CH = 3  # chamfer size in px
TIERS = ["white", "green", "blue", "purple", "gold", "red"]
SHAPES = {
    "1x1": [(0, 0)], "1x2": [(0, 0), (0, 1)], "2x1": [(0, 0), (1, 0)], "2x2": [(0, 0), (1, 0), (0, 1), (1, 1)],
    "l0": [(0, 0), (1, 0), (0, 1)], "l1": [(0, 0), (1, 0), (1, 1)], "l2": [(1, 0), (0, 1), (1, 1)], "l3": [(0, 0), (0, 1), (1, 1)],
    # footprints of the MVP item catalog (tools/art/item_catalog.py): long, large, T and S pieces
    "1x3": [(0, 0), (0, 1), (0, 2)], "2x3": [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2)],
    "t": [(0, 0), (1, 0), (2, 0), (1, 1), (1, 2)], "s": [(0, 0), (1, 0), (1, 1), (2, 1)], "l": [(0, 0), (1, 0), (0, 1)],
}


def erode4(m: np.ndarray) -> np.ndarray:
    pad = np.pad(m, 1)
    return m & pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]


def convex_corners(cells: set[tuple[int, int]]) -> list[tuple[int, int, int, int]]:
    """Lattice vertices where exactly one of the four surrounding cells is filled: (X, Y, sx, sy) in px."""
    w = max(x for x, _ in cells) + 1
    h = max(y for _, y in cells) + 1
    out = []
    for j in range(h + 1):
        for i in range(w + 1):
            around = {"tl": (i - 1, j - 1), "tr": (i, j - 1), "bl": (i - 1, j), "br": (i, j)}
            filled = [k for k, c in around.items() if c in cells]
            if len(filled) == 1:
                k = filled[0]
                out.append((i * C, j * C, 1 if k in ("tr", "br") else -1, 1 if k in ("bl", "br") else -1))
    return sorted(out, key=lambda t: (t[1], t[0]))  # reading order


def build_mask(cells: set[tuple[int, int]], corners: list[tuple[int, int, int, int]], chamfered: list[int]) -> np.ndarray:
    w = max(x for x, _ in cells) + 1
    h = max(y for _, y in cells) + 1
    m = np.zeros((h * C, w * C), bool)
    for x, y in cells:
        m[y * C:(y + 1) * C, x * C:(x + 1) * C] = True
    for idx in chamfered:
        X, Y, sx, sy = corners[idx]
        px, py = (X if sx > 0 else X - 1), (Y if sy > 0 else Y - 1)
        for dy in range(CH):
            for dx in range(CH):
                if dx + dy < CH:
                    m[py + dy * sy, px + dx * sx] = False
    return m


def draw(tier: str, shape: str, state: str) -> np.ndarray:
    cells = set(SHAPES[shape])
    corners = convex_corners(cells)
    n = len(corners)
    if tier == "white":
        chamfered: list[int] = []
    elif tier == "green":
        chamfered = [0]
    elif tier == "blue":
        chamfered = [0, n - 1]
    else:
        chamfered = list(range(n))
    mask = build_mask(cells, corners, chamfered)
    ring1 = mask & ~erode4(mask)
    inner = erode4(mask)
    ring2 = inner & ~erode4(inner)
    bed = erode4(inner)
    T = tier.upper()
    frame = {"rest": f"{T}-BASE", "hi": f"{T}-HI", "lifthi": f"{T}-HI", "lift": f"{T}-BASE", "liftdim": f"{T}-EDGE"}[state]
    img = np.zeros((*mask.shape, 4), np.uint8)
    img[ring1] = (*P.rgb(f"{T}-EDGE"), 255)
    img[ring2] = (*P.rgb(frame), 255)
    img[bed] = (*P.rgb("INK-2"), 255)
    h, w = mask.shape
    if tier == "gold":  # short inner bracket line at each convex corner, 3 px each way, 1 px inside the frame
        for X, Y, sx, sy in corners:
            for k in range(4):
                for (x, y) in ((X + sx * (3 + k), Y + sy * 3), (X + sx * 3, Y + sy * (3 + k))):
                    xx, yy = (x if sx > 0 else x - 1), (y if sy > 0 else y - 1)
                    if 0 <= xx < w and 0 <= yy < h and bed[yy, xx]:
                        img[yy, xx] = (*P.rgb(frame), 255)
    if tier == "red":  # serrated top edge: Red-hi teeth hanging inward from the top ring
        for y in range(h):
            xs = [x for x in range(w) if mask[y, x] and (y == 0 or not mask[y - 1, x])]
            if not xs:
                continue
            runs, start, prev = [], xs[0], xs[0]
            for x in xs[1:] + [None]:
                if x is None or x != prev + 1:
                    runs.append((start, prev))
                    if x is not None:
                        start = x
                if x is not None:
                    prev = x
            for a, b in runs:
                x = a + 2
                while x + 1 <= b - 2:
                    for dy in (2, 3):
                        for dx in (0, 1):
                            if bed[y + dy, x + dx]:
                                img[y + dy, x + dx] = (*P.rgb("RED-HI"), 255)
                    x += 4
    if state.startswith("lift"):
        out = np.zeros((h + 2, w + 2, 4), np.uint8)
        sh = np.zeros((h + 2, w + 2), bool)
        sh[1:h + 1, 1:w + 1] = mask
        out[sh] = (*P.rgb("INK-0"), 255)
        solid = np.zeros_like(sh)
        solid[:h, :w] = mask
        out[:h, :w][mask] = img[mask]
        return out
    return img


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    count = 0
    for t in TIERS:
        for s in SHAPES:
            states = ["rest", "hi"] + (["lift", "lifthi", "liftdim"] if s == "1x1" else [])
            for st in states:
                Image.fromarray(draw(t, s, st), "RGBA").save(OUT / f"plate_{t}_{st}_{s}.png")
                count += 1
    print(f"wrote {count} plates to {OUT}")
    # evidence: colour strip, then greyscale strip (form must survive greyscale)
    strip = Image.new("RGB", (len(TIERS) * 28 + 4, 2 * 48 + 4), P.rgb("INK-1"))
    for i, t in enumerate(TIERS):
        a = Image.fromarray(draw(t, "1x1", "rest"), "RGBA")
        strip.paste(a, (4 + i * 28, 4), a)
        b = Image.fromarray(draw(t, "2x2", "rest"), "RGBA").resize((20, 20), Image.NEAREST)
    big = strip.resize((strip.width * 4, strip.height * 4), Image.NEAREST)
    grey = big.convert("L").convert("RGB")
    sheet = Image.new("RGB", (big.width, big.height * 2), P.rgb("INK-0"))
    sheet.paste(big, (0, 0)); sheet.paste(grey, (0, big.height))
    sheet.save(EVIDENCE / "plates_tiers_2026-11-15.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
