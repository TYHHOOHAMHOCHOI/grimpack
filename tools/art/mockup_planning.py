"""Greybox mock-up of the PLANNING state (worst case) at 640x360, in the master palette.

This is a measuring tool, not final art. Heroes and enemies are placeholder
silhouettes built from the rules in art bible Section 5; prose text is drawn as
placeholder blocks (no font exists yet); item glyphs are procedural stand-ins.
Its job is to re-measure the hand-computed figures in art bible 4.10 / 7.x:
lit-area budget, lit-mask precision, APL, and the palette audit.

Run:  python -I tools/art/mockup_planning.py
Writes to production/qa/evidence/.
"""

from __future__ import annotations

import random
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

W, H = 640, 360
SEED = 20261110

DIGITS = {
    "0": ("111", "101", "101", "101", "111"), "1": ("010", "110", "010", "010", "111"),
    "2": ("111", "001", "111", "100", "111"), "3": ("111", "001", "111", "001", "111"),
    "4": ("101", "101", "111", "001", "001"), "5": ("111", "100", "111", "001", "111"),
    "6": ("111", "100", "111", "101", "111"), "7": ("111", "001", "001", "001", "001"),
    "8": ("111", "101", "111", "101", "111"), "9": ("111", "101", "111", "001", "111"),
    "/": ("001", "001", "010", "100", "100"), "x": ("000", "101", "010", "101", "000"),
    "K": ("101", "101", "110", "101", "101"), "+": ("000", "010", "111", "010", "000"),
    "-": ("000", "000", "111", "000", "000"), " ": ("000",) * 5,
}

SHAPES = {
    "1x1": [(0, 0)], "1x2": [(0, 0), (0, 1)], "2x1": [(0, 0), (1, 0)],
    "2x2": [(0, 0), (1, 0), (0, 1), (1, 1)],
    "L0": [(0, 0), (1, 0), (0, 1)], "L1": [(0, 0), (1, 0), (1, 1)],
    "L2": [(1, 0), (0, 1), (1, 1)], "L3": [(0, 0), (0, 1), (1, 1)],
}
SHAPE_WEIGHT = {"1x1": 6, "1x2": 3, "2x1": 3, "2x2": 1, "L0": 1, "L1": 1, "L2": 1, "L3": 1}
TIER_WEIGHT = {"WHITE": 6, "GREEN": 5, "BLUE": 4, "PURPLE": 3, "GOLD": 2, "RED": 1}


class Canvas:
    def __init__(self) -> None:
        self.img = np.zeros((H, W, 3), np.uint8)
        self.info = np.zeros((H, W), bool)  # "things that matter" mask (4.10)
        self.img[:, :] = P.rgb("INK-0")

    def px(self, x: int, y: int, name: str, info: bool = False) -> None:
        if 0 <= x < W and 0 <= y < H:
            self.img[y, x] = P.rgb(name)
            if info:
                self.info[y, x] = True

    def rect(self, x: int, y: int, w: int, h: int, name: str, info: bool = False) -> None:
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.px(xx, yy, name, info)

    def outline(self, x: int, y: int, w: int, h: int, name: str, t: int = 1, info: bool = False) -> None:
        self.rect(x, y, w, t, name, info)
        self.rect(x, y + h - t, w, t, name, info)
        self.rect(x, y, t, h, name, info)
        self.rect(x + w - t, y, t, h, name, info)

    def panel(self, x: int, y: int, w: int, h: int, field: str = "INK-1") -> None:
        """Thick plain frame: 3 px INK-3 with a 1 px INK-4 highlight top-left (7.3)."""
        self.rect(x, y, w, h, field)
        self.outline(x, y, w, h, "INK-3", 3)
        self.rect(x, y, w, 1, "INK-4")
        self.rect(x, y, 1, h, "INK-4")

    def digits(self, x: int, y: int, text: str, name: str = "BONE", info: bool = True) -> int:
        for ch in text:
            g = DIGITS.get(ch, DIGITS[" "])
            for j, row in enumerate(g):
                for i, c in enumerate(row):
                    if c == "1":
                        self.px(x + i, y + j, name, info)
            x += 4
        return x

    def text_block(self, x: int, y: int, w: int, lines: int, name: str = "BONE", pitch: int = 14, gh: int = 8) -> None:
        """Placeholder prose: ~40% lit pixels per glyph row, which is the density of real text."""
        for ln in range(lines):
            lw = w - (ln % 3) * 9
            for yy in range(gh):
                for xx in range(lw):
                    if (xx * 7 + yy * 3 + ln) % 5 < 2 and (xx // 6) % 7 != 6:
                        self.px(x + xx, y + ln * pitch + yy, name, True)


def glyph_mask(kind: str) -> np.ndarray:
    m = np.zeros((12, 12), bool)
    if kind == "flask":
        for y in range(12):
            for x in range(12):
                if (x - 5.5) ** 2 + (y - 7.5) ** 2 <= 14 or (4 <= x <= 7 and y <= 3):
                    m[y, x] = True
    elif kind == "gem":
        for y in range(12):
            for x in range(12):
                if abs(x - 5.5) + abs(y - 5.5) <= 5.5:
                    m[y, x] = True
    elif kind == "shield":
        for y in range(12):
            for x in range(12):
                if 2 <= x <= 9 and y <= 7 or (y > 7 and abs(x - 5.5) <= (11 - y) * 0.9 + 0.5):
                    m[y, x] = True
    elif kind == "blade":
        for i in range(11):
            m[10 - i, i] = m[10 - i, min(i + 1, 11)] = True
        m[8:11, 2:5] = True
        m[9:11, 0:3] = True
    else:  # tome
        m[2:11, 2:10] = True
        m[2:11, 4] = False
    return m


def draw_glyph(cv: Canvas, cx: int, cy: int, kind: str, colors: tuple[str, str, str]) -> None:
    """Procedural 12x12 glyph centred on (cx, cy): outline INK-0, highlight, main, shade."""
    main, hi, shade = colors
    m = glyph_mask(kind)
    ox, oy = cx - 6, cy - 6
    h, w = m.shape

    def at(x: int, y: int) -> bool:
        return 0 <= x < w and 0 <= y < h and bool(m[y, x])

    for y in range(-1, h + 1):
        for x in range(-1, w + 1):
            if at(x, y):
                if not at(x - 1, y) or not at(x, y - 1):
                    cv.px(ox + x, oy + y, hi, True)
                elif not at(x + 1, y) or not at(x, y + 1):
                    cv.px(ox + x, oy + y, shade, True)
                else:
                    cv.px(ox + x, oy + y, main, True)
            elif any(at(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                cv.px(ox + x, oy + y, "INK-0", True)


class Plate:
    def __init__(self, cells: list[tuple[int, int]], tier: str, glyph: str, colors: tuple[str, str, str]) -> None:
        self.cells, self.tier, self.glyph, self.colors = cells, tier, glyph, colors


def convex_corners(cells: set[tuple[int, int]]) -> list[tuple[int, int, int, int]]:
    """Convex lattice vertices of a cell union: (vx, vy, inward_sx, inward_sy), reading order."""
    out = []
    xs = [c[0] for c in cells]
    ys = [c[1] for c in cells]
    for vy in range(min(ys), max(ys) + 2):
        for vx in range(min(xs), max(xs) + 2):
            around = {(-1, -1): (vx - 1, vy - 1), (1, -1): (vx, vy - 1), (-1, 1): (vx - 1, vy), (1, 1): (vx, vy)}
            present = [k for k, c in around.items() if c in cells]
            if len(present) == 1:
                sx, sy = present[0]
                out.append((vx, vy, sx, sy))
    return out


def draw_plate(cv: Canvas, gx: int, gy: int, C: int, plate: Plate, tier_cues: bool = True) -> tuple[int, int, int, int]:
    """Draw one plate at grid origin (gx, gy). Returns its pixel bounding box."""
    cells = set(plate.cells)
    minx = min(c[0] for c in cells); miny = min(c[1] for c in cells)
    maxx = max(c[0] for c in cells); maxy = max(c[1] for c in cells)
    bw, bh = (maxx - minx + 1) * C, (maxy - miny + 1) * C
    mask = np.zeros((bh, bw), bool)
    for (cx, cy) in cells:
        mask[(cy - miny) * C:(cy - miny + 1) * C, (cx - minx) * C:(cx - minx + 1) * C] = True
    corners = convex_corners(cells)
    t = plate.tier
    n_ch = {"WHITE": 0, "GREEN": 1, "BLUE": 2}.get(t, len(corners))
    chosen = corners if n_ch >= len(corners) else (corners[:1] if n_ch == 1 else [corners[0], corners[-1]] if n_ch == 2 else [])
    K = 3
    for (vx, vy, sx, sy) in chosen:
        X, Y = (vx - minx) * C, (vy - miny) * C
        for i in range(K):
            for j in range(K - i):
                x = X + (i if sx > 0 else -1 - i)
                y = Y + (j if sy > 0 else -1 - j)
                if 0 <= x < bw and 0 <= y < bh:
                    mask[y, x] = False

    def inside(x: int, y: int) -> bool:
        return 0 <= x < bw and 0 <= y < bh and bool(mask[y, x])

    edge, base = f"{t}-EDGE", f"{t}-BASE"
    for y in range(bh):
        for x in range(bw):
            if not mask[y, x]:
                continue
            n4 = [inside(x + 1, y), inside(x - 1, y), inside(x, y + 1), inside(x, y - 1)]
            if not all(n4):
                col = edge
            else:
                n8 = n4 + [inside(x + 1, y + 1), inside(x - 1, y - 1), inside(x + 1, y - 1), inside(x - 1, y + 1)]
                col = base if not all(n8) else "INK-2"
                # frame ring: a pixel whose 4-neighbours include an edge pixel
                if col == "INK-2":
                    ring = False
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        xx, yy = x + dx, y + dy
                        nb4 = [inside(xx + 1, yy), inside(xx - 1, yy), inside(xx, yy + 1), inside(xx, yy - 1)]
                        if inside(xx, yy) and not all(nb4):
                            ring = True
                    col = base if ring else "INK-2"
            cv.px(gx + minx * C + x, gy + miny * C + y, col, True)
    ox, oy = gx + minx * C, gy + miny * C
    if t == "GOLD" and tier_cues:
        for (vx, vy, sx, sy) in corners:
            X, Y = ox + (vx - minx) * C, oy + (vy - miny) * C
            for k in range(3, 8):
                cv.px(X + (k if sx > 0 else -1 - k), Y + (3 if sy > 0 else -4), base, True)
                cv.px(X + (3 if sx > 0 else -4), Y + (k if sy > 0 else -1 - k), base, True)
    if t == "RED" and tier_cues:
        for x in range(bw):
            if mask[0, x] and x % 4 in (1, 2) and 6 < x < bw - 6:
                cv.px(ox + x, oy + 2, "RED-HI", True)   # teeth hang inward from the lit ring, in RED-HI (3.4.4)
                cv.px(ox + x, oy + 3, "RED-HI", True)
    # glyph: compact glyph on the elbow cell / centre of the first cell
    if len(cells) == 1:
        gcx, gcy = ox + C // 2, oy + C // 2
    else:
        corner = None
        for c in cells:
            if sum(1 for d in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (c[0] + d[0], c[1] + d[1]) in cells) >= 2 or len(cells) == 2:
                corner = c
        if len(cells) == 2:
            gcx, gcy = ox + bw // 2, oy + bh // 2
        else:
            c = corner or plate.cells[0]
            gcx, gcy = ox + (c[0] - minx) * C + C // 2, oy + (c[1] - miny) * C + C // 2
    draw_glyph(cv, gcx, gcy, plate.glyph, plate.colors)
    return ox, oy, bw, bh


def pack_plates(cols: int, rows: int, target: float, rng: random.Random) -> list[Plate]:
    occ = [[False] * cols for _ in range(rows)]
    plates: list[Plate] = []
    names = list(SHAPES)
    weights = [SHAPE_WEIGHT[n] for n in names]
    tiers, tw = list(TIER_WEIGHT), list(TIER_WEIGHT.values())
    kinds = ["flask", "gem", "shield", "blade", "tome"]
    pool = [P.PALETTE_GROUPS_FOR_ICONS] if hasattr(P, "PALETTE_GROUPS_FOR_ICONS") else None
    icon_sets = [("FOG", "FOG-HI", "INK-4"), ("BLOOD", "RED-HI", "BLOOD-D"), ("GREEN-BASE", "GREEN-HI", "GREEN-EDGE"),
                 ("BLUE-BASE", "BLUE-HI", "BLUE-EDGE"), ("CANDLE", "BONE", "CANDLE-D"),
                 ("PURPLE-BASE", "PURPLE-HI", "PURPLE-EDGE"), ("GOLD-BASE", "GOLD-HI", "GOLD-EDGE"),
                 ("INK-4", "FOG", "INK-3")]
    del pool
    free = [(x, y) for y in range(rows) for x in range(cols)]
    for _ in range(400):
        if sum(sum(r) for r in occ) / (cols * rows) >= target:
            break
        x, y = rng.choice(free)
        for name in rng.choices(names, weights, k=6):
            cells = [(x + dx, y + dy) for dx, dy in SHAPES[name]]
            if all(0 <= cx < cols and 0 <= cy < rows and not occ[cy][cx] for cx, cy in cells):
                for cx, cy in cells:
                    occ[cy][cx] = True
                tier = rng.choices(tiers, tw)[0]
                plates.append(Plate(cells, tier, rng.choice(kinds), rng.choice(icon_sets)))
                break
    return plates


def hero(cv: Canvas, cx: int, ground_y: int, family: str, selected: bool) -> None:
    Hh = 32
    w = {"block": 29, "round": 22, "tall": 16}[family]
    x0, y0 = cx - w // 2, ground_y - Hh
    mask = np.zeros((Hh, w), bool)
    for y in range(Hh):
        for x in range(w):
            if family == "round":
                mask[y, x] = ((x - (w - 1) / 2) / (w / 2)) ** 2 + ((y - (Hh - 1) / 2) / (Hh / 2)) ** 2 <= 1
            else:
                mask[y, x] = True
    def inside(x: int, y: int) -> bool:
        return 0 <= x < w and 0 <= y < Hh and bool(mask[y, x])
    for y in range(-1, Hh + 1):
        for x in range(-1, w + 1):
            if inside(x, y):
                top_left_edge = not inside(x - 1, y) or not inside(x, y - 1)
                if top_left_edge:
                    cv.px(x0 + x, y0 + y, "CANDLE", True)
                elif not inside(x + 1, y) or not inside(x, y + 1):
                    cv.px(x0 + x, y0 + y, "INK-2", True)
                else:
                    cv.px(x0 + x, y0 + y, "INK-3", True)
            elif any(inside(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                cv.px(x0 + x, y0 + y, "INK-0", True)
    fx, fy = cx - 4, y0 + 8
    cv.rect(fx, fy, 8, 5, "CANDLE-D", True)
    cv.px(fx + 2, fy + 2, "INK-0", True)
    cv.px(fx + 5, fy + 2, "INK-0", True)
    # prop (identity only): crown, hand or back, at most 0.25 H above the body
    if family == "tall":
        cv.rect(cx - 4, y0 - 8, 8, 8, "INK-3", True)
        cv.outline(cx - 4, y0 - 8, 8, 8, "INK-0", 1, True)
        cv.rect(cx - 4, y0 - 8, 8, 1, "CANDLE", True)
    elif family == "round":
        cv.rect(x0 + w + 1, y0 - 4, 3, 30, "INK-4", True)
    else:
        cv.rect(x0 - 5, y0 + 6, 4, 14, "INK-3", True)
        cv.outline(x0 - 5, y0 + 6, 4, 14, "INK-0", 1, True)
    # pedestal ellipse with Candle pool
    for y in range(-6, 7):
        for x in range(-26, 27):
            d = (x / 26.0) ** 2 + (y / 6.0) ** 2
            if d <= 1.0:
                cv.px(cx + x, ground_y + 3 + y, "CANDLE-D" if d <= 0.6 else "INK-2", True)
            elif d <= 1.25 and selected:
                cv.px(cx + x, ground_y + 3 + y, "CANDLE", True)


def enemy(cv: Canvas, cx: int, ground_y: int, kind: str, magnitude: str, target: str) -> None:
    Hh = {"medium": 40, "small": 26, "boss": 96}[kind]
    w = {"medium": 24, "small": 18, "boss": 44}[kind]
    x0, y0 = cx - w // 2, ground_y - Hh
    for y in range(Hh):
        for x in range(w):
            top_left = x == 0 or y == 0
            cv.px(x0 + x, y0 + y, "FOG" if top_left else ("INK-1" if (x == w - 1 or y == Hh - 1) else "INK-2"), True)
    cv.outline(x0 - 1, y0 - 1, w + 2, Hh + 2, "INK-4", 1, True)   # enemy outline A: INK-4 on all sides (5.9)
    if kind in ("medium", "boss"):  # one oversized arm = the single wrongness device
        cv.rect(x0 - 9, y0 + 10, 9, 22, "INK-2", True)
        cv.rect(x0 - 9, y0 + 10, 9, 1, "FOG", True)
        cv.rect(x0 - 9, y0 + 10, 1, 22, "FOG", True)
    cv.rect(x0 + 5, y0 + 8, 3, 3, "INK-0", True)
    cv.rect(x0 + w - 8, y0 + 12, 3, 3, "INK-0", True)
    # intent tab: pennant (downward) with glyph, magnitude, target chip
    tx, ty = cx - 20, y0 - 26
    cv.rect(tx, ty, 40, 18, "INK-1")
    cv.outline(tx, ty, 40, 18, "INK-4", 1)
    for i in range(6):
        cv.rect(cx - 5 + i, ty + 18 + i, 10 - 2 * i, 1, "INK-4")
    for j, row in enumerate(("0011100", "0111110", "1111111", "0111110", "0011100", "0001000", "0001000")):
        for i, c in enumerate(row):
            if c == "1":
                cv.px(tx + 4 + i, ty + 5 + j, "FOG-HI", True)
    cv.digits(tx + 14, ty + 6, magnitude, "FOG-HI")
    cv.rect(tx + 31, ty + 3, 7, 12, "INK-2", True)
    cv.digits(tx + 33, ty + 7, target, "BONE")


def brick_wall(cv: Canvas, x: int, y: int, w: int, h: int) -> None:
    """D1 plane: running bond, INK-1 face, INK-0 joints (6.3)."""
    cv.rect(x, y, w, h, "INK-1")
    for yy in range(y, y + h, 8):
        cv.rect(x, yy, w, 1, "INK-0")
        off = 0 if ((yy - y) // 8) % 2 == 0 else 8
        for xx in range(x + off, x + w, 16):
            cv.rect(xx, yy, 1, 8, "INK-0")


def build(C: int) -> tuple[Canvas, dict]:
    rng = random.Random(SEED)
    cv = Canvas()
    LW = 8 * C + 14           # left workbench width (7.3 formula)
    PW = 140                  # party column width
    top = 16
    meta: dict = {}

    # --- top strip (16 px, chrome-dim; only numerals and chips are lit)
    cv.rect(0, 0, W, top, "INK-1")
    cv.rect(0, top - 1, W, 1, "INK-3")
    cv.rect(8, 4, 7, 7, "CANDLE", True); cv.outline(8, 4, 7, 7, "INK-0", 1, True)
    cv.digits(19, 5, "128", "BONE")
    cv.digits(52, 5, "7/9", "BONE")
    sx = LW + 8
    for i, (num, kind) in enumerate((("1", "hero"), ("2", "hero"), ("1", "foe"), ("2", "foe"))):
        cx0 = sx + i * 17
        cv.rect(cx0, 1, 14, 13, "INK-1")
        cv.outline(cx0, 1, 14, 13, "INK-4", 1)
        if kind == "hero":
            cv.digits(cx0 + 5, 5, num, "BONE")
        else:
            for j, row in enumerate(("00100", "01110", "11111", "01110", "00100")):
                for ii, c in enumerate(row):
                    if c == "1":
                        cv.px(cx0 + 4 + ii, 4 + j, "FOG-HI", True)
    cv.rect(sx + 3, 14, 7, 1, "CANDLE", True)   # active-turn chevron strip under the first chip
    cv.outline(W - 18, 2, 12, 12, "INK-4", 1)
    cv.rect(W - 13, 4, 2, 8, "INK-4"); cv.rect(W - 16, 7, 8, 2, "INK-4")

    # --- panels
    cv.panel(0, top, LW, H - top)
    cv.panel(LW, top, W - LW, H - top)
    # name plate (16 px): Candle chevron, join numeral, name placeholder
    cv.rect(6, top + 5, 5, 2, "CANDLE", True); cv.rect(7, top + 7, 3, 1, "CANDLE", True)
    cv.digits(14, top + 5, "1", "BONE")
    cv.text_block(22, top + 3, 60, 1, "BONE", gh=8)

    # --- left workbench: grid (8 x 6, late-run worst case), tabs, stash
    gx, gy = 4, top + 3 + 16
    rows_g, cols_g = 6, 8
    for r in range(rows_g):
        for c in range(cols_g):
            cv.rect(gx + c * C, gy + r * C, C, C, "INK-2")
            cv.rect(gx + c * C + C - 1, gy + r * C, 1, C, "INK-3")
            cv.rect(gx + c * C, gy + r * C + C - 1, C, 1, "INK-3")
    plates = pack_plates(cols_g, rows_g, 0.90, rng)
    boxes = []
    for p in plates:
        boxes.append(draw_plate(cv, gx, gy, C, p))
    occ = {c: i for i, p in enumerate(plates) for c in p.cells}
    meta["items_in_grid"] = len(plates)
    meta["grid_occupancy"] = round(len(occ) / (cols_g * rows_g), 3)

    # resting bridges: Fog, 1 px, on the 2 px seam between adjacent plates (about 10 pairs)
    pairs = []
    for (cx, cy), i in occ.items():
        for dx, dy in ((1, 0), (0, 1)):
            j = occ.get((cx + dx, cy + dy))
            if j is not None and j != i:
                pairs.append(((cx, cy), (dx, dy)))
    rng.shuffle(pairs)
    for (cx, cy), (dx, dy) in pairs[:10]:
        if dx == 1:   # vertical seam: bridge runs along the seam
            sxp = gx + (cx + 1) * C
            for k in range(6):
                cv.px(sxp - 1, gy + cy * C + C // 2 - 3 + k, "FOG", True)
                cv.px(sxp, gy + cy * C + C // 2 - 3 + k, "FOG", True)
        else:
            syp = gy + (cy + 1) * C
            for k in range(6):
                cv.px(gx + cx * C + C // 2 - 3 + k, syp - 1, "FOG", True)
                cv.px(gx + cx * C + C // 2 - 3 + k, syp, "FOG", True)
    meta["resting_bridges"] = min(10, len(pairs))
    # one focused plate: Candle 2 px ring, z-lifted over neighbours, with preview bridges (Fog-hi, 2 px)
    fx, fy, fw, fh = boxes[len(boxes) // 2]
    cv.outline(fx - 3, fy - 3, fw + 6, fh + 6, "CANDLE", 2, True)

    # tabs + lower section (stash)
    ty0 = gy + rows_g * C + 4
    cv.rect(4, ty0, 70, 14, "INK-2"); cv.outline(4, ty0, 70, 14, "INK-4", 1)
    cv.text_block(10, ty0 + 3, 54, 1, "BONE", gh=7)
    cv.rect(78, ty0, 64, 14, "INK-1"); cv.outline(78, ty0, 64, 14, "INK-3", 1)
    cv.text_block(84, ty0 + 3, 50, 1, "FOG-HI", gh=7)
    sy = ty0 + 18
    stash_rows = max(1, min(4, (H - 3 - sy) // C))
    for r in range(stash_rows):
        for c in range(cols_g):
            cv.rect(gx + c * C, sy + r * C, C, C, "INK-2")
            cv.rect(gx + c * C + C - 1, sy + r * C, 1, C, "INK-3")
            cv.rect(gx + c * C, sy + r * C + C - 1, C, 1, "INK-3")
    for p in pack_plates(cols_g, stash_rows, 0.45, rng):
        draw_plate(cv, gx, sy, C, p)

    # --- party column (three slots)
    pitch = C + 4 + 40 + 8 + 10 + 12
    meta["slot_pitch"] = pitch
    px0 = LW + 3
    brick_wall(cv, px0, top + 3, PW - 3, H - top - 6 - 24)
    cx_party = px0 + (PW - 3) // 2
    fams = ["block", "round", "tall"]
    hp = [(28, 40), (22, 30), (17, 24)]
    for s in range(3):
        sy0 = top + 6 + s * pitch
        ground = sy0 + (C + 4) + 40
        hero(cv, cx_party, ground, fams[s], selected=(s == 0))
        cv.digits(cx_party - 23, ground + 1, str(s + 1), "BONE")
        # HP row
        hy = ground + 8 + 3
        hx = cx_party - 38
        for j, row in enumerate(("0110110", "1111111", "1111111", "0111110", "0011100", "0001000")):
            for i, c in enumerate(row):
                if c == "1":
                    cv.px(hx + i, hy + j, "BLOOD", True)
        cv.rect(hx + 10, hy, 36, 7, "INK-0", True); cv.outline(hx + 10, hy, 36, 7, "INK-4", 1, True)
        cv.rect(hx + 11, hy + 1, int(34 * hp[s][0] / hp[s][1]), 5, "BLOOD", True)
        cv.digits(hx + 49, hy + 1, f"{hp[s][0]}/{hp[s][1]}", "BONE")
        # stat row
        ry = hy + 12
        for j, row in enumerate(("111111", "111111", "111111", "011110", "001100")):
            for i, c in enumerate(row):
                if c == "1":
                    cv.px(hx + i, ry + j, "FOG", True)
        cv.digits(hx + 9, ry, "5", "BONE")
        cv.rect(hx + 18, ry + 1, 20, 4, "INK-0", True); cv.outline(hx + 18, ry + 1, 20, 4, "INK-4", 1, True)
        cv.rect(hx + 19, ry + 2, 9 + s * 3, 2, "FOG", True)
        for k in range(3):
            cv.rect(hx + 42 + k * 11, ry - 1, 10, 8, "INK-1", True)
            cv.outline(hx + 42 + k * 11, ry - 1, 10, 8, "INK-4", 1, True)
            cv.rect(hx + 45 + k * 11, ry + 2, 3, 3, "BONE", True)

    # --- right column: no dock. Enemies stand in TWO vertical lines side by side (user decision):
    # line 1 against the party column, line 2 to its right. Item detail is a hover popup (drawn last).
    rx = LW + PW
    rw = W - rx - 3
    zone_h = (H - 3 - 24) - (top + 3)
    cv.rect(rx, top + 3, rw, zone_h, "INK-1")
    LINE_W = 140
    l1x0, l2x0 = rx, rx + LINE_W + 4        # 4 px pier between the two bays
    def bay(x0: int, round_head: bool) -> None:
        cxb = x0 + LINE_W // 2
        cyb = top + 3 + LINE_W // 2
        for y in range(top + 3, top + 3 + zone_h):
            for x in range(x0, x0 + LINE_W):
                inside = y >= cyb or (not round_head) or (x - cxb) ** 2 + (y - cyb) ** 2 <= (LINE_W // 2) ** 2
                if inside:
                    cv.px(x, y, "INK-0")
    bay(l1x0, True)       # round-headed niche over line 1
    bay(l2x0, False)      # square-headed void bay over line 2
    for y in range(top + 3, top + 3 + zone_h, 8):   # running-bond joints on the wall mass
        for x in range(rx, rx + rw):
            if not (l1x0 <= x < l1x0 + LINE_W or l2x0 <= x < l2x0 + LINE_W):
                cv.px(x, y, "INK-0")
    heights = {"medium": 40, "small": 26, "boss": 96}

    def enemy_line(cxl: int, kinds: list, start_y: int) -> list:
        # each line is centred in the column independently
        slots = [26 + heights[k] + 4 + 4 + 10 + 12 for k, _, _ in kinds]
        total = sum(slots)
        y = top + 3 + max(4, (zone_h - total) // 2)
        for (kind, mag, tgt), slot in zip(kinds, slots):
            ground = y + 26 + heights[kind]
            enemy(cv, cxl, ground, kind, mag, tgt)
            hy = ground + 8
            for j, row in enumerate(("0110110", "1111111", "1111111", "0111110", "0011100", "0001000")):
                for i, c in enumerate(row):
                    if c == "1":
                        cv.px(cxl - 30 + i, hy + j, "BLOOD", True)
            cv.rect(cxl - 20, hy, 36, 7, "INK-0", True); cv.outline(cxl - 20, hy, 36, 7, "INK-4", 1, True)
            cv.rect(cxl - 19, hy + 1, 24, 5, "BLOOD", True)
            cv.digits(cxl + 19, hy + 1, "30", "BONE")
            sy2 = hy + 10
            for j, row in enumerate(("111111", "111111", "111111", "011110", "001100")):
                for i, c in enumerate(row):
                    if c == "1":
                        cv.px(cxl - 30 + i, sy2 + j, "FOG", True)
            cv.digits(cxl - 21, sy2, "2", "BONE")
            for k2 in range(2):
                cv.rect(cxl - 6 + k2 * 11, sy2 - 1, 10, 8, "INK-1", True)
                cv.outline(cxl - 6 + k2 * 11, sy2 - 1, 10, 8, "INK-4", 1, True)
                cv.rect(cxl - 3 + k2 * 11, sy2 + 2, 3, 3, "BONE", True)
            y += slot
        return slots

    s1 = enemy_line(l1x0 + LINE_W // 2, [("medium", "12x3", "1"), ("small", "8", "2"), ("medium", "6x2", "3")], 0)
    s2 = enemy_line(l2x0 + LINE_W // 2, [("small", "5", "1"), ("medium", "9", "2")], 0)
    meta["enemy_zone_px"] = zone_h
    meta["enemy_line1_slots"] = s1
    meta["enemy_line2_slots"] = s2
    meta["enemy_line1_used_px"] = sum(s1)
    meta["enemy_line2_used_px"] = sum(s2)
    meta["right_margin_px"] = rw - (2 * LINE_W + 4)

    # --- action strip (24 px): primary action
    ay = H - 3 - 24
    cv.rect(LW + 3, ay, W - LW - 6, 24, "INK-1")
    cv.outline(W - 3 - 86, ay + 3, 80, 18, "CANDLE", 2, True)
    cv.text_block(W - 3 - 78, ay + 6, 56, 1, "BONE", gh=8)
    cv.outline(LW + 8, ay + 6, 12, 12, "INK-4", 1, True)
    cv.rect(LW + 12, ay + 10, 4, 4, "BONE", True)

    # --- hover popup (7.9): the hovered plate is the focused one in the grid; it lands in the
    # workbench lower section (cost 1) because the stage has no legal rectangle. Compact: 152 x 140.
    pw_, ph_ = 152, 140
    pxx, pyy = gx + 20, sy - 6
    cv.rect(pxx, pyy, pw_, ph_, "INK-1")
    cv.outline(pxx, pyy, pw_, ph_, "INK-3", 3)
    cv.rect(pxx, pyy, pw_, 1, "INK-4"); cv.rect(pxx, pyy, 1, ph_, "INK-4")
    ix, iy = pxx + 8, pyy + 8
    cv.text_block(ix, iy, 100, 1, "BONE", gh=8)                 # name (Heading)
    cv.text_block(ix, iy + 14, 70, 1, "FOG-HI", gh=7)           # tier name in words
    for d in range(3):                                           # rank diamonds
        for j, row in enumerate(("010", "111", "010")):
            for i, c in enumerate(row):
                if c == "1":
                    cv.px(ix + 80 + d * 5 + i, iy + 15 + j, "BLUE-BASE", True)
    cv.digits(ix, iy + 28, "12", "BONE"); cv.digits(ix + 22, iy + 28, "3", "BONE"); cv.digits(ix + 36, iy + 28, "5", "BONE")
    for ln in range(2):                                          # effect lines with keyword glyph + underline
        cv.rect(ix, iy + 42 + ln * 14, 6, 6, "BONE", True)
        cv.text_block(ix + 10, iy + 40 + ln * 14, pw_ - 36, 1, "BONE", gh=8)
        cv.rect(ix + 10, iy + 49 + ln * 14, 36, 1, "FOG-HI", True)
    for k in range(2):                                           # affects: join numerals of heroes
        cv.rect(ix + k * 14, iy + 72, 12, 10, "INK-1", True); cv.outline(ix + k * 14, iy + 72, 12, 10, "INK-4", 1, True)
        cv.digits(ix + 4 + k * 14, iy + 74, str(k + 2), "BONE")
    cv.rect(pxx + pw_ - 52, pyy + ph_ - 30, 44, 22, "PLUM", True)  # curse tag, hanging
    cv.outline(pxx + pw_ - 52, pyy + ph_ - 30, 44, 22, "PLUM-E", 1, True)
    cv.text_block(pxx + pw_ - 48, pyy + ph_ - 26, 36, 1, "BONE", gh=7)
    meta["popup_px"] = (pw_, ph_)
    return cv, meta


def measure(img: np.ndarray, info: np.ndarray) -> dict:
    lin = np.vectorize(P.srgb_to_linear)(np.arange(256)).astype(np.float64)
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    lum = 0.2126 * lin[r] + 0.7152 * lin[g] + 0.0722 * lin[b]
    mx = img.max(axis=2).astype(np.float64); mn = img.min(axis=2).astype(np.float64)
    sat = np.where(mx == 0, 0, (mx - mn) / np.maximum(mx, 1)); val = mx / 255.0
    lit = (lum >= 0.30) | ((sat >= 0.60) & (val >= 0.55))
    saturated = (sat >= 0.60) & (val >= 0.55)
    total = img.shape[0] * img.shape[1]
    pal = {P.rgb(n) for n in P.PALETTE}
    flat = {tuple(p) for p in img.reshape(-1, 3)}
    off = sorted(flat - pal)
    floor_bad = int(((img <= 0x0A).all(axis=2)).sum())
    ceil_bad = int(((img >= 0xF6).all(axis=2)).sum())
    return {
        "lit_pct": 100.0 * lit.sum() / total,
        "lit_in_info_pct": 100.0 * (lit & info).sum() / max(1, lit.sum()),
        "lit_outside_info": int((lit & ~info).sum()),
        "saturated_pct": 100.0 * saturated.sum() / total,
        "apl": float(lum.mean()),
        "palette_off": len(off), "palette_off_examples": off[:5],
        "below_floor": floor_bad, "above_ceiling": ceil_bad,
        "lit": lit,
    }


def save_images(cv: Canvas, m: dict, C: int, out: Path) -> None:
    img = Image.fromarray(cv.img, "RGB")
    img.save(out / f"mockup-planning-c{C}.png")
    img.resize((W * 3, H * 3), Image.NEAREST).save(out / f"mockup-planning-c{C}-3x.png")
    grey = img.convert("L").convert("RGB")
    grey.resize((W * 3, H * 3), Image.NEAREST).save(out / f"mockup-planning-c{C}-grey-3x.png")
    over = cv.img.copy()
    lit = m["lit"]
    over[lit & cv.info] = (0, 200, 0)
    over[lit & ~cv.info] = (255, 0, 255)
    Image.fromarray(over, "RGB").resize((W * 2, H * 2), Image.NEAREST).save(out / f"mockup-planning-c{C}-litmask-2x.png")


def tier_strip(C: int, out: Path) -> None:
    """Six tiers x (1x1, 1x2, L) with SAME-HUE icons, for the greyscale and colour-blind read."""
    cv = Canvas()
    cv.rect(0, 0, W, H, "INK-1")
    kinds = ["gem", "shield", "flask", "blade", "tome", "gem"]
    hue = {"WHITE": ("WHITE-BASE", "WHITE-HI", "WHITE-EDGE"), "GREEN": ("GREEN-BASE", "GREEN-HI", "GREEN-EDGE"),
           "BLUE": ("BLUE-BASE", "BLUE-HI", "BLUE-EDGE"), "PURPLE": ("PURPLE-BASE", "PURPLE-HI", "PURPLE-EDGE"),
           "GOLD": ("GOLD-BASE", "GOLD-HI", "GOLD-EDGE"), "RED": ("RED-BASE", "RED-HI", "RED-EDGE")}
    for i, t in enumerate(P.TIERS):
        x = 20 + i * 100
        for k, shape in enumerate(("1x1", "1x2", "L0")):
            cells = SHAPES[shape]
            draw_plate(cv, x, 20 + k * 110, C, Plate(cells, t, kinds[i], hue[t]))
    img = Image.fromarray(cv.img, "RGB")
    img.resize((W * 3, H * 3), Image.NEAREST).save(out / f"tier-strip-c{C}-3x.png")
    img.convert("L").convert("RGB").resize((W * 3, H * 3), Image.NEAREST).save(out / f"tier-strip-c{C}-grey-3x.png")


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    out = root / "production" / "qa" / "evidence"
    out.mkdir(parents=True, exist_ok=True)
    lines = ["# Planning mock-up measurements (greybox, worst case)", "",
             "Generated by `tools/art/mockup_planning.py`. Greybox only: placeholder silhouettes, placeholder text blocks,",
             "procedural item glyphs. Figures re-measure the hand estimates in art bible 4.10 and 7.x.", "",
             "| Measure | C = 24 | C = 20 | Budget or rule |", "| ---- | ---- | ---- | ---- |"]
    results = {}
    for C in (24, 20):
        cv, meta = build(C)
        m = measure(cv.img, cv.info)
        save_images(cv, m, C, out)
        tier_strip(C, out)
        results[C] = (m, meta)
        print(f"C={C}: lit {m['lit_pct']:.2f}%  precision {m['lit_in_info_pct']:.1f}%  APL {m['apl']:.3f}  "
              f"saturated {m['saturated_pct']:.2f}%  off-palette {m['palette_off']}  items {meta['items_in_grid']}  "
              f"occupancy {meta['grid_occupancy']}  slot pitch {meta['slot_pitch']}")
    a, b = results[24][0], results[20][0]
    ma, mb = results[24][1], results[20][1]

    def row(label: str, fa: str, fb: str, rule: str) -> None:
        lines.append(f"| {label} | {fa} | {fb} | {rule} |")
    row("Lit area (lit mask, 4.10)", f"{a['lit_pct']:.2f}%", f"{b['lit_pct']:.2f}%", "Planning budget 12%")
    row("Lit pixels inside the mattering mask", f"{a['lit_in_info_pct']:.1f}%", f"{b['lit_in_info_pct']:.1f}%", "At least 90%")
    row("Lit pixels outside the mask", str(a["lit_outside_info"]), str(b["lit_outside_info"]), "Should be near 0 (mask is by construction here)")
    row("Saturated area (S >= 0.60, V >= 0.55)", f"{a['saturated_pct']:.2f}%", f"{b['saturated_pct']:.2f}%", "Tier and Red-room light cap 12% (2.2 rule 4); icons counted here too")
    row("APL (mean relative luminance)", f"{a['apl']:.3f}", f"{b['apl']:.3f}", "Planning cap 0.08")
    row("Off-palette pixels", str(a["palette_off"]), str(b["palette_off"]), "0")
    row("Pixels below the ink floor", str(a["below_floor"]), str(b["below_floor"]), "0")
    row("Pixels above the light ceiling", str(a["above_ceiling"]), str(b["above_ceiling"]), "0")
    row("Grid items / occupancy", f"{ma['items_in_grid']} / {ma['grid_occupancy']}", f"{mb['items_in_grid']} / {mb['grid_occupancy']}", "Late-run worst case (about 90%)")
    row("Hero slot pitch (px)", str(ma["slot_pitch"]), str(mb["slot_pitch"]), "98 at C=24, 94 at C=20 (7.4)")
    row("Enemy line 1 / line 2 used (px of about 300)", f"{ma['enemy_line1_used_px']} / {ma['enemy_line2_used_px']}", f"{mb['enemy_line1_used_px']} / {mb['enemy_line2_used_px']}", "Per line at most 300")
    row("Margin right of two lines (px)", str(ma["right_margin_px"]), str(mb["right_margin_px"]), "10 at C=24, 42 at C=20 (less 3 px frame)")
    lines += ["", "Method: lit = relative luminance >= 0.30 or (HSV saturation >= 0.60 and value >= 0.55). The mattering mask is",
              "set by construction (everything drawn as an item, hero, enemy, HP, intent, text or bridge counts as mattering),",
              "so the precision figure here only proves the script's mask is complete; it does not test a real artist's frame."]
    (out / "mockup-planning-measurements.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
